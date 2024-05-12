using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Console;

using RobotLib;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Section;

namespace MrRobot
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			new G(this);

			DataContext = new MWsizeDC();
			WindowState = MWsizeDC.State;

			InitializeComponent();
			LoadProcess();
		}


		void Msg(string v) => LogLoad.Prgs.Report(v);

		// Вставка главных разделов
		void Section(UserControl sect)
		{
			G.Hid(sect);
			G.Grid1.Add(sect);
		}

		async void LoadProcess()
		{
			new LogLoad();		await Task.Run(() => Msg("Инициализация главного окна..."));

			AppExceptionLog();	await Task.Run(() => Msg("Подключение событий для необработанных исключений..."));
			Control_Mysqld();	await Task.Run(() => Msg("Проверка наличия сервера Базы данных..."));
			G.CefSettings();	await Task.Run(() => Msg("Настройка встроенного Веб-браузера..."));

			new my();			await Task.Run(() => Msg("Подключение к базе данных..."));
			new position();		await Task.Run(() => Msg("Загрузка параметров приложения"));
			my.IS_LOG = position.Val("6.1.SqlLog", false);
			new Exchange();		await Task.Run(() => Msg("Загрузка списка Бирж"));
			new Candle();		await Task.Run(() => Msg("Загрузка информации о Свечных данных"));
			new Robots();		await Task.Run(() => Msg("Загрузка Роботов"));
			new HttpServer();	await Task.Run(() => Msg("Запуск Http-сервера"));

			//await Task.Run(() => new CDIpanel()); StartLog("Инициализация выпадающего списка для выбора Свечных данных");
			//G.ISPanel.Init(); StartLog("Инициализация выпадающего списка для выбора Инструментов...");


			Section(new History());		await Task.Run(() => Msg("Секция History: инициализация..."));
			Section(new Converter());	await Task.Run(() => Msg("Секция Converter: инициализация..."));
			Section(new Pattern());		await Task.Run(() => Msg("Секция Pattern: инициализация..."));
			Section(new Setting());		await Task.Run(() => Msg("Секция Setting: инициализация..."));
			Section(new Tester());		await Task.Run(() => Msg("Секция Tester: инициализация..."));
			Section(new Trade());		await Task.Run(() => Msg("Секция Trade: инициализация..."));
			Section(new LogFile());		await Task.Run(() => Msg("Секция LogFile: инициализация..."));
			Section(new Manual());		await Task.Run(() => Msg("Секция Manual: инициализация..."));

			new MainMenu();				await Task.Run(() => Msg("Создание кнопок главного меню..."));

			//G.MW.SizeChanged += Depth.SizeChanged;
			//G.MW.SizeChanged += G.Tester.RobotLogWidthSet;
			SizeChanged += MWsizeDC.WindowState;
			Loaded += MouseHookInit;
			Closed += HttpServer.Stop;
			Closed += my.Close;
			await Task.Run(() => Msg("Установка событий для главного окна..."));

			LogLoad.Finish(); await Task.Run(() => Msg("Общее время загрузки:"));
		}










		// Отслеживание изменения размера и перетаскивания экрана (при удежнании кнопки мыши)
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

		// События для необработанных исключений
		void AppExceptionLog()
		{
			AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
				G.LogWrite($"Обработанное исключение: {e.Exception}", "error.txt");

			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
				G.LogWrite($"Необработанное исключение: {e.ExceptionObject}", "error.txt");
		}

		// Проверка запущен ли сервер mysqld
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


	// Лог загрузки приложения
	public class LogLoad
	{
		static ListBox LB;
		public LogLoad()
		{
			LB = new ListBox();
			LB.HorizontalAlignment = HorizontalAlignment.Left;
			LB.VerticalAlignment = VerticalAlignment.Top;
			LB.Margin = new Thickness(20, 20, 0, 0);
			LB.Width = 460;
			LB.Height = 400;
			LB.Background = format.RGB("#FFFFF0");
			LB.BorderBrush = format.RGB("#CCCC88");
			Panel.SetZIndex(LB, 10);
			Grid.SetColumn(LB, 1);
			G.MainGrid.Add(LB);

			var back = new Back();
			back.Method += () => G.MainGrid.Remove(LB);
		}



		// Время выполнения очередного шага
		static long MsecLast { get; set; }
		static string Msec()
		{
			if(IsFinish)
				return G.dur.Second();

			long ms = G.dur.ElapsedMS;
			var ts = TimeSpan.FromMilliseconds(ms - MsecLast);
			MsecLast = ms;

			if (ts.Seconds == 0)
				return $"{ts.Milliseconds} ms";

			return string.Format("{0}:{1:000} ms", ts.Seconds, ts.Milliseconds);
		}
		public static IProgress<string> Prgs = new Progress<string>(msg => LBitem(msg));
		// Вставка очередной строки лога
		static void LBitem(string msg)
		{
			var panel = new WrapPanel();
			var label = new Label();
			label.Content = msg;
			label.Padding = new Thickness(0);
			label.Width = 370;
			label.FontWeight = IsFinish ? FontWeights.Medium : FontWeights.Normal;
			panel.Children.Add(label);

			label = new Label();
			label.Content = Msec();
			label.Padding = new Thickness(0);
			label.Width = 70;
			label.HorizontalContentAlignment = HorizontalAlignment.Right;
			label.FontWeight = IsFinish ? FontWeights.Medium : FontWeights.Normal;
			panel.Children.Add(label);

			LB.Items.Add(panel);
		}


		static bool IsFinish { get; set; } = false;
		public static void Finish()
		{
			G.LogWrite($"Загружено за {G.dur.Second()} сек.");
			IsFinish = true;
		}
	}
}


