using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;

namespace RobotAPI
{
    public static partial class Robot
    {
        public static List<dynamic> TRADE_LIST { get; set; } = new List<dynamic>();   // Список сделкок по инструменту
        public static bool IS_CANDLE_NEW { get; private set; }  // Появилась новая свеча


        /// <summary>
        /// Инициализация данных для Торговли
        /// </summary>
        public static void TRADE_GLOBAL_INIT()
        {
            IS_TESTER = false;
            IS_CANDLE_NEW = false;
            CandleGlobalSet(CANDLES[0]);
            TRADE_LIST = new List<dynamic>();
            new CANDLES_ACTUAL();
        }

        /// <summary>
        /// Очередной шаг в Реальной торговле
        /// </summary>
        // 
        public static void TRADE_GLOBAL_STEP()
        {
            IS_CANDLE_NEW = CANDLE_NEW.TF(CANDLES[0].TimeFrame);
            CandleGlobalSet(CANDLES[0]);
        }


        /// <summary>
        /// Актуальные свечные данные по выбранной валютой паре и таймфрейму
        /// В первом вызове производится подписка на Инструмент + Таймфрейм
        /// Перед появлением новой свечи производится обновление данных и выдача роботу (функция Trade:CandlesActualUpdate)
        /// </summary>
        public class CANDLES_ACTUAL
        {
            /// <summary>
            /// Массивы свечных данных, на которые создана подписка
            /// Key в виде BTCUSDT_15
            /// Value: List<CandleUnit>
            /// </summary>
            static Dictionary<string, List<dynamic>> MASS { get; set; }
            /// <summary>
            /// Массивы свечных данных с таймфреймом 1 для каждого инструмента, на который оформлена подписка.
            /// Если инструмент подписан на несколько таймфреймов.
            /// Этот массив берётся с биржи, затем остальные таймфреймы конвертируются с него.
            /// Key в виде BTCUSDT
            /// Value: List<CandleUnit>
            /// </summary>
            static Dictionary<string, List<dynamic>> MASS_TF1 { get; set; }
            /// <summary>
            /// Статический конструктор для инициализации
            /// </summary>
            public CANDLES_ACTUAL()
            {
                MASS = new Dictionary<string, List<dynamic>>();
                MASS_TF1 = new Dictionary<string, List<dynamic>>();
            }
            /// <summary>
            /// Подписка на выбранные Инструменты и таймфреймы
            /// </summary>
            public static List<dynamic> Subscribe(string symbol, int timeFrame)
            {
                string key = symbol.ToUpper() + "_" + timeFrame;
                if (!MASS.ContainsKey(key))
                    MASS.Add(key, new List<dynamic>());

                if (!MASS_TF1.ContainsKey(symbol))
                    MASS_TF1.Add(symbol, new List<dynamic>());

                return MASS[key];
            }

            /// <summary>
            /// Получение списка Инструментов, которые нужно подгрузить в конкретную минуту
            /// </summary>
            public static Dictionary<string, int> SymbolList()
            {
                var ass = new Dictionary<string, int>();
                foreach (string key in MASS.Keys)
                {
                    string[] spl = key.Split('_');

                    string symbol = spl[0];
                    int tf = Convert.ToInt32(spl[1]);

                    if (!CANDLE_NEW.TF(tf))
                        continue;

                    if (!ass.ContainsKey(symbol))
                        ass.Add(symbol, 0);

                    if (MASS_TF1[symbol].Count == 0)
                        continue;

                    int unix = MASS_TF1[symbol][0].Unix;
                    if (ass[symbol] == 0)
                        ass[symbol] = unix;

                    if (ass[symbol] > unix)
                        ass[symbol] = unix;
                }

                foreach (string key in ass.Keys)
                    WriteLine($"{key}: {ass[key]}");

                return ass;
            }

            /// <summary>
            /// Получение времени в формате UNIX для определённого таймфрейма
            /// </summary>
            static int UnixTF(int unix, int tf)
            {
                int MinuteTotal = unix / 60;
                int MinuteDay = MinuteTotal / 1440 * 1440;
                int ost = (MinuteTotal - MinuteDay) % tf;

                return (MinuteTotal - ost) * 60;
            }

            /// <summary>
            /// Пустой список или нет
            /// </summary>
            static bool ListEmpty(ref List<dynamic> list)
            {
                if (list.Count == 0)
                    return true;

                // Определение удаления текущей минуты (несформированной свечи Таймфрейм 1)
                var ux = new DateTime(1970, 1, 1).AddSeconds(list[0].Unix);
                var now = DateTime.UtcNow;
                if (ux.Minute != now.Minute)
                    return false;

                list.RemoveRange(0, 1);

                return list.Count == 0;
            }

            /// <summary>
            /// Сохранение полученных данных для Инструмента Таймфрейм 1
            /// </summary>
            public static void Save(string symbol, List<dynamic> list)
            {
                if (ListEmpty(ref list))
                    return;

                list.Reverse();
                for (int i = 0; i < list.Count; i++)
                    MASS_TF1[symbol].Insert(0, list[i]);

                MASScheck(symbol);
                MASSсonvert(symbol);
            }
            static void MASScheck(string symbol)
            {
                MASS_TF1[symbol].Reverse();
                int UnixNext = MASS_TF1[symbol][0].Unix;
                for(int i = 0; i < MASS_TF1[symbol].Count; i++)
                {
                    if (UnixNext != MASS_TF1[symbol][i].Unix)
                        WriteLine($"----------- Пропущена минута: {symbol}:{UnixNext}");
                    UnixNext += 60;
                }
                MASS_TF1[symbol].Reverse();
            }
            static void MASSсonvert(string symbol)
            {
                for (int i = 0; i < MASS.Count; i++)
                {
                    string key = MASS.ElementAt(i).Key;
                    string[] spl = key.Split('_');

                    if (symbol != spl[0])
                        continue;

                    int tf = Convert.ToInt32(spl[1]);
                    if (!CANDLE_NEW.TF(tf))
                        continue;

                    MASSсonvertTF(symbol, tf);
                }
            }

            /// <summary>
            /// Конвертация свечей в указанный таймфрейм
            /// </summary>
            static void MASSсonvertTF(string symbol, int tf)
            {
                string key = symbol + "_" + tf;

                int iBegin;
                // Наполнение (массив пуст)
                if (MASS[key].Count == 0)
                {
                    // Определение начала первой свечи согласно таймфрейму
                    for (iBegin = MASS_TF1[symbol].Count - 1; iBegin >= 0; iBegin--)
                    {
                        var src = MASS_TF1[symbol][iBegin];
                        if (src.Unix == UnixTF(src.Unix, tf))
                            break;
                    }
                }
                else // Догрузка свечи
                {
                    int unixTF = MASS[key][0].Unix + tf * 60;
                    for (iBegin = 0; iBegin < MASS_TF1[symbol].Count; iBegin++)
                        if (unixTF == MASS_TF1[symbol][iBegin].Unix)
                            break;
                }

                var dst = MASS_TF1[symbol][iBegin--];
                dst.TimeFrame = tf;

                for (int i = iBegin; i >= 0; i--)
                {
                    var src = MASS_TF1[symbol][i];

                    if (dst.Upd(src))
                        continue;

                    MASS[key].Insert(0, dst);
                    dst = src;
                    dst.TimeFrame = tf;
                }

                MASS[key].Insert(0, dst);
            }
        }
    }
}
