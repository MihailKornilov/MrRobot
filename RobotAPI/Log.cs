using System;
using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        /// <summary>
        /// Отображение сообщений от Робота в логе Тестера
        /// </summary>
        public static void LOG(string txt) => new LOGG(txt);
        public static void LOGSTATIC(string txt) => new LOGG(txt, true);
        public class LOGG
        {
            // Инициализация лога
            public LOGG()
            {
                Method = null;
                LogList = new List<LogUnit>();

                Stat = null;
                StatList = new List<string>();
            }



            public delegate void Call(List<LogUnit> list);
            public static Call Method;
            static List<LogUnit> LogList;
            // Добавление новой записи в обычный лог
            public LOGG(string txt) => LogList.Add(new LogUnit(txt));
            public static void Output()
            {
                StatOut();

                if (LogList.Count == 0)
                    return;

                Method(LogList);
                LogList.Clear();
            }





            public delegate void CallStat(string txt);
            public static CallStat Stat;
            static List<string> StatList;
            // Добавление новой записи в статический лог
            public LOGG(string txt, bool isStatic) => StatList.Add(txt);
            public static void StatOut()
            {
                if (StatList.Count == 0)
                    return;
                if (Stat == null)
                    return;

                StatList.Insert(0, DateTime.Now.ToString());
                Stat(string.Join("\n", StatList.ToArray()));
                StatList.Clear();
            }
        }
    }


    /// <summary>
    /// Шаблон для лога робота
    /// </summary>
    public class LogUnit
    {
        public string Text { get; set; }                // Содержание лога
        public string DTime { get; private set; }       // Текущая дата и время (момент, в который выводится запись)
        public string CandleTime { get; private set; }  // Дата и время свечи графика

        public LogUnit(string txt)
        {
            Text = txt;
            DTime = DateTime.Now.ToString();
            CandleTime = Robot.UNIX == 0 ? "Init" : Robot.DATE_TIME;
            if (Robot.TESTER_FINISHED)
                CandleTime = "Finish";
        }
    }
}
