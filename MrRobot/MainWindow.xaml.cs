using System;
using System.IO;
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
			DataContext = new MWsizeDC();
			WindowState = MWsizeDC.State;

			InitializeComponent();
			Loaded += MouseHookInit;
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
				MWsizeDC.Upd($"{Width} {Height} {Left} {Top}", true);
				G.Pattern.FoundLine();
			}

			return IntPtr.Zero;
		}
	}

	// Положение и размеры окна через DataContext
	class MWsizeDC
	{
		public MWsizeDC()
		{
			if (Arr != null)
				return;
			if (!File.Exists(FileName))
				 File.Create(FileName);
			Upd(File.ReadAllText(FileName));
		}

		static string FileName => "MainWindowSize.txt";
		public static void Upd(string size, bool isSave = false)
		{
			if (size == null || size.Length == 0)
				size = "1200 700 100 100 0";
			if (isSave)
				size += $" {Arr[4]}";
			Arr = Array.ConvertAll(size.Split(' '), x => int.Parse(x));
			SizeSave(isSave);
		}
		static void SizeSave(bool isSave)
		{
			if (!isSave)
				return;
			File.WriteAllText(FileName, $"{Width} {Height} {Left} {Top} {Arr[4]}");
		}
		static int[] Arr { get; set; }
		public static int Width  => Arr[0];
		public static int Height => Arr[1];
		public static int Left   => Arr[2];
		public static int Top    => Arr[3];
		public static string Name { get; set; }
		// Вовесь экран
		public static WindowState State =>
			Arr[4] == 1
				? System.Windows.WindowState.Maximized
				: System.Windows.WindowState.Normal;
		public static void WindowState(object s, SizeChangedEventArgs e)
		{
			Arr[4] = G.MW.WindowState == System.Windows.WindowState.Maximized ? 1 : 0;
			SizeSave(true);
		}
	}
}


