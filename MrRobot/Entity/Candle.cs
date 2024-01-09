﻿using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;

namespace MrRobot.Entity
{
    public static class Candle
    {
        /// <summary>
        /// Список доступных скачанных свечных данных
        /// </summary>
        private static List<CandleDataInfoUnit> CDIlist { get; set; }
        /// <summary>
        /// Ассоциативный массив ID и свечных данных (для быстрого поиска)
        /// </summary>
        private static Dictionary<int, CandleDataInfoUnit> IdUnitAss { get; set; }


        /// <summary>
        /// Загрузка из базы списка свечных данных
        /// </summary>
        public static void ListCreate(bool upd = false)
        {
            if (!upd && CDIlist != null && CDIlist.Count > 0)
                return;

            CDIlist = new List<CandleDataInfoUnit>();
            IdUnitAss = new Dictionary<int, CandleDataInfoUnit>();

            string sql = "SELECT*" +
                         "FROM`_candle_data_info`" +
                         "ORDER BY`marketId`,`symbol`,`timeFrame`";
            foreach (Dictionary<string, string> v in mysql.QueryList(sql))
            {
                var Instr = Instrument.Unit(v["instrumentId"]);
                int marketId = Convert.ToInt32(v["marketId"]);
                int timeFrame = Convert.ToInt32(v["timeFrame"]);
                string[] spl = v["name"].Split('/');
                string begin = v["begin"].Substring(0, 10);
                string end = v["end"].Substring(0, 10);
                var Unit = new CandleDataInfoUnit
                {
                    Id = Convert.ToInt32(v["id"]),
                    Market = format.MarketName(marketId),
                    InstrumentId = Convert.ToInt32(v["instrumentId"]),
                    Name = v["name"],
                    Symbol = spl[0] + spl[1],
                    DateBegin = begin,
                    DateEnd = end,
                    DatePeriod = begin + "-" + end,
                    TimeFrame = timeFrame,
                    TF = format.TF(timeFrame),
                    Table = v["table"],
                    RowsCount = Convert.ToInt32(v["rowsCount"]),
                    TickSize = Instr.TickSize,
                    NolCount = Instr.NolCount,
                    ConvertedFromId = Convert.ToInt32(v["convertedFromId"])
                };
                CDIlist.Add(Unit);
                IdUnitAss.Add(Unit.Id, Unit);
            }
        }

        /// <summary>
        /// Получение всего списка свечных данных
        /// </summary>
        public static List<CandleDataInfoUnit> ListAll()
        {
            return CDIlist;
        }

        /// <summary>
        /// Получение списка свечных данных с таймфреймом 1m с учётом поиска
        /// </summary>
        public static List<CandleDataInfoUnit> List1m(string txt = "")
        {
            var send = new List<CandleDataInfoUnit>();
            var num = 0;
            foreach (var v in CDIlist)
            {
                if (v.TimeFrame != 1)
                    continue;
                if (txt.Length > 0)
                    if(!v.Name.Contains(txt.ToUpper()))
                        continue;

                v.Num = num++;
                send.Add(v);
            }

            return send;
        }

        /// <summary>
        /// Получение списка свечных данных по ID инструмента
        /// </summary>
        // iid - ID инструмента
        // TF1Enable - включать таймфрейм 1
        public static List<CandleDataInfoUnit> ListOnIID(int iid, bool TF1Enable = true)
        {
            var send = new List<CandleDataInfoUnit>();

            foreach (var v in CDIlist)
            {
                if (v.InstrumentId != iid)
                    continue;
                if (!TF1Enable && v.TimeFrame == 1)
                    continue;

                send.Add(v);
            }

            return send;
        }

        /// <summary>
        /// Получение идентификаторов свечных данных по Name
        /// </summary>
        // name - Name инструмента в виде USDT/BTC
        // TFs - таймфреймы, будет получение
        public static string IdsOnSymbol(string name, string TFs)
        {
            var list = new List<int>();
            string[] ids = TFs.Split(',');
            
            foreach (var v in CDIlist)
            {
                if (v.Name != name)
                    continue;
                if (!ids.Contains(v.TimeFrame.ToString()))
                    continue;

                list.Add(v.Id);
            }

            return string.Join(",", list.ToArray());
        }

