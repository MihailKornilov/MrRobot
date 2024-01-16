using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace RobotAPI
{
    public static partial class Robot
    {
        public static double PRICE  { get; private set; }
        public static double HIGH   { get; private set; }
        public static double OPEN   { get; private set; }
        public static double CLOSE  { get; private set; }
        public static double LOW    { get; private set; }
        public static double VOLUME { get; private set; }
        public static int UNIX      { get; private set; }
        public static string DATE_TIME { get { return format.DTimeFromUnix(UNIX); } }


        public static dynamic INSTRUMENT { get; set; }          // Текущий инструмент
        public static List<dynamic> CANDLES { get; set; }       // Текущий список свечей
        public static List<object> ORDERS { get; private set; } // Список ордеров
    

        static bool IS_TESTER { get; set; }                     // Робот запущен с тестера


        /// <summary>
        /// Установка глобальных значений свечи
        /// </summary>
        static void CandleGlobalSet()
        {
            UNIX = CANDLES[0].Unix;
            PRICE = CANDLES[0].Close;
            HIGH = CANDLES[0].High;
            OPEN = CANDLES[0].Open;
            CLOSE = CANDLES[0].Close;
            LOW = CANDLES[0].Low;
            VOLUME = CANDLES[0].Volume;
        }


        /// <summary>
        /// Класс с информацией о появлении новой свечи по каждому таймфрейму
        /// </summary>
        public class CANDLE_NEW
        {
            /// <summary>
            /// Ассоциативный массив таймфреймов и флагов новый свечей
            /// </summary>
            static Dictionary<int, bool> tfAss;

            /// <summary>
            /// Инициализация ассоциативного массива таймфреймов
            /// </summary>
            public CANDLE_NEW(Dictionary<int, string> ass)
            {
                if(tfAss == null)
                {
                    tfAss = new Dictionary<int, bool>();
                    foreach (int key in ass.Keys)
                        tfAss.Add(key, false);
                }

                Update();
            }

            /// <summary>
            /// Обновление флагов таймфреймов каждую минуту
            /// </summary>
            public static void Update()
            {
                WriteLine();
                WriteLine("MSec: " + DateTime.Now.Millisecond);
                var now = DateTime.UtcNow;
                int minute = now.Hour * 60 + now.Minute;
                for (int i = 0; i < tfAss.Count; i++)
                {
                    int key = tfAss.ElementAt(i).Key;
                    tfAss[key] = minute % key == 0;
                }
            }

            /// <summary>
            /// Получение информации о новой свече по конкретному таймфрейму
            /// </summary>
            public static bool TF(int v)
            {
                if(!tfAss.ContainsKey(v))
                    return false;

                return tfAss[v];
            }

            /// <summary>
            /// Показ таймфреймов, у который появилась новая свеча
            /// </summary>
            public static void Show()
            {
                var list = new List<string>();
                foreach (int key in tfAss.Keys)
                    if (tfAss[key])
                        list.Add(key.ToString());

                string tfs = string.Join(",", list.ToArray());
                WriteLine(format.TimeNow() + "  " + tfs);
            }
        }
    }
}
