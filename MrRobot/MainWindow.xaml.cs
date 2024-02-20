using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Diagnostics;
using static System.Console;

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
            G.Settings();

            new position();
			new Exchange();
            new Candle();
            new Robots();
            new CDIpanel();
            new BYBIT();
            new MOEX();
            new HttpServer();

			InitializeComponent();

            DataContext = new MWsizeDC();
			G.MW = this;
            
            Loaded += MouseHookInit;
            Loaded += G.ISPanel.Init;
            Loaded += (s, e) =>
            {
                new MainMenu();
				SizeChanged += Depth.SizeChanged;
				SizeChanged += G.Tester.RobotLogWidthSet;
				SizeChanged += MWsizeDC.WindowState;
			};
            Closed += HttpServer.Stop;

            string txt = $"Загружено за {dur.Second()} сек.";
            G.LogWrite(txt);
             WriteLine(txt);
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
            AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
                G.LogWrite($"Обработанное исключение: {e.Exception}", "error.txt");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                G.LogWrite($"Необработанное исключение: {e.ExceptionObject}", "error.txt");
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


            // Разрешение экрана
            //Rect scr = SystemParameters.WorkArea;
            //WriteLine("scr.Width = " + scr.Width);
            //WriteLine("scr.Height = " + scr.Height);


            // Левая кнопка мыши была отпущена в области заголовка или изменения размера окна
            if (msg == WM_EXITSIZEMOVE)
            {
                MWsizeDC.Width  = (int)Width;
                MWsizeDC.Height = (int)Height;
                MWsizeDC.Left   = (int)Left;
                MWsizeDC.Top    = (int)Top;

                G.Pattern.FoundLine();
            }

            return IntPtr.Zero;
        }
    }

    // Положение и размеры окна через DataContext
    class MWsizeDC
    {
        public static WindowState State
		{
            get => position.Val("MainWindow.Maximized", false)
                    ? System.Windows.WindowState.Maximized
                    : System.Windows.WindowState.Normal;
			set => position.Set("MainWindow.Maximized", value == System.Windows.WindowState.Maximized);
		}
		public static void WindowState(object s, SizeChangedEventArgs e) => State = G.MW.WindowState;

		public static int Width
        {
            get => position.Val("MainWindow.Width", 1366);
            set => position.Set("MainWindow.Width", value);
        }
        public static int Height
        {
            get => position.Val("MainWindow.Height", 700);
            set => position.Set("MainWindow.Height", value);
        }
        public static int Left
        {
            get => position.Val("MainWindow.Left", 100);
            set => position.Set("MainWindow.Left", value);
        }
        public static int Top
        {
            get => position.Val("MainWindow.Top", 100);
            set => position.Set("MainWindow.Top", value);
        }
    }
}


