using System.Collections.Generic;

namespace RobotAPI
{
    public static partial class Robot
    {
        public static List<dynamic> CANDLES_DATA { get; set; }      // Список свечей выбранного инструмента
        public static List<dynamic> CANDLES_TF1 { get; set; }       // Список свечей выбранного инструмента: таймфрейма 1m
        public static bool CANDLES_TF1_USE { get; set; }            // Флаг испльзования таймфрейма 1m
        public static bool IS_CANDLE_FULL { get; private set; }     // Свеча сформирована (всегда, если используется таймфрейм 1m)

        public static bool AUTO_TEST { get; set; }          // Флаг запуска/остановки автоматического тестирования
        public static bool FINISH_TEST { get; private set; }// Флаг завершения тестирования (когда закончились свечи)




        /// <summary>
        /// Инициализация данных для тестера
        /// </summary>
        public static void TESTER_GLOBAL_INIT()
        {
            IS_TESTER = true;
            LogClear();
            CandleClear();
            OrderClear();
            AUTO_TEST = false;
            FINISH_TEST = false;
            IS_CANDLE_FULL = !CANDLES_TF1_USE;
        }

        public static bool TESTER_GLOBAL_STEP()
        {
            if (FINISH_TEST)
                return false;

            CandleNew();

            return true;
        }
    }
}
