using System;
using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        /// <summary>
        /// Отображение сообщений от Робота в логе Тестера
        /// </summary>
        static List<LogUnit> LogList = new List<LogUnit>();
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
        static void LogClear()
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
            DTime = DateTime.Now.ToString();
            CandleTime = Robot.UNIX == 0 ? "Init" : Robot.DATE_TIME;
            if (Robot.TESTER_FINISHED)
                CandleTime = "Finish";
        }

        public string Text { get; set; }                // Содержание лога
        public string DTime { get; private set; }       // Текущая дата и время (момент, в который выводится запись)
        public string CandleTime { get; private set; }  // Дата и время свечи графика

    }
}