        /// <summary>
        /// Единица информации свечных данных на основании названия таблицы со свечами
        /// </summary>
        public static CandleDataInfoUnit InfoUnitOnTable(string Table)
        {
            foreach (var v in CDIlist)
                if (v.Table == Table)
                    return v;
            return null;
        }

        /// <summary>
        /// Единица информации свечных данных на основании ID
        /// </summary>
        public static CandleDataInfoUnit InfoUnit(int Id)
        {
            if (IdUnitAss.ContainsKey(Id))
                return IdUnitAss[Id];

            return null;
        }
        public static CandleDataInfoUnit InfoUnit(string IdString)
        {
            int Id = Convert.ToInt32(IdString);
            return InfoUnit(Id);
        }

        /// <summary>
        /// Единица информации свечных данных на основании Name и указанного таймфрейма
        /// </summary>
        public static CandleDataInfoUnit UnitTF(string name, string tf = "1")
        {
            int TimeFrame = Convert.ToInt32(tf);
            foreach (var v in CDIlist)
            {
                if(v.Name != name)
                    continue;
                if(v.TimeFrame != TimeFrame)
                    continue;

                return v;
            }

            return null;
        }




        /// <summary>
        /// Удаление единицы свечных данных из списка
        /// </summary>
        public static void InfoUnitDel(int id)
        {
            if (CDIlist == null)
                return;
            if (CDIlist.Count == 0)
                return;
            if(!IdUnitAss.ContainsKey(id))
                return;

            var unit = IdUnitAss[id];
            
            string sql = $"DROP TABLE IF EXISTS`{unit.Table}`";
            mysql.Query(sql);

            sql = $"DELETE FROM`_candle_data_info`WHERE`id`={id}";
            mysql.Query(sql);

            // Удаление Архива поисков паттернов
            sql = $"SELECT`id`FROM`_pattern_search`WHERE`cdiId`={id}";
            string ids = mysql.Ids(sql);

            sql = $"DELETE FROM`_pattern_found`WHERE`searchId`IN({ids})";
            mysql.Query(sql);

            sql = $"DELETE FROM`_pattern_search`WHERE`cdiId`={id}";
            mysql.Query(sql);

            CDIlist.Remove(unit);
            IdUnitAss.Remove(id);
            Instrument.DataCountMinus(unit.InstrumentId);
        }











        /// <summary>
        /// Создание таблицы со свечными данными, если не существует
        /// </summary>
        public static string DataTableCreate(CandleDataParam param)
        {
            string TableName = "bybit_" + param.Symbol.ToLower() + "_" + param.TimeFrame;

            string sql = $"DROP TABLE IF EXISTS `{TableName}`";
            mysql.Query(sql);

            sql = $"CREATE TABLE `{TableName}` (" +
                        "`unix` INT UNSIGNED DEFAULT 0 NOT NULL," +
                       $"`high` DECIMAL(20,{param.NolCount}) UNSIGNED DEFAULT 0 NOT NULL," +
                       $"`open` DECIMAL(20,{param.NolCount}) UNSIGNED DEFAULT 0 NOT NULL," +
                       $"`close`DECIMAL(20,{param.NolCount}) UNSIGNED DEFAULT 0 NOT NULL," +
                       $"`low`  DECIMAL(20,{param.NolCount}) UNSIGNED DEFAULT 0 NOT NULL," +
                        "`vol`  DECIMAL(30,8) UNSIGNED DEFAULT 0 NOT NULL," +
                        "PRIMARY KEY (`unix`)" +
                  $") ENGINE=MyISAM DEFAULT CHARSET=cp1251;";
            mysql.Query(sql);

            return TableName;
        }

        /// <summary>
        /// Внесение в базу сформированных свечных записей
        /// </summary>
        public static void DataInsert(string TableName, List<string> insert, int CountMin = 0)
        {
            if (insert.Count == 0)
                return;
            if (CountMin > 0 && insert.Count < CountMin)
                return;

            string sql = $"INSERT INTO`{TableName}`" +
                        $"(`unix`,`high`,`open`,`close`,`low`,`vol`) " +
                        $"VALUES " + string.Join(",", insert.ToArray());
            mysql.Query(sql);

            insert.Clear();
        }

