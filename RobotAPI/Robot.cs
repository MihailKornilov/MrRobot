﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            if (CANDLES == null)
                return;
            if (CANDLES.Count == 0)
                return;

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

                CandleList.Add(CANDLES[0]);

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

                var dst = SRC.Create(CandleList, SRC.CdiId);
                if (dst.Size == 0)
                    return false;

                return SRC.Compare(dst);
            }
        }
    }
}
