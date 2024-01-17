using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        public static List<dynamic> CANDLES_DATA { get; set; }      // Список свечей выбранного инструмента
        public static List<dynamic> CANDLES_TF1 { get; set; }       // Список свечей выбранного инструмента: таймфрейма 1m
        public static bool CANDLES_TF1_USE { get; set; }            // Флаг испльзования таймфрейма 1m
        public static bool IS_CANDLE_FULL { get; private set; }     // Свеча сформирована (всегда, если используется таймфрейм 1m)

        public static bool TESTER_AUTO { get; set; }                // Флаг запуска/остановки автоматического тестирования
        public static bool TESTER_FINISHED { get; set; }            // Флаг завершения тестирования (когда закончились свечи)




        /// <summary>
        /// Инициализация данных для тестера
        /// </summary>
        public static void TESTER_GLOBAL_INIT()
        {
            IS_TESTER = true;
            LogClear();
            CandleClear();
            OrderClear();
            TESTER_AUTO = false;
            TESTER_FINISHED = false;
            IS_CANDLE_FULL = !CANDLES_TF1_USE;
        }

        public static bool TESTER_GLOBAL_STEP()
        {
            if (TESTER_FINISHED)
                return false;

            CandleNew();

            return true;
        }





        /// <summary>
        /// Очистка данных первой свечи и текущих свечных данных
        /// </summary>
        static void CandleClear()
        {
            UNIX = 0;
            PRICE = 0;
            HIGH = 0;
            OPEN = 0;
            CLOSE = 0;
            LOW = 0;
            VOLUME = 0;

            CANDLES = new List<dynamic>();

            // Инициализация графика свечных данных 1m
            TF1i = 0;
        }

        /// <summary>
        /// Добавление очередной свечи к текущему графику
        /// </summary>
        static void CandleNew()
        {
            if (CandleNewTF1())
                return;

            CANDLES.Insert(0, CANDLES_DATA[TF1i++]);
            CandleGlobalSet();

            TESTER_FINISHED = TF1i >= INSTRUMENT.RowsCount;
        }


        static int TF1i { get; set; }  // Индекс свечных данных CANDLES_TF1
        /// <summary>
        /// Формирование свечи из минутного таймфрейма
        /// </summary>
        static bool CandleNewTF1()
        {
            if (!CANDLES_TF1_USE)
                return false;

            dynamic tf1 = CANDLES_TF1[TF1i++];

            if (CANDLES.Count == 0 || !CANDLES[0].Upd(tf1))
                CANDLES.Insert(0, tf1.Clone(INSTRUMENT.TimeFrame));

            CandleGlobalSet();

            IS_CANDLE_FULL = CANDLES[0].IsFull;
            TESTER_FINISHED = TF1i >= CANDLES_TF1.Count;

            return true;
        }
    }
}
