using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
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


            new Market();
            new Instrument();
            new Candle();
            new Patterns();
            new Robots();
            new CDIpanel();

            InitializeComponent();

            // Разрешение экрана
            //Rect scr = SystemParameters.WorkArea;
            //WriteLine("scr.Width = " + scr.Width);
            //WriteLine("scr.Height = " + scr.Height);

            Width  = position.Val("MainWindow.Width", 1366);
            Height = position.Val("MainWindow.Height", 700);
            Left   = position.Val("MainWindow.Left",   100);
            Top    = position.Val("MainWindow.Top",    100);


            
            MouseLeftButtonDown += error.Clear;      // Очистка нижней строки приложения
            SizeChanged += Tester.RobotLogWidthSet;
            SizeChanged += Depth.SizeChanged;

            Loaded += (s, e) =>
            {
                Trade.InstrumentSelect();

                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd).AddHook(MouseHook);
            };


            global.Inited();
            global.LogWrite($"Загружено за {dur.Second()} сек.");
        }








        void AppLoadControl()
        {
            global.LogWrite();
            global.LogWrite("MrRobot загружается...");

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
            global.LogWrite($"Необработанное исключение: {e.ExceptionObject}", "error.txt");
        }
        static void FirstChanceExceptionEventHandler(object sender, FirstChanceExceptionEventArgs e)
        {
            global.LogWrite($"Обработанное исключение: {e.Exception}", "error.txt");
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

            global.LogWrite($"Запуск процесса `{ProcessName}`...");
            var mysqld = Process.Start(Path.GetFullPath($"mysql\\server\\bin\\{ProcessName}.exe"));
            global.LogWrite($"Процесс `{ProcessName}` запущен. ID: {mysqld.Id}");
            //Environment.Exit(0);
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

                global.MW.Pattern.FoundLine();
            }

            return IntPtr.Zero;
        }
    }
}