        /// <summary>
        /// Внесение заголовка свечных данных
        /// </summary>
        public static int InfoCreate(string TableName, int ConvertedFromId = 0)
        {
            string sql = "SELECT " +
                            "COUNT(*)`count`," +
                            "MIN(FROM_UNIXTIME(`unix`))`begin`," +
                            "MAX(FROM_UNIXTIME(`unix`))`end`" +
                         $"FROM`{TableName}`" +
                          "LIMIT 1";
            var data = mysql.QueryOne(sql);

            if (data["count"] == "0")
                return 0;

            string[] spl = TableName.Split('_');
            string TimeFrame = spl[2];
            string Symbol = spl[1].ToUpper();


            // Получение данных об инструменте по его названию
            var Instr = Instrument.UnitOnSymbol(Symbol);

            sql = "INSERT INTO`_candle_data_info`(" +
                    "`marketId`," +
                    "`instrumentId`," +
                    "`table`," +
                    "`timeFrame`," +
                    "`name`," +
                    "`symbol`," +
                    "`rowsCount`," +
                    "`begin`," +
                    "`end`," +
                    "`convertedFromId`" +
                ")VALUES(" +
                    "1," +
                    $"{Instr.Id}," +
                    $"'{TableName}'," +
                    $"{TimeFrame}," +
                    $"'{Instr.Name}'," +
                    $"'{Symbol}'," +
                    $"{data["count"]}," +
                    $"'{data["begin"]}'," +
                    $"'{data["end"]}'," +
                    $"{ConvertedFromId}" +
            ")";
            return mysql.Query(sql);
        }

        /// <summary>
        /// Проверка соответствия скачанных данных с заголовками
        /// </summary>
        public static void DataControl(string tableLike = "bybit_", IProgress<int> prgs = null)
        {
            string sql = $"SHOW TABLES LIKE '{tableLike}%'";
            string[] mass = mysql.QueryColOne(sql);

            if (mass.Length == 0)
                return;

            var bar = new ProBar(mass.Length);
            for (int i = 0; i < mass.Length; i++)
            {
                string tableName = mass[i];

                sql = $"SELECT " +
                            $"COUNT(*)`count`," +
                            $"MIN(FROM_UNIXTIME(`unix`))`begin`," +
                            $"MAX(FROM_UNIXTIME(`unix`))`end`" +
                      $"FROM`{tableName}`";
                var res = mysql.QueryOne(sql);

                int count = Convert.ToInt32(res["count"]);
                string begin = res["begin"];
                string end = res["end"];

                if (bar.isUpd(i))
                    prgs.Report(bar.Value);

                if (count == 0)
                {
                    sql = $"DROP TABLE`{tableName}`";
                    mysql.Query(sql);

                    sql = $"DELETE FROM`_candle_data_info`WHERE`table`='{tableName}'";
                    mysql.Query(sql);

                    continue;
                }

                // Получение названия инструмента и таймфрейма из названия таблицы
                string[] sp = tableName.Split('_');
                string symbol = sp[1].ToUpper();
                string timeFrame = sp[2];

                // Получение данных об инструменте по его названию
                var Instr = Instrument.UnitOnSymbol(symbol);

                // Проверка на наличие заголовка истории
                string id = "0";
                sql = $"SELECT*FROM`_candle_data_info`WHERE`table`='{tableName}'";
                res = mysql.QueryOne(sql);
                if (res.Count != 0)
                    id = res["id"];


                // Внесение заголовка истории
                sql = "INSERT INTO `_candle_data_info` (" +
                            "`id`," +
                            "`marketId`," +
                            "`instrumentId`," +
                            "`table`," +
                            "`timeFrame`," +
                            "`name`," +
                            "`symbol`," +
                            "`rowsCount`," +
                            "`begin`," +
                            "`end`" +
                        ") VALUES (" +
                           $"{id}," +
                            "1," +
                           $"{Instr.Id}," +
                           $"'{tableName}'," +
                           $"{timeFrame}," +
                           $"'{Instr.Name}'," +
                           $"'{symbol}'," +
                           $"{count}," +
                           $"'{begin}'," +
                           $"'{end}'" +
                        ") ON DUPLICATE KEY UPDATE " +
                            "`rowsCount`=VALUES(`rowsCount`)," +
                            "`begin`=VALUES(`begin`)," +
                            "`end`=VALUES(`end`)";
                mysql.Query(sql);
            }

            ListCreate(true);
        }

