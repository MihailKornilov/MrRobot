using System;
using System.Windows;
using System.Windows.Interop;
using static System.Console;

using MrRobot.inc;

namespace MrRobot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void MouseHookInit(object sender, RoutedEventArgs e)
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


