using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using static System.Console;

using MrRobot.Section;

namespace MrRobot.inc
{
    public class global
    {
        /// <summary>
        /// Флаги инициализации страниц и всего приложения
        /// </summary>
        public static bool[] InitPage { get; set; } = new bool[7];
        private static string[] PageName = new string[7]
        {
            "GLOBAL",
            "HISTORY",
            "CONVERTER",
            "PATTERN",
            "TESTER",
            "TRARE",
            ""
        };
        public static bool IsInited(int page = 0)
        {
            if (InitPage[page])
                return true;
            if (page > 0 && position.MainMenu() != page)
                return true;

            return false;
        }
        public static void Inited(int page = 0)
        {
            InitPage[page] = true;
            string pg = page > 0 ? page + "." : "";
            WriteLine(pg + PageName[page] + " inited");
        }


        public static MainWindow MW = Application.Current.Windows[0] as MainWindow;


        // Флаг запущенного АвтоПрогона
        public static bool IsAutoProgon
        {
            get { return AutoProgon.Active; }
        }

        /// <summary>
        /// Запись данных в log-файл
        /// </summary>
        public static void LogWrite(string txt = "", string fileName = "log.txt")
        {
            FileStream file;
            try
            {
                file = new FileStream(fileName, FileMode.OpenOrCreate);
            }
            catch
            {
                return;
            }
            txt = txt.Length > 0 ? $"{format.DTimeNow()}: {txt}\n" : file.Length == 0 ? "" : "\n";
            byte[] buffer = Encoding.Default.GetBytes(txt);
            file.Seek(0, SeekOrigin.End);
            file.Write(buffer, 0, buffer.Length);
            file.Close();
        }
    }




    /// <summary>
    /// Измерение скорости выполнения участка программы
    /// </summary>
    public class Dur
    {
        private Stopwatch SW;
        public Dur()
        {
            SW = new Stopwatch();
            SW.Start();
        }
        public string Minutes()
        {
            SW.Stop();
            TimeSpan ts = SW.Elapsed;

            if(ts.Hours > 0)
                return string.Format("{0}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
        public string Second()
        {
            SW.Stop();
            TimeSpan ts = SW.Elapsed;
            return string.Format("{0}:{1:000}", ts.Seconds, ts.Milliseconds);
        }
        /// <summary>
        /// Вывод результата в секундах и миллисекундах в Консоль
        /// </summary>
        public void SecondCWL(string msg="")
        {
            WriteLine(msg + Second());
        }

        public int Elapsed()
        {
            return Convert.ToInt32(SW.ElapsedMilliseconds / 1000);
        }
        public long ElapsedMS()
        {
            return SW.ElapsedMilliseconds;
        }
    }




    /// <summary>
    /// Статический класс для вывода сообщений об ошибке в нижней строке приложения
    /// </summary>
    public static class error
    {
        /// <summary>
        /// Показ сообщения об ошибке в нижней строке приложения
        /// </summary>
        public static void Msg(string txt)
        {
            global.MW.MainLineBottom.Text = txt;
            global.MW.MainLineBottom.Background = new SolidColorBrush(Color.FromArgb(255, 0xFF, 0xAA, 0xAA));  // FAA
            global.MW.MainLineBottom.Foreground = new SolidColorBrush(Color.FromArgb(255, 0xCC, 0x00, 0x00));  // C00
        }

        /// <summary>
        /// Очистка нижней строки приложения
        /// </summary>
        public static void Clear(object sender, MouseButtonEventArgs e)
        {
            if (global.MW.MainLineBottom.Text.Length == 0)
                return;

            global.MW.MainLineBottom.Text = "";
            global.MW.MainLineBottom.Background = null;
            global.MW.MainLineBottom.Foreground = null;
        }
    }




    /// <summary>
    /// Обновление значения прогресс-бара
    /// </summary>
    public class ProBar
    {
        long All { get; set; }// Общее количество
        bool IsMore100;     // Флаг: общее количество больше 100
        double Sotka;       // Сотая часть от общей суммы
        double Area;        // Участок, при завершении которого обновляется процент ПрогрессБара
        double Percent;     // Значение в процентах, которое будет передаваться в Value
        double PercentStep; // Значение, на которое увеличивается процент при следующем шаге
        public int Value { get; private set; }  // Значение в процентах, которое будет выводиться в Прогресс-бар

        Dur dur;
        double MilliSecondPass;
        double MilliSecondLeft;
        public string TimePass {
            get
            {
                var pass = TimeSpan.FromMilliseconds(MilliSecondPass);
                if (pass.Hours > 0)
                    return string.Format("{0}:{1:00}:{2:00}", pass.Hours, pass.Minutes, pass.Seconds);

                return string.Format("{0:00}:{1:00}", pass.Minutes, pass.Seconds);
            }
        }
        public string TimeLeft {
            get
            {
                var left = TimeSpan.FromMilliseconds(MilliSecondLeft);
                if (left.Hours > 0)
                    return string.Format("{0}:{1:00}:{2:00}", left.Hours, left.Minutes, left.Seconds);

                return string.Format("{0:00}:{1:00}", left.Minutes, left.Seconds);
            }
        }

        public ProBar(long all)
        {
            All = all;
            IsMore100 = all > 100;
            InitMore100();
            InitLess100();

            dur = new Dur();
            MilliSecondPass = 0;
            MilliSecondLeft = 0;
        }

        /// <summary>
        /// Инициализация при общем количестве больше 100
        /// </summary>
        void InitMore100()
        {
            if (!IsMore100)
                return;

            Sotka = (double)All / 100;
            Area = Sotka;
            Percent = 1;
        }
        /// <summary>
        /// Инициализация при общем количестве меньше 100
        /// </summary>
        void InitLess100()
        {
            if (IsMore100)
                return;

            Sotka = 100 / (double)All;
            Percent = Sotka;
            PercentStep = Sotka;
        }

        /// <summary>
        /// Проверка, обновлять ли Прогресс-бар (при All > 100)
        /// </summary>
        public bool isUpd(long count)
        {
            if (count < Area)
                return false;

            Area += Sotka;
            Value = Convert.ToInt32(Percent);
            Percent += 1;

            MilliSecondPass = dur.ElapsedMS();
            MilliSecondLeft = MilliSecondPass / count * All - MilliSecondPass;

            return true;
        }
        /// <summary>
        /// Отправка значения Прогресс-бару (при All < 100)
        /// </summary>
        public int Val()
        {
            double ceil = Math.Ceiling(Percent);
            Value = Convert.ToInt32(ceil);
            Percent += PercentStep;
            return Value;
        }
    }
}
