using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using RobotLib;

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
        public static string DATE_TIME => format.DTimeFromUnix(UNIX);

        
        static bool IS_TESTER { get; set; }                     // Робот запущен с тестера


        public static dynamic INSTRUMENT { get; set; }          // Текущий инструмент
        public static List<object> ORDERS { get; private set; } // Список ордеров



        static dynamic CANDLE_CURRENT { get; set; }     // Текущая свеча, которая формируется в данный момент из TF1
        static int CANDLES_INDEX { get; set; }  // Индекс свечных данных
        public static int CANDLES_COUNT => CANDLES_INDEX + 1;
        public static dynamic CANDLES(int index = 0)
        {
            if (CANDLES_TF1_USE && index == 0)
                return CANDLE_CURRENT;

            int i = CANDLES_INDEX - index;

            if(i < 0)
                return null;
            if (CANDLES_DATA == null)
                return null;

            return CANDLES_DATA[i];
        }



        /// <summary>
        /// Установка глобальных значений свечи
        /// </summary>
        static void CandleGlobalSet()
        {
            if (CANDLES() == null)
                return;

            UNIX   = CANDLES().Unix;
            PRICE  = CANDLES().Close;
            HIGH   = CANDLES().High;
            OPEN   = CANDLES().Open;
            CLOSE  = CANDLES().Close;
            LOW    = CANDLES().Low;
            VOLUME = CANDLES().Volume;
        }


        /// <summary>
        /// Единица данных для настроки робота в выпадающем списке (внешние данные)
        /// </summary>
        public class SETUP
        {
            public static List<SETUP> Items = new List<SETUP>();
            public static void Init() => Items.Clear();

            public SETUP(string label1 = "") => Label1 = label1;

            /// <summary>
            /// Вид настройки: Text, Slider, Check
            /// </summary>
            string Type { get; set; }




            /// <summary>
            /// Описание настройки
            /// </summary>
            public string Label1 { get; private set; } = "";
            public string Label1Vis => Label1.Length > 0 ? "Visible" : "Hidden";



            /// <summary>
            /// Вид Slider
            /// </summary>
            public int Slider(int min = 0, int max = 10, int step = 1, int val = 0)
            {
                Type = "Slider";
                SliderMin = min;
                SliderMax = max;
                SliderStep = step;
                SliderValue = val;
                Items.Add(this);
                return val;
            }
            public string SliderVis { get => Type == "Slider" ? "Visible" : "Collapsed"; }
            public int SliderMin { get; set; } = 0;
            public int SliderMax { get; set; } = 10;
            public int SliderStep { get; set; } = 1;
            public int SliderValue { get; set; } = 0;



            public string Text(string txt, string label3 = "")
            {
                Type = "Text";
                TextValue = txt;
                Label3 = label3;
                Items.Add(this);
                return txt;
            }
            public int Text(int val, string label3 = "") => Convert.ToInt32(Text(val.ToString(), label3));
            public string TextVis { get => Type == "Text" ? "Visible" : "Collapsed"; }
            public string TextValue { get; set; } = "";



            public bool Check(string txt, bool isChecked = false)
            {
                Type = "Check";
                CheckTxt = txt;
                IsChecked = isChecked;
                Items.Add(this);
                return isChecked;
            }
            public string CheckVis { get => Type == "Check" ? "Visible" : "Collapsed"; }
            public string CheckTxt { get; set; }
            public bool IsChecked { get; set; }



            public string Label3 { get; private set; } = "";
            public string Label3Vis { get => Label3.Length > 0 ? "Visible" : "Hidden"; }
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
            public static string Show()
            {
                var list = new List<string>();
                foreach (int key in tfAss.Keys)
                    if (tfAss[key])
                        list.Add(key.ToString());

                return string.Join(",", list.ToArray());
            }
        }


        /// <summary>
        /// Класс с данными о найденных паттернах
        /// </summary>
        public class PATTERN
        {
            public delegate void Compare(dynamic src, dynamic dst);
            public static Compare TradeCompare;

            static List<object> All;    // Весь список найденных паттернов
            public static int Count { get { return All.Count; } }   // Общее количество найденных паттернов


            public static int CdiId { get; private set; }
            public static string Symbol { get; private set; }
            public static int TimeFrame { get; private set; }
            public static string TF { get; private set; }
            public static int Length { get; set; }
            public static int PrecisionPercent { get; set; }


            // Исходный паттерн, по которому будет производиться поиск
            static dynamic SRC { get; set; }
            // Свечи, из которых будут формироваться паттерны для сравнения
            static List<dynamic> CandleList { get; set; }
            // Установка исходного паттерна
            public static void Source(dynamic patt)
            {
                SRC = patt;
                Symbol = SRC.Name;
                TimeFrame = SRC.TimeFrame;
                TF = SRC.TF;
                Length = SRC.Length;
                PrecisionPercent = SRC.PrecisionPercent;
                CandleList = new List<dynamic>();
            }







            // ----==== РАЗДЕЛ TESTER ====---- ----------------------------------------
            public PATTERN(List<object> all)
            {
                All = all;
                SRC = null;
                CdiId = INSTRUMENT.CdiId;
                Symbol = INSTRUMENT.Name;
                TimeFrame = INSTRUMENT.TimeFrame;
                TF = INSTRUMENT.TF;
                Length = 0;
                PrecisionPercent = 0;
            }
            // Список паттернов по инструменту [и таймфрейму], которые ещё не тестировались
            public static List<object> NoTestedList()
            {
                var send = new List<object>();

                foreach (dynamic item in All)
                {
                    if (item.CdiId != CdiId)
                        continue;
                    if (item.IsTested)
                        continue;

                    send.Add(item);
                }

                return send;
            }
            // Поиск паттерна
            public static bool TesterFound()
            {
                if (SRC == null)
                    return false;
                if (!IS_CANDLE_FULL)
                    return false;

                CandleList.Add(CANDLES());

                if (CandleList.Count < SRC.Length)
                    return false;

                if (CandleList.Count > SRC.Length)
                    CandleList.RemoveRange(0, 1);

                var dst = SRC.Create(CandleList, SRC.CdiId, PrecisionPercent);
                if (dst.Size == 0)
                    return false;

                return SRC.Compare(dst);
            }
            // Сохранение результатов поиска в базу
            public static void TesterSave(int profit, int loss)
            {
                SRC.ProfitCount = profit;
                SRC.LossCount = loss;
                SRC.TesterSave();
            }





            // ----==== РАЗДЕЛ TRADE ====---- ----------------------------------------
            public PATTERN(List<object> all, bool isTrade)
            {
                All = all;
                SRC = null;
                Length = 0;
                PrecisionPercent = 0;
            }
            // Список всех прибыльных паттернов, которые прошли тест
            public static List<object> ProfitList()
            {
                var send = new List<object>();

                foreach (dynamic item in All)
                {
                    if (!item.IsTested)
                        continue;
                    if (item.ProfitCount <= item.LossCount)
                        continue;

                    send.Add(item);
                }

                return send;
            }
            // Поиск паттерна
            public static bool TradeFound(dynamic patt, List<dynamic> list)
            {
                Source(patt);

                if (!CANDLE_NEW.TF(TimeFrame))
                    return false;
                if (list.Count < Length)
                    return false;

                for (int i = 0; i < Length; i++)
                    CandleList.Add(list[i]);

                var dst = SRC.Create(CandleList, SRC.CdiId, PrecisionPercent);
                if (dst.Size == 0)
                    return false;

                 TradeCompare?.Invoke(SRC, dst);

                return SRC.Compare(dst);
            }
        }
    }
}
