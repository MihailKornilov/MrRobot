using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using static System.Console;

using CefSharp;
using CefSharp.Wpf;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Section;
using MrRobot.Connector;

namespace MrRobot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var dur = new Dur();

            AppLoadControl();

            // Формат даны в виде 03.12.2023 (для календаря)
            var dtInfo = new DateTimeFormatInfo()
            {
                ShortDatePattern = "dd.MM.yyyy",
                ShortTimePattern = "hh:mm:ss tt",
                DateSeparator = ".",
                TimeSeparator = ":"
            };
            // Точка вместо запятой в дробных числах
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us")
            {
                DateTimeFormat = dtInfo
            };


            // Установка каталога кеша для Веб-браузера
            var Settings = new CefSettings();
            Settings.CachePath = Directory.GetCurrentDirectory() + @"\CefSharpCache";
            Cef.Initialize(Settings);


            new position();
            new HttpServer();
            new Market();
            new Instrument();
            new Candle();
            new Robots();
            new CDIpanel();
            new MOEX();

            InitializeComponent();

            G.MW = this;

            // Разрешение экрана
            //Rect scr = SystemParameters.WorkArea;
            //WriteLine("scr.Width = " + scr.Width);
            //WriteLine("scr.Height = " + scr.Height);

            Width  = position.Val("MainWindow.Width", 1366);
            Height = position.Val("MainWindow.Height", 700);
            Left   = position.Val("MainWindow.Left",   100);
            Top    = position.Val("MainWindow.Top",    100);


            Loaded += MouseHookInit;
            Loaded += ISunit.Init;
            Loaded += G.LogFile.FileRead;
            SizeChanged += Depth.SizeChanged;
            SizeChanged += G.Tester.RobotLogWidthSet;
            Closed += HttpServer.Stop;

            new MainMenu();

            G.Inited();
            G.LogWrite($"Загружено за {dur.Second()} сек.");
        }

        void AppLoadControl()
        {
            G.LogWrite();
            G.LogWrite("MrRobot загружается...");

            AppExceptionLog();
            Control_Mysqld();
        }

        void AppExceptionLog()
        {
            AppDomain.CurrentDomain.FirstChanceException += FirstChanceExceptionEventHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
        }
        static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            G.LogWrite($"Необработанное исключение: {e.ExceptionObject}", "error.txt");
        }
        static void FirstChanceExceptionEventHandler(object sender, FirstChanceExceptionEventArgs e)
        {
            G.LogWrite($"Обработанное исключение: {e.Exception}", "error.txt");
        }


        /// <summary>
        /// Проверка запущен ли сервер mysqld
        /// </summary>
        void Control_Mysqld()
        {
            if (Process.GetProcessesByName("mysqld").Length > 0)
                return;

            string ProcessName = "mysqld_robot";
            if (Process.GetProcessesByName(ProcessName).Length > 0)
                return;

            G.LogWrite($"Запуск процесса `{ProcessName}`...");
            var mysqld = Process.Start(Path.GetFullPath($"mysql\\server\\bin\\{ProcessName}.exe"));
            G.LogWrite($"Процесс `{ProcessName}` запущен. ID: {mysqld.Id}");
            //Environment.Exit(0);
        }





        void MouseHookInit(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(MouseHook);
        }
        IntPtr MouseHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_ENTERSIZEMOVE = 0x0231;
            const int WM_EXITSIZEMOVE = 0x0232;

            // Левая кнопка мыши была нажата в области заголовка или изменения размера окна
            if (msg == WM_ENTERSIZEMOVE)
            {
            }

            // Левая кнопка мыши была отпущена в области заголовка или изменения размера окна
            if (msg == WM_EXITSIZEMOVE)
            {
                position.Set("MainWindow.Width",  (int)Width);
                position.Set("MainWindow.Height", (int)Height);
                position.Set("MainWindow.Left",   (int)Left);
                position.Set("MainWindow.Top",    (int)Top);

                G.Pattern.FoundLine();
            }

            return IntPtr.Zero;
        }
    }
}


