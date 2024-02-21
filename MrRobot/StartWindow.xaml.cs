using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading.Tasks;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Section;
using MrRobot.Connector;
using System.Threading;

namespace MrRobot
{
	public partial class StartWindow : Window
	{
		public StartWindow()
		{
			new G();
			DataContext = new MWsizeDC();
			WindowState = MWsizeDC.State;
			InitializeComponent();	StartLog($"Инициализация стартового окна...");
			MainMenu.Changed += SWclose;
			LoadProcess();
		}

		async void SWclose()
		{
			await Task.Run(() => Thread.Sleep(500));
			Close();
			MainMenu.Changed -= SWclose;
		}

		void StartLog(string msg, bool isFinish = false) =>
			StartLogBox.Items.Add(new StartLogUnit(msg, isFinish));

		async void LoadProcess()
		{
			await Task.Run(AppExceptionLog);StartLog("Подключение событий для необработанных исключений...");
			await Task.Run(Control_Mysqld);	StartLog("Проверка наличия сервера Базы данных...");
			G.CefSettings();				StartLog("Настройка встроенного Веб-браузера");

			await Task.Run(() => new position());	StartLog("Загрузка параметров приложения");
			await Task.Run(() => new Exchange());	StartLog("Загрузка списка Бирж");
			await Task.Run(() => new Candle());		StartLog("Загрузка информации о Свечных данных");
			await Task.Run(() => new Robots());		StartLog("Загрузка Роботов");
			await Task.Run(() => new BYBIT());		StartLog("Загрузка данных биржи ByBit");
			await Task.Run(() => new MOEX());		StartLog("Загрузка данных МосБиржи");
			await Task.Run(() => new CDIpanel());	StartLog("Инициализация выпадающего списка для выбора Свечных данных");
			await Task.Run(() => new HttpServer());	StartLog("Запуск Http-сервера");
			
			G.MW = new MainWindow();StartLog("Инициализация главного окна");
			G.MW.Show();

			G.History.Init();		StartLog("Секция History: инициализация...");
			G.Converter.Init();		StartLog("Секция Converter: инициализация...");
			G.Pattern.Init();		StartLog("Секция Pattern: инициализация...");
			G.Tester.TesterInit();	StartLog("Секция Tester: инициализация...");
			G.Trade.TradeInit();	StartLog("Секция Trade: инициализация...");
			G.Setting.Init();		StartLog("Секция Setting: инициализация...");
			G.LogFile.Init();		StartLog("Секция LogFile: инициализация...");
			G.Manual.Init();		StartLog("Секция Manual: инициализация...");

			G.ISPanel.Init();		StartLog("Инициализация выпадающего списка для выбора Инструментов...");

			new MainMenu();			StartLog("Создание кнопок главного меню...");

			G.MW.SizeChanged += Depth.SizeChanged;
			G.MW.SizeChanged += G.Tester.RobotLogWidthSet;
			G.MW.SizeChanged += MWsizeDC.WindowState;
			G.MW.Closed += HttpServer.Stop;
			StartLog("Установка событий для главного окна...");
			StartLog("Общее время загрузки:", true);
		}



		/// <summary>
		/// События для необработанных исключений
		/// </summary>
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
	}


	public class StartLogUnit
	{
		public StartLogUnit(string msg, bool isfinish)
		{
			Msg = msg;

			if (IsFinish = isfinish)
			{
				Msec = G.dur.Second();
				G.LogWrite($"Загружено за {Msec} сек.");
				return;
			}

			long ms = G.dur.ElapsedMS;
			var ts = TimeSpan.FromMilliseconds(ms - MsecLast);
			MsecLast = ms;

			if(ts.Seconds == 0)
				Msec = $"{ts.Milliseconds}";
			else
				Msec = string.Format("{0}:{1:000}", ts.Seconds, ts.Milliseconds);
		}
		bool IsFinish { get; set; }
		public SolidColorBrush BG => format.RGB(IsFinish ? "#DDDDDD" : "DDFFDD");
		public string Msg { get; set; }
		public string Msec { get; set; }
		public static long MsecLast { get; set; }
		public string MsecWeight => IsFinish ? "Medium" : "Normal";
	}
}