        /// <summary>
        /// Количество свечей в виде текста: "1 234 свечи"
        /// </summary>
        public static string CountTxt(int count)
        {
            if (count == 0)
                return "";

            return format.Num(count) + " свеч" + format.End(count, "а", "и", "ей");
        }

        /// <summary>
        /// Получение времени в формате UNIX для определённого таймфрейма
        /// </summary>
        public static int UnixTF(int unix, int tf = 1)
        {
            int MinuteTotal = unix / 60;
            int MinuteDay = MinuteTotal / 1440 * 1440;
            int ost = (MinuteTotal - MinuteDay) % tf;

            return (MinuteTotal - ost) * 60;
        }

        /// <summary>
        /// Загрузка свечей с бриржи для выбранного инструмента
        /// </summary>
        public static List<object> WCkline(string symbol, int start = 0)
        {
            var dur = new Dur();

            start = start == 0 ? format.UnixNow() - 59_880 : start + 60;
            string url = "https://api.bybit.com/v5/market/kline?category=spot" +
                        "&symbol=" + symbol +
                        "&interval=1" +
                        "&start=" + start + "000" +
                        "&limit=1000";
            string json = new WebClient().DownloadString(url);
            dynamic arr = JsonConvert.DeserializeObject(json);

            var TF1List = new List<object>();

            if (arr.retMsg == null)
                return TF1List;
            if (arr.retMsg != "OK")
                return TF1List;

            var list = arr.result.list;
            if (list.Count == 0)
                return TF1List;

            WriteLine($"{url}   {list.Count}   {dur.Second()}");

            var Instr = Instrument.UnitOnSymbol(symbol);

            for (int k = 0; k < list.Count; k++)
            {
                int unix = Convert.ToInt32(list[k][0].ToString().Substring(0, 10));

                if (unix < start)
                    continue;

                TF1List.Add(new CandleUnit
                {
                    Unix = unix,
                    High = list[k][2],
                    Open = list[k][1],
                    Close = list[k][4],
                    Low = list[k][3],
                    Volume = list[k][5],
                    Exp = Instr.Exp
                });
            }

            return TF1List;
        }
    }

    

    /// <summary>
    /// Единица информации свечных данных
    /// </summary>
    public class CandleDataInfoUnit
    {
        public int Num { get; set; }            // Порядковый номер
        public int Id { get; set; }             // ID инфо свечных данных из `_candle_data_info`
        public int InstrumentId { get; set; }   // ID инструмента из `_instrument`
        public string Market { get; set; }      // Имя биржи, из которой взят инструмент
        public string Symbol { get; set; }      // Название инструмента в виде "BTCUSDT"
        public string Name { get; set; }        // Название инструмента в виде "BTC/USDT"
        public string Table { get; set; }       // Имя таблицы со свечами
        public int TimeFrame { get; set; }      // Таймфрейм в виде 15
        public string TF { get; set; }          // Таймфрейм в виде "15m"
        public int RowsCount { get; set; }      // Количество свечей в графике (в таблице)
        public string DateBegin { get; set; }   // Дата начала графика в формате 12.03.2022
        public string DateEnd { get; set; }     // Дата конца графика в формате 12.03.2022
        public string DatePeriod { get; set; }  // Диапазон даты от начала до конца всего графика в формате 12.03.2022 - 30.11.2022
        public int ConvertedFromId { get; set; }// ID минутного таймфрейма, с которого была произведена конвертация


        public double TickSize { get; set; }    // Шаг цены
        public int NolCount { get; set; }       // Количество нулей после запятой
    }

