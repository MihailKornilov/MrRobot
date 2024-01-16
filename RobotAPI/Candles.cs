using System.Collections.Generic;
using static System.Console;

namespace RobotAPI
{
    public static partial class Robot
    {
        /// <summary>
        /// Установка глобальных значений свечи
        /// </summary>
        static void CandleGlobalSet(dynamic candle)
        {
            UNIX   = candle.Unix;
            PRICE  = candle.Close;
            HIGH   = candle.High;
            OPEN   = candle.Open;
            CLOSE  = candle.Close;
            LOW    = candle.Low;
            VOLUME = candle.Volume;
        }

        /// <summary>
        /// Добавление очередной свечи к текущему графику
        /// </summary>
        static void CandleNew()
        {
            if (CandleNewTF1())
                return;

            int c = CANDLES.Count;
            dynamic candle = CANDLES_DATA[c];

            CANDLES.Insert(0, candle);
            CandleGlobalSet(candle);

            FINISH_TEST = CANDLES.Count == INSTRUMENT.RowsCount;
        }


        static int CDi;         // Индекс свечных данных CANDLES_DATA
        static int TF1i;        // Индекс свечных данных CANDLES_TF1
        static dynamic CanTF1;  // Свеча из CANDLES_TF1
        /// <summary>
        /// Формирование свечи из минутного таймфрейма
        /// </summary>
        static bool CandleNewTF1()
        {
            if (!CANDLES_TF1_USE)
                return false;

            dynamic tf1 = CANDLES_TF1[TF1i];

            if (CDi < CANDLES_DATA.Count && tf1.Unix >= CANDLES_DATA[CDi].Unix)
            {
                CanTF1 = tf1;
                CanTF1.Unix = CANDLES_DATA[CDi].Unix;
                CANDLES.Insert(0, CanTF1);
                CDi++;
            } else
            {
                if (CanTF1.High < tf1.High)
                    CanTF1.High = tf1.High;
                if (CanTF1.High < tf1.Open)
                    CanTF1.High = tf1.Open;
                if (CanTF1.High < tf1.Close)
                    CanTF1.High = tf1.Close;

                CanTF1.Close = tf1.Close;

                if (CanTF1.Low > tf1.Low)
                    CanTF1.Low = tf1.Low;
                if (CanTF1.Low > tf1.Open)
                    CanTF1.Low = tf1.Open;
                if (CanTF1.Low > tf1.Close)
                    CanTF1.Low = tf1.Close;

                CanTF1.Volume += tf1.Volume;

                CANDLES[0] = CanTF1;
            }

            CandleGlobalSet(CanTF1);

            if (IS_CANDLE_FULL = CandleFull())
                CANDLES[0].Exp = INSTRUMENT.Exp;

            FINISH_TEST = ++TF1i >= CANDLES_TF1.Count;

            return true;
        }
        static bool CandleFull()
        {
            if (TF1i + 1 >= CANDLES_TF1.Count)
                return true;
            if(CDi < CANDLES_DATA.Count)
                return CANDLES_TF1[TF1i + 1].Unix >= CANDLES_DATA[CDi].Unix;
            return false;
        }

        /// <summary>
        /// Очистка данных первой свечи и текущих свечных данных
        /// </summary>
        static void CandleClear()
        {
            UNIX   = 0;
            PRICE  = 0;
            HIGH   = 0;
            OPEN   = 0;
            CLOSE  = 0;
            LOW    = 0;
            VOLUME = 0;

            CANDLES = new List<dynamic>();

            // Инициализация графика свечных данных 1m
            CDi = 0;
            TF1i = 0;
            CanTF1 = null;
        }
    }
}
