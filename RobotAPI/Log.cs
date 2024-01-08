using System;
using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        /// <summary>
        /// Отображение сообщений от Робота в логе Тестера
        /// </summary>
        private static List<LogUnit> LogList = new List<LogUnit>();
        public static void LOG(string Text)
        {
            LogList.Add(new LogUnit(Text));
        }
        public static List<LogUnit> LOG_LIST()
        {
            var tmpList = LogList;
            LogClear();
            return tmpList;
        }
        private static void LogClear()
        {
            LogList = new List<LogUnit>();
        }
    }


    /// <summary>
    /// Шаблон для лога робота
    /// </summary>
    public class LogUnit
    {
        public LogUnit(string txt)
        {
            Text = txt;
        }

        // Текущая дата и время (момент, в который выводится запись)
        private string _DTime;
        public string DTime
        {
            get { return _DTime; }
            set { _DTime = DateTime.Now.ToString(); }
        }

        // Дата и время со свечи графика
        public string _CandleTime;
        public string CandleTime
        {
            get { return _CandleTime; }
            set { _CandleTime = Robot.DATE_TIME; }
        }

        public string Text { get; set; }
    }
}
