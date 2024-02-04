using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MrRobot.inc;

namespace MrRobot.Entity
{
    public class Instrument
    {
        static List<InstrumentUnit> InstrumentList { get; set; }

        /// <summary>
        /// Количество доступных инструментов
        /// </summary>
        public static int Count { get => InstrumentList.Count; }

        /// <summary>
        /// Ассоциативный массив ID и данных об инструменте (для быстрого поиска)
        /// </summary>
        static Dictionary<int, InstrumentUnit> IdUnitAss { get; set; } = null;

        /// <summary>
        /// Загрузка списка инструментов из базы
        /// </summary>
        public Instrument()
        {
            string sql = "SELECT" +
                            "`instrumentId`," +
                            "COUNT(`id`)" +
                         "FROM`_candle_data_info`" +
                         "GROUP BY`instrumentId`";
            var CountAss = mysql.IntStringAss(sql);

            InstrumentList = new List<InstrumentUnit>();
            IdUnitAss = new Dictionary<int, InstrumentUnit>();

            sql = "SELECT*" +
                  "FROM`_instrument`" +
                  "WHERE`marketId`=1 " +
                  "ORDER BY`quoteCoin`,`baseCoin`";
            foreach (Dictionary<string, string> v in mysql.QueryList(sql))
            {
                int Id = Convert.ToInt32(v["id"]);
                var Unit = new InstrumentUnit
                {
                    Id = Id,
                    MarketId = Convert.ToInt32(v["marketId"]),
                    Name = v["baseCoin"] + "/" + v["quoteCoin"],
                    Symbol = v["symbol"],
                    BasePrecision = Convert.ToDouble(v["basePrecision"]),
                    QuotePrecision = Convert.ToDouble(v["quotePrecision"]),
                    MinOrderQty = Convert.ToDouble(v["minOrderQty"]),
                    HistoryBegin = format.DateOne(v["historyBegin"]),
                    CandleDataCount = CountAss.ContainsKey(Id) ? CountAss[Id] : "",
                    TickSize = Convert.ToDouble(v["tickSize"]),
                    NolCount = format.NolCount(v["tickSize"]),
                    Status = v["status"],

                    BaseCoin = v["baseCoin"],
                    QuoteCoin = v["quoteCoin"]
                };

                InstrumentList.Add(Unit);
                IdUnitAss.Add(Id, Unit);
            }
        }

        /// <summary>
        /// Список инструментов с учётом поиска
        /// </summary>
        public static List<InstrumentUnit> ListBox(string txt = "")
        {
            var list = new List<InstrumentUnit>();
            bool isHist = txt == "/HISTORY";
            bool isTxt = txt.Length > 0 && !isHist;
            int num = 1;
            foreach (var v in InstrumentList)
            {
                if (isTxt && !v.Name.Contains(txt.ToUpper()))
                    continue;
                if (isHist && v.CandleDataCount.Length == 0)
                    continue;

                v.Num = num++ + ".";
                list.Add(v);
            }

            return list;
        }

        /// <summary>
        /// Ассоциативный массив инструментов по Symbol
        /// </summary>
        public static Dictionary<string, InstrumentUnit> SymbolUnitAss()
        {
            var ass = new Dictionary<string, InstrumentUnit>();
            foreach (InstrumentUnit unit in InstrumentList)
                ass.Add(unit.Symbol, unit);
            return ass;
        }



        /// <summary>
        /// Один инструмент по ID
        /// </summary>
        public static InstrumentUnit Unit(int Id)
        {
            if (IdUnitAss.ContainsKey(Id))
                return IdUnitAss[Id];

            return null;
        }
        public static InstrumentUnit Unit(string IdS)
        {
            int id = Convert.ToInt32(IdS);
            return Unit(id);
        }

        /// <summary>
        /// Один инструмент по Symbol в виде "BTC/USDT"
        /// </summary>
        public static InstrumentUnit UnitOnSymbol(string symbol)
        {
            if (symbol.Contains("/"))
            {
                string[] spl = symbol.Split('/');
                symbol = spl[0] + spl[1];
            }

            foreach(InstrumentUnit unit in InstrumentList)
                if(unit.Symbol == symbol)
                    return unit;

            return null;
        }


        /// <summary>
        /// Прибавление количества свечных данных инструмента
        /// </summary>
        public static void DataCountPlus(int id, int add = 1)
        {
            var unit = Unit(id);
            int count = add;
            if(unit.CandleDataCount != "")
                count = Convert.ToInt32(unit.CandleDataCount) + add;
            unit.CandleDataCount = count.ToString();
        }

        /// <summary>
        /// Убавление количества свечных данных инструмента
        /// </summary>
        public static void DataCountMinus(int id)
        {
            var unit = Unit(id);
            int count = Convert.ToInt32(unit.CandleDataCount) - 1;
            unit.CandleDataCount = count > 0 ? count.ToString() : "";
        }
    }



    public class InstrumentUnit
    {
        public string Num { get; set; }

        // ID инструмента
        public int Id { get; set; }

        public int MarketId { get; set; }       // ID биржи из `_market`
        public string Name { get; set; }        // Название инструмента в виде "BTC/USDT"
        public string Symbol { get; set; }      // Название инструмента в виде "BTCUSDT"

        public double BasePrecision { get; set; }
        public double QuotePrecision { get; set; }
        public double MinOrderQty { get; set; }
        public string HistoryBegin { get; set; }// Дата начала истории инструмента
        public string CandleDataCount { get; set; }// Количество скачанных свечных данных (графиков)

        public double TickSize { get; set; }    // Шаг цены
        public int NolCount { get; set; }       // Количество нулей после запятой. Получение из TickSize
        public ulong Exp { get { return format.Exp(NolCount); } } // Cтепень числа 10
        public string Status { get; set; }      // Статус инструмента:
                                                //      "1" - активен
                                                //      "0" - не активен



        // ---=== ДЛЯ РОБОТА ===---
        public string BaseCoin { get; set; }    // Название базовой монеты
        public string QuoteCoin { get; set; }   // Название котировочной монеты
        public double BaseBalance {  get; set; }// Баланс базовой монеты
        public double QuoteBalance {  get; set; }// Баланс котировочной монеты
        public double BaseCommiss { get; set; } // Сумма комиссий исполненных ордеров базовой монеты
        public double QuoteCommiss { get; set; }// Сумма комиссий исполненных ордеров базовой котировочной монеты

        public int CdiId { get; set; }          // ID свечных данных
        public string Table { get { return Candle.Unit(CdiId).Table; } }        // Имя таблицы со свечами
        public int RowsCount { get { return Candle.Unit(CdiId).RowsCount; } }   // Количество свечей в графике (в таблице)
        public int TimeFrame { get { return Candle.Unit(CdiId).TimeFrame; } }   // Таймферйм
        public string TF { get { return Candle.Unit(CdiId).TF; } } // Таймфрейм 10m
    }
}
