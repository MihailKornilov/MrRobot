using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using CefSharp;
using CefSharp.Wpf;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Section;
using MrRobot.Section.Trade;
using System.Runtime.ExceptionServices;

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


            //try
            //{
            //    // Установка каталога кеша для Веб-браузера
            //    var Settings = new CefSettings();
            //    Settings.CachePath = Directory.GetCurrentDirectory() + @"\CefSharpCache";
            //    global.LogWrite(Directory.GetCurrentDirectory() + @"\CefSharpCache");
            //    Cef.Initialize(Settings);
            //}
            //catch (Exception ex)
            //{
            //    global.LogWrite($"Не удалось настроить CefSettings: {ex.Message}");
            //    Environment.Exit(0);
            //}


            global.LogWrite("CefSettings прошли.");


            if (Instrument.Count > 0)
                Candle.ListCreate();

            global.LogWrite("Instrument.Count прошли.");

            InitializeComponent();

            global.LogWrite("MAIN InitializeComponent прошли.");



            MainMenuCreate();

            // Очистка нижней строки приложения
            MW.MouseLeftButtonDown += error.Clear;
            MW.Loaded += Trade.InstrumentSet;
            MW.SizeChanged += Tester.RobotLogWidthSet;
            MW.SizeChanged += Depth.SizeChanged;

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
                    Converter.ConverterFindBox.Focus();
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
                    Trade.InstrumentSet();
                    break;
            }
        }


        void MainMenuButtonClick(object sender, RoutedEventArgs e)
        {
            var but = sender as Button;
            MainMenuListBox.SelectedIndex = but.TabIndex - 1;
        }
        void MainMenuChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = MainMenuListBox.SelectedIndex + 1;
            position.MainMenu(index);
            MainMenuSet();
        }









        void AppLoadControl()
        {
            //InnoSetup();

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
        private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            global.LogWrite($"Необработанное исключение: {e.ExceptionObject}");
        }
        private static void FirstChanceExceptionEventHandler(object sender, FirstChanceExceptionEventArgs e)
        {
            global.LogWrite($"Обработанное исключение: {e.Exception}");
        }


        /// <summary>
        /// Проверка запущен ли сервер mysqld
        /// </summary>
        void Control_Mysqld()
        {
            if (Process.GetProcessesByName("mysqld").Length > 0)
                return;

            global.LogWrite("Запуск процесса `mysqld`...");
            var mysqld = Process.Start(Path.GetFullPath("mysql\\server\\bin\\mysqld.exe"));
            global.LogWrite($"Процесс `mysqld` запущен. ID: {mysqld.Id}");
            //Environment.Exit(0);
        }




        void InnoSetup()
        {
            File.Copy("c:\\Work\\mr_robot_script.tmp",
                      "c:\\Work\\mr_robot_script.iss", true);
            InnoDirs("c:\\Work\\MrRobot");
        }
        void InnoDirs(string path, string directory = "")
        {
            InnoFiles(path, directory);
            var dirs = new DirectoryInfo(path).GetDirectories();
            foreach (var dir in dirs)
                InnoDirs($"{path}\\{dir.Name}", $"{directory}\\{dir.Name}");
        }
        void InnoFiles(string path, string dir)
        {
            var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return;
            
            var write = new StreamWriter("c:\\Work\\mr_robot_script.iss", true);
            foreach (var file in files)
                write.WriteLine($"Source: \"{file}\"; DestDir: \"{{app}}{dir}\"; Flags: ignoreversion");
            write.Close();
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