    /// <summary>
    /// Настройки для скачивания или конвертации свечных данных
    /// </summary>
    public class CandleDataParam
    {
        public bool IsProcess { get; set; } = true; // Флаг выполнения фонового процесса
        public int Id { get; set; }                 // ID свечных данных
        public string Symbol { get; set; }
        public int TimeFrame { get; set; }
        public int NolCount { get; set; }
        public ProBar Bar { get; set; }             // Основная линия Прогресс-бара


        // Для History
        public string Table { get; set; }
        public int CC { get; set; }                 // CandlesCount - сколько свечей загружено (в процессе)
        public int UnixStart { get; set; }
        public int UnixFinish { get; set; }

 
        // Для Converter
        public string SourceTable { get; set; }
        public int ProgressMainValue { get; set; }  // Значение, которое будет отображаться Main-прогресс-бар
        public int TfNum { get; set; }              // Номер конвертации, если было выбрано несколько таймфреймов
        public int[] ConvertedIds { get; set; }     // ID сконвертированных свечных данных
    }

    /// <summary>
    /// Данные об одной свече
    /// </summary>
    public class CandleUnit
    {
        public int TimeFrame { get; set; } = 1; // Таймфрейм свечи

        public int Unix { get; set; }           // Время свечи в формате Unix согласно Таймфрейму
        public string DateTime { get { return format.DTimeFromUnix(Unix); } }
        public double High { get; set; }        // Максимальная цена свечи
        public double Open { get; set; }        // Цена открытия
        public double Close { get; set; }       // Цена закрытия
        public double Low { get; set; }         // Минимальная цена
        public double Volume { get; set; }      // Объём


        // Обновление свечи (для динамического графика)
        public void Update(int unix, double price = 0, double volume = 0)
        {
            // Unix-время свечи на основании Таймфрейма
            int UnixTF = Candle.UnixTF(unix, TimeFrame);

            if (Unix == UnixTF)
            {
                // Обновление цены закрытия
                if (price > 0)
                {
                    Close = price;
                    if (High < price)
                        High = price;
                    if (Low > price)
                        Low = price;
                }

                // Обновление объёма
                Volume += volume;

                return;
            }

            // Новая свеча
            Unix = UnixTF;
            Close = price > 0 ? price : Close;
            High = Close;
            Open = Close;
            Low = Close;
            Volume = volume;
        }

        // Обновление свечи согласно таймфрейму
        public bool Upd(CandleUnit src)
        {
            if (Unix != Candle.UnixTF(src.Unix, TimeFrame))
                return false;

            Close = src.Close;

            if (High < src.High)
                High = src.High;
            if (Low > src.Low)
                Low = src.Low;

            Volume += src.Volume;

            return true;
        }

        // Количество нулей после запятой в 10-й степени
        public ulong Exp { get; set; }

        // Размер верхнего хвоста свечи в пунктах
        public int WickTop
        {
            get { return Convert.ToInt32((Close > Open ? High - Close : High - Open) * Exp); }
        }
        // Размер тела свечи
        public int Body
        {
            get { return Convert.ToInt32((Close - Open) * Exp); }
        }
        // Размер нижнего хвоста свечи
        public int WickBtm
        {
            get { return Convert.ToInt32((Close > Open ? Open - Low : Close - Low) * Exp); }
        }


        // Обновление первой свечи в графике
        public string CandleToChart()
        {
            return "{" +
                $"time:{format.TimeZone(Unix)}," +
                $"high:{High}," +
                $"open:{Open}," +
                $"close:{Close}," +
                $"low:{Low}" +
            "}";
        }
        // Обновление объёма в графике
        public string VolumeToChart()
        {
            string color = Close > Open ? "#127350" : "#86303E";
            return "{" +
                $"time:{format.TimeZone(Unix)}," +
                $"value:{Volume}," +
                $"color:\"{color}\"" +
            "}";
        }


        // Внесение в базу одной свечи
        public string Insert { get { return $"({Unix},{High},{Open},{Close},{Low},{Volume})"; } }
    }
}