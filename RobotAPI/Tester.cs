using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        public static List<dynamic> CANDLES_DATA { get; set; }      // Список свечей выбранного инструмента
        public static List<dynamic> CANDLES_TF1_DATA { get; set; }  // Список свечей выбранного инструмента: таймфрейма 1m
        static int CANDLES_TF1_INDEX { get; set; }
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

            CANDLES_INDEX = -1;
            CANDLES_TF1_INDEX = 0;
            CANDLE_CURRENT = null;
            IS_CANDLE_FULL = !CANDLES_TF1_USE;

            CandleClear();
            OrderClear();
            TESTER_AUTO = false;
            TESTER_FINISHED = false;
            new LOGG();
            SETUP.Init();
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
        }

        /// <summary>
        /// Добавление очередной свечи к текущему графику
        /// </summary>
        static void CandleNew()
        {
            if (CandleNewTF1())
                return;

            CANDLES_INDEX++;
            CandleGlobalSet();

            TESTER_FINISHED = CANDLES_COUNT >= INSTRUMENT.RowsCount;
        }
        /// <summary>
        /// Формирование свечи из минутного таймфрейма
        /// </summary>
        static bool CandleNewTF1()
        {
            if (!CANDLES_TF1_USE)
                return false;

            int i = CANDLES_TF1_INDEX++;
            dynamic tf1 = CANDLES_TF1_DATA[i];

            if(CANDLE_CURRENT == null || !CANDLE_CURRENT.Upd(tf1))
            {
                CANDLE_CURRENT = tf1.Clone(INSTRUMENT.TimeFrame);
                CANDLES_INDEX++;
            }

            CandleGlobalSet();

            IS_CANDLE_FULL = CANDLE_CURRENT.IsFull;
            TESTER_FINISHED = CANDLES_TF1_INDEX >= CANDLES_TF1_DATA.Count;

            return true;
        }
    }
}
