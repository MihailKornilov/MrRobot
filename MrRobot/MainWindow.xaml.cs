using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
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
            Height = position.Val("MainWindow.Height", 800);
            Left   = position.Val("MainWindow.Left",   100);
            Top    = position.Val("MainWindow.Top",    100);


            
            MouseLeftButtonDown += error.Clear;      // Очистка нижней строки приложения
            SizeChanged += Tester.RobotLogWidthSet;
            SizeChanged += Depth.SizeChanged;

            Loaded += (s, e) =>
            {
                MainMenuCreate();
                Trade.InstrumentSelect();

                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd).AddHook(MouseHook);
            };


            global.Inited();
            global.LogWrite($"Загружено за {dur.Second()} сек.");
        }

        /// <summary>
        /// Список разделов меню
        /// </summary>
        string[] MainMenuSectionName()
        {
            string[] mass = {
                "Global",
                "History",
                "Converter",
                "Pattern",
                "Tester",
                "Trade",
                "Setting"
            };

            return mass;
        }

        /// <summary>
        /// Создание главного меню
        /// </summary>
        void MainMenuCreate()
        {
            var butList = new List<MMUnit>();
            for (int i = 1; i < MainMenuSectionName().Length; i++)
                butList.Add(new MMUnit
                {
                    Index = i,
                    Section = MainMenuSectionName()[i]
                });

            MainMenuListBox.ItemsSource = butList;
            MainMenuListBox.SelectedIndex = position.MainMenu() - 1;
        }


        /// <summary>
        /// Установка раздела Главного меню
        /// </summary>
        void MainMenuSet()
        {
            int index = position.MainMenu();
            string[] section = MainMenuSectionName();
            UserControl sect;
            for (int i = 1; i < section.Length; i++)
            {
                sect = FindName(section[i]) as UserControl;
                sect.Visibility = Visibility.Collapsed;
            }

            sect = FindName(section[index]) as UserControl;
            sect.Visibility = Visibility.Visible;

            CDIpanel.PageChanged();

            switch (index)
            {
                case 1:
                    if (SectionUpd.Update[1])
                        SectionUpd.History();

                    History.HistoryInit();
                    History.InstrumentFindBox.Focus();
                    break;

                case 2:
                    if (SectionUpd.Update[2])
                        SectionUpd.Converter();

                    Converter.ConverterInit();
                    break;

                case 3:
                    if (SectionUpd.Update[3])
                        SectionUpd.Pattern();

                    Pattern.PatternInit();
                    break;

                case 4:
                    if (SectionUpd.Update[4])
                        SectionUpd.Tester();

                    Tester.TesterInit();
                    break;

                case 5:
                    Trade.TradeInit();
                    Trade.InstrumentSelect();
                    break;
            }
        }
        void MainMenuButtonClick(object sender, RoutedEventArgs e)
        {
            var but = sender as Button;
            MainMenuListBox.SelectedIndex = but.TabIndex - 1;
            Pattern.ArchiveGo(true);
        }
        void MainMenuChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = MainMenuListBox.SelectedIndex + 1;
            position.MainMenu(index);
            MainMenuSet();
        }


        //public static Rect GetAbsolutePlacement(this FrameworkElement element, bool relativeToScreen = false)
        //{
        //    var absolutePos = element.PointToScreen(new Point(0, 0));

        //    if (relativeToScreen)
        //        return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);

        //    var posMW = Application.Current.MainWindow.PointToScreen(new Point(0, 0));
        //    absolutePos = new Point(absolutePos.X - posMW.X, absolutePos.Y - posMW.Y);
        //    return new Rect(absolutePos.X, absolutePos.Y, element.ActualWidth, element.ActualHeight);
        //}






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
            }

            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Данные кнопок главного меню
    /// </summary>
    public class MMUnit
    {
        public int Index { get; set; }      // Подярковый индекс
        public string Section { get; set; }    // Имя раздела
        public string Image { get { return $"pack://application:,,,/Resources/images/button-{Section}.png"; } }
        public string ImageOver { get { return $"pack://application:,,,/Resources/images/button-{Section}-over.png"; } }
    }
}
