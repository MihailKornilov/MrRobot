using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;
using MrRobot.Section;
using static MrRobot.Connector.MOEX;
using MrRobot.Entity;

namespace MrRobot.Connector
{
    public class MOEX
    {
        public MOEX()
        {
            new Engine();
            new Market();
            new BoardGroup();
            new Board();
            new SecurityGroup();
            new SecurityType();
            new SecurityСollections();
            new Security();
        }


        // Формирование страницы со всеми запросами ISS
        public static void IssQueriesPage(BoardUnit board)
        {
            string name = "MoexIssQueries";
            string src = Path.GetFullPath($"Browser/{name}.tmp.html");
            string dst = Path.GetFullPath($"Browser/{name}.html");

            var read  = new StreamReader(src);
            var write = new StreamWriter(dst);

            string line;
            while ((line = read.ReadLine()) != null)
            {
/*
                if (line.Contains("        /iss"))
                {
                    line = line.Replace("        ", "");
                    line = $"    <a href='https://iss.moex.com{line}.json' target='_blank'>{line}</a>";
                }
                else if (line.Contains("        "))
                {
                    line = line.Replace("        ", "");
                    line = $"    <div>{line}</div>";
                }
                else if (line.Length == 0)
                    line = "<br>";
*/
                line = line.Replace("[engine]", board.EngineName);
                line = line.Replace("[market]", board.MarketName);
                line = line.Replace("[board]",  board.Name);
                line = line.Replace("[boardgroup]",  board.Group);
                line = line.Replace("[security]", board.SecId);

                write.WriteLine(line);
            }
            read.Close();
            write.Close();
        }

        // Информация о Бумаге и Режимы торгов
        public static dynamic[] SecurityInfoBoards(string secid)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string url = $"https://iss.moex.com/iss/securities/{secid}.json?" +
                                "iss.meta=off" +
                               "&description.columns=name,title,value" +
                               "&boards.columns=" +
                                    "secid," +
                                    "boardid," +
                                    "is_primary," +
                                    "listed_from," +
                                    "listed_till," +
                                    "title," +
                                    "is_traded," +
                                    "decimals";
            string str = wc.DownloadString(url);
            dynamic json = JsonConvert.DeserializeObject(str);

            dynamic data = json.description.data;
            var SecInfoList = new List<SecurityInfoUnit>();
            for (int i = 0; i < data.Count; i++)
                SecInfoList.Add(new SecurityInfoUnit(data[i]));

            data = json.boards.data;
            var BoardsList = new List<BoardUnit>();
            for (int i = 0; i < data.Count; i++)
                BoardsList.Add(new BoardUnit(i+1, data[i]));

            return new dynamic[2] { SecInfoList, BoardsList };
        }

        // Параметры для загрузки свечных данных выбранного Режима торгов
        public static List<BorderUnit> BoardLoad(BoardUnit board)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string url = $"https://iss.moex.com/iss" +
                                $"/engines/{board.EngineName}" +
                                $"/markets/{board.MarketName}" +
                                $"/boards/{board.Name}" +
                                $"/securities/{board.SecId}" +
                                 "/candleborders.json" +
                                 "?iss.only=borders" +
                                 "&iss.meta=off";
            WriteLine(url);
            string str = wc.DownloadString(url);
            dynamic json = JsonConvert.DeserializeObject(str);
            dynamic data = json.borders.data;

            var list = new List<BorderUnit>();
            if (data.Count == 0)
                return list;

            var dict = new Dictionary<int, BorderUnit>();
            for (int i = 0; i < data.Count; i++)
            {
                var unit = new BorderUnit(data[i]);
                dict.Add(unit.Interval, unit);
            }

            foreach (int i in BorderUnit.Sort)
                if (dict.ContainsKey(i))
                    list.Add(dict[i]);

            return list;
        }

        // Загрузка свечных данных
        public static void CandlesLoad(BoardUnit board, int interval, string from, string till)
        {
            string table = Candle.DataTableCreate("moex", board.SecId, interval, board.Decimals);

            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;

            while (true)
            {
                string url = $"https://iss.moex.com/iss" +
                                    $"/engines/{board.EngineName}" +
                                    $"/markets/{board.MarketName}" +
                                    $"/boards/{board.Name}" +
                                    $"/securities/{board.SecId}" +
                                     "/candles.json" +
                                     "?iss.only=candles" +
                                     "&candles.columns=begin,open,high,low,close,volume" +
                                     "&iss.meta=off" +
                                    $"&interval={interval}" +
                                    $"&from={from}" +
                                    $"&till={till}";
                WriteLine(url);
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);
                dynamic data = json.candles.data;

                var insert = new List<string>();

                if (data.Count == 0)
                    break;

                int count = data.Count;
                if (data.Count == 500) count--;

                for (int i = 0; i < count; i++)
                    insert.Add(new CandleUnit(data[i]).Insert);

                Candle.DataInsert(table, insert);

                if (data.Count < 500)
                    break;

                from = data[count][0];
            }

            Candle.InfoCreate(table);
        }


        // Запрос и получение данных от биржи по указанным данным
        static dynamic WsIssData(string value)
        {
            var wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string url = $"https://iss.moex.com/iss/index.json?iss.only={value}&iss.meta=off";
            WriteLine(url);
            string str = wc.DownloadString(url);
            dynamic json = JsonConvert.DeserializeObject(str);
            return json[value].data;
        }

        /// <summary>
        /// Торговая система
        /// </summary>
        public class Engine
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public Engine()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT DISTINCT(`engineId`),COUNT(*)FROM`_moex_securities`GROUP BY`engineId`";
                var ASS = mysql.IntAss(sql);

                sql = "SELECT*FROM`_moex_engines`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    unit.SecurityCount = ASS.ContainsKey(unit.Id) ? ASS[unit.Id] : 0;
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Весь список
            public static List<MoexUnit> ListAll() => UnitList;
            // Список с бумагами
            public static List<MoexUnit> ListActual()
            {
                var send = new List<MoexUnit>();
                for (int i = 0; i < UnitList.Count; i++)
                {
                    var unit = UnitList[i];
                    if (unit.SecurityCount > 0)
                        send.Add(unit);
                }
                return send;
            }

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;

            // Порядковый номер в списке
            public static int FilterIndex()
            {
                var list = ListActual();
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Id == SecurityFilter.EngineId)
                        return i;
                return -1;
            }

            // Обновление количеств бумаг на основании фильтра
            public static void CountFilter()
            {
                // Обнуление количеств бумаг
                foreach (var unit in ListActual())
                    unit.SecurityCountFilter = 0;

                foreach (var sec in Security.ListAll)
                {
                    if (!SecurityFilter.IsAllowFast(sec))
                        continue;

                    var unit = Unit(sec.EngineId);
                    if (unit != null)
                        unit.SecurityCountFilter++;
                }
            }



            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("engines");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = $"({v[0]},'{v[1]}','{v[2]}')";
                }

                string sql = "INSERT INTO`_moex_engines`" +
                                "(`id`,`name`,`title`)" +
                            $"VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)";
                mysql.Query(sql);

                new Engine();
            }
        }



        /// <summary>
        /// Рынки
        /// </summary>
        public class Market
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public Market()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT DISTINCT(`marketId`),COUNT(*)FROM`_moex_securities`GROUP BY`marketId`";
                var ASS = mysql.IntAss(sql);

                sql = "SELECT*FROM`_moex_markets`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    unit.SecurityCount = ASS.ContainsKey(unit.Id) ? ASS[unit.Id] : 0;
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Весь список
            public static List<MoexUnit> ListAll => UnitList;

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;

            // Список рынков с учётом фильтра
            public static List<MoexUnit> ListEngine()
            {
                var send = new List<MoexUnit>();

                if(SecurityFilter.EngineId == 0)
                    return send;

                for (int i = 0; i < UnitList.Count; i++)
                {
                    var unit = UnitList[i];
                    if (unit.EngineId == SecurityFilter.EngineId)
                        if(unit.SecurityCount > 0)
                            send.Add(unit);
                }
                return send;
            }

            // Обновление количеств бумаг на основании фильтра
            public static void CountFilter()
            {
                if (SecurityFilter.EngineId == 0)
                    return;

                // Обнуление количеств бумаг
                foreach (var unit in ListEngine())
                    unit.SecurityCountFilter = 0;

                foreach (var sec in Security.ListAll)
                {
                    if (sec.EngineId != SecurityFilter.EngineId)
                        continue;
                    if (!SecurityFilter.IsAllowFast(sec))
                        continue;

                    var unit = Unit(sec.MarketId);
                    if (unit != null)
                        unit.SecurityCountFilter++;
                }
            }

            // Порядковый номер в списке
            public static int FilterIndex()
            {
                var list = ListEngine();
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Id == SecurityFilter.MarketId)
                        return i;
                return -1;
            }





            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("markets");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = $"({v[0]},{v[1]},'{v[4]}','{v[5]}')";
                }

                string sql = "INSERT INTO`_moex_markets`" +
                                "(`id`,`engineId`,`name`,`title`)" +
                            $"VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`engineId`=VALUES(`engineId`)," +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)";
                mysql.Query(sql);

                new Market();
            }
        }





        /// <summary>
        /// Группы режимов торгов
        /// </summary>
        public class BoardGroup
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public BoardGroup()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT*FROM`_moex_boardgroups`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;

            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("boardgroups");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    int isOrderDriven = v[11] == null ? 0 : v[11];
                    values[i] = "(" +
                                    $"{v[0]}," +
                                    $"{v[1]}," +    // engineId
                                    $"{v[4]}," +    // marketId
                                    $"'{v[6]}'," +  // name
                                    $"'{v[7]}'," +  // title
                                    $"{v[8]}," +    // isDefault
                                    $"{v[10]}," +   // isTraded
                                    $"{isOrderDriven}," +
                                    $"'{v[12]}'" +  // category
                                ")";
                }

                string sql = "INSERT INTO`_moex_boardgroups`(" +
                                "`id`," +
                                "`engineId`," +
                                "`marketId`," +
                                "`name`," +
                                "`title`," +
                                "`isDefault`," +
                                "`isTraded`," +
                                "`isOrderDriven`," +
                                "`category`" +
                            $")VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`engineId`=VALUES(`engineId`)," +
                             "`marketId`=VALUES(`marketId`)," +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)," +
                             "`isTraded`=VALUES(`isTraded`)," +
                             "`isDefault`=VALUES(`isDefault`)," +
                             "`isOrderDriven`=VALUES(`isOrderDriven`)," +
                             "`category`=VALUES(`category`)";
                mysql.Query(sql);

                new BoardGroup();
            }
        }

        /// <summary>
        /// Режимы торгов
        /// </summary>
        public class Board
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            static Dictionary<string, int> NAME_ID { get; set; }
            public Board()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();
                NAME_ID = new Dictionary<string, int>();

                string sql = "SELECT*FROM`_moex_boards`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                    NAME_ID.Add(unit.Name, unit.Id);
                }
            }

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;

            // ID режима торгов по названию
            public static int IdOnName(string name) => NAME_ID.ContainsKey(name) ? NAME_ID[name] : 0;



            // ID Торговой системы по ID режима торгов
            public static int EngineId(int boardId) => ID_UNIT.ContainsKey(boardId) ? ID_UNIT[boardId].EngineId : 0;
            // Название Торговой системы по ID режима торгов
            public static string EngineName(int boardId)
            {
                int engineId = EngineId(boardId);
                if (engineId == 0)
                    return "";

                var unit = Engine.Unit(engineId);
                if (unit == null)
                    return "";

                return unit.Name;
            }


            // ID Рынка по ID режима торгов
            public static int MarketId(int boardId) => ID_UNIT.ContainsKey(boardId) ? ID_UNIT[boardId].MarketId : 0;
            // Название Рынка по ID режима торгов
            public static string MarketName(int boardId)
            {
                int marketId = MarketId(boardId);
                if (marketId == 0)
                    return "";

                var unit = Market.Unit(marketId);
                if (unit == null)
                    return "";

                return unit.Name;
            }


            // Название Группы режима по ID режима торгов
            public static string Group(int boardId)
            {
                var board = Unit(boardId);
                if (board == null)
                    return "";

                var unit = BoardGroup.Unit(board.GroupId);
                if (unit == null)
                    return "";

                return unit.Name;
            }


            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("boards");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = "(" +
                                    $"{v[0]}," +
                                    $"{v[1]}," +
                                    $"{v[2]}," +
                                    $"{v[3]}," +
                                    $"'{v[4]}'," +
                                    $"'{v[5]}'," +
                                    $"{v[6]}," +
                                    $"{v[7]}," +
                                    $"{v[8]}" +
                                ")";
                }

                string sql = "INSERT INTO`_moex_boards`(" +
                                "`id`," +
                                "`groupId`," +
                                "`engineId`," +
                                "`marketId`," +
                                "`name`," +
                                "`title`," +
                                "`isTraded`," +
                                "`hasCandles`," +
                                "`isPrimary`" +
                            $")VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`groupId`=VALUES(`groupId`)," +
                             "`engineId`=VALUES(`engineId`)," +
                             "`marketId`=VALUES(`marketId`)," +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)," +
                             "`isTraded`=VALUES(`isTraded`)," +
                             "`hasCandles`=VALUES(`hasCandles`)," +
                             "`isPrimary`=VALUES(`isPrimary`)";
                mysql.Query(sql);

                new Board();
            }
        }







        /// <summary>
        /// Группы бумаг
        /// </summary>
        public class SecurityGroup
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            static Dictionary<string, int> NAME_ID { get; set; }
            public SecurityGroup()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();
                NAME_ID = new Dictionary<string, int>();

                string sql = "SELECT*FROM`_moex_securitygroups`ORDER BY`title`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                    NAME_ID.Add(unit.Name, unit.Id);
                }
            }

            public static int IdOnName(string name) => NAME_ID.ContainsKey(name) ? NAME_ID[name] : 0;





            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("securitygroups");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = "(" +
                                    $"{v[0]}," +
                                    $"'{v[1]}'," +  // name
                                    $"'{v[2]}'," +  // title
                                    $"{v[3]}" +     // isHidden
                                ")";
                }

                string sql = "INSERT INTO`_moex_securitygroups`(" +
                                "`id`," +
                                "`name`," +
                                "`title`," +
                                "`isHidden`" +
                            $")VALUES{string.Join(",\n", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)," +
                             "`isHidden`=VALUES(`isHidden`)";
                mysql.Query(sql);

                new SecurityGroup();
            }
        }

        /// <summary>
        /// Виды бумаг
        /// </summary>
        public class SecurityType
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            static Dictionary<string, int> NAME_ID { get; set; }
            public SecurityType()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();
                NAME_ID = new Dictionary<string, int>();

                string sql = "SELECT*FROM`_moex_securitytypes`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                    NAME_ID.Add(unit.Name, unit.Id);
                }
            }

            public static int IdOnName(string name) => NAME_ID.ContainsKey(name) ? NAME_ID[name] : 0;

            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("securitytypes");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = "(" +
                                    $"{v[0]}," +
                                    $"{v[1]}," +    // engineId
                                    $"{SecurityGroup.IdOnName(v[6].ToString())}," +    // securityGroupId
                                    $"'{v[4]}'," +  // name
                                    $"'{v[5]}'" +   // title
                                ")";
                }

                string sql = "INSERT INTO`_moex_securitytypes`(" +
                                "`id`," +
                                "`engineId`," +
                                "`securityGroupId`," +
                                "`name`," +
                                "`title`" +
                            $")VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`engineId`=VALUES(`engineId`)," +
                             "`securityGroupId`=VALUES(`securityGroupId`)," +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)";
                mysql.Query(sql);

                new SecurityType();
            }
        }

        /// <summary>
        /// Коллекции бумаг
        /// </summary>
        public class SecurityСollections
        {
            static List<MoexUnit> UnitList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public SecurityСollections()
            {
                UnitList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT*FROM`_moex_securitycollections`ORDER BY`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Загрузка данных с биржи
            public static void iss()
            {
                var data = WsIssData("securitycollections");
                string[] values = new string[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = "(" +
                                    $"{v[0]}," +
                                    $"'{v[1]}'," +  // name
                                    $"'{v[2]}'," +  // title
                                    $"{v[3]}" +     // securityGroupId
                                ")";
                }

                string sql = "INSERT INTO`_moex_securitycollections`(" +
                                "`id`," +
                                "`name`," +
                                "`title`," +
                                "`securityGroupId`" +
                            $")VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                                "`name`=VALUES(`name`)," +
                                "`title`=VALUES(`title`)," +
                                "`securityGroupId`=VALUES(`securityGroupId`)";
                mysql.Query(sql);

                new SecurityСollections();
            }
        }

        /// <summary>
        /// Бумаги
        /// </summary>
        public class Security
        {
            static List<SecurityUnit> UnitList { get; set; }
            static Dictionary<int, SecurityUnit> ID_UNIT { get; set; }
            public Security()
            {
                UnitList = new List<SecurityUnit>();
                ID_UNIT = new Dictionary<int, SecurityUnit>();

                string sql = "SELECT" +
                                "`id`," +
                                "`engineId`," +
                                "`marketId`," +
                                "`name`," +
                                "`shortName`," +
                                "`secId`," +
                                "`isTraded`" +
                             "FROM`_moex_securities`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new SecurityUnit(row["id"]);
                    unit.EngineId = Convert.ToInt32(row["engineId"]);
                    unit.MarketId = Convert.ToInt32(row["marketId"]);
                    unit.Name = row["name"];
                    unit.ShortName = row["shortName"];
                    unit.SecId = row["secId"];
                    unit.IsTraded = row["isTraded"] == "1";
                    UnitList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Общее количество бумаг
            public static int Count => UnitList.Count;
            public static List<SecurityUnit> ListAll => UnitList;

            // Информация о бумаге на основании SecId
            public static SecurityUnit UnitOnSecId(string secid)
            {
                foreach (var unit in UnitList)
                    if (unit.SecId == secid)
                        return unit;
                return null;
            }

            // Текст: "1403 бумаги"
            public static string CountStr(int c = -1)
            {
                c = c == -1 ? Count : c;
                return $"{c} бумаг{format.End(c, "а", "и", "")}";
            }

            public static int FoundCount()
            {
                int count = 0;
                for (int i = 0; i < Count; i++)
                {
                    var unit = UnitList[i];
                    if (!SecurityFilter.IsAllow(unit))
                        continue;

                    count++;
                }
                return count;
            }
            public static List<SecurityUnit> ListFilter()
            {
                var send = new List<SecurityUnit>();
                for (int i = 0; i < UnitList.Count; i++)
                {
                    if (!SecurityFilter.IsAllow(UnitList[i]))
                        continue;

                    send.Add(UnitList[i]);
                    if (send.Count >= 300)
                        break;
                }
                return send;
            }



            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                int start = 0;
                while (true)
                {
                    string url = "https://iss.moex.com/iss/securities.json" +
                                     "?iss.meta=off" +
                                     "&securities.columns=" +
                                            "id," +
                                            "primary_boardid," +
                                            "type," +
                                            "group," +
                                            "secid," +
                                            "shortname," +
                                            "name" +
                                     "&is_trading=1" +
                                    $"&start={start}";
                    string str = wc.DownloadString(url);
                    dynamic json = JsonConvert.DeserializeObject(str);
                    var data = json.securities.data;

                    WriteLine(url);

                    if (data.Count == 0)
                        break;

                    string[] values = new string[data.Count];
                    for (int i = 0; i < data.Count; i++)
                    {
                        var v = data[i];
                        int boardId = Board.IdOnName(v[1].ToString());
                        values[i] = "(" +
                                        $"{v[0]}," +        // id
                                        $"{Board.EngineId(boardId)}," + // EngineId
                                        $"{Board.MarketId(boardId)}," + // MarketId
                                        $"{boardId}," +     // primary_boardid
                                        $"{SecurityGroup.IdOnName(v[2].ToString())}," + // type
                                        $"{SecurityType.IdOnName(v[3].ToString())}," +  // group
                                        $"'{v[4]}'," +      // secId
                                        $"'{v[5].ToString().Replace("'", "")}'," +      // shortName
                                        $"'{v[6].ToString().Replace("'", "")}'" +      // name
                                    ")";
                    }

                    string sql = "INSERT INTO`_moex_securities`(" +
                                    "`id`," +
                                    "`engineId`," +
                                    "`marketId`," +
                                    "`boardId`," +
                                    "`securityGroupId`," +
                                    "`securityTypeId`," +
                                    "`secId`," +
                                    "`shortName`," +
                                    "`name`" +
                                $")VALUES{string.Join(",\n", values)}" +
                                 "ON DUPLICATE KEY UPDATE" +
                                    "`engineId`=VALUES(`engineId`)," +
                                    "`marketId`=VALUES(`marketId`)," +
                                    "`boardId`=VALUES(`boardId`)," +
                                    "`securityGroupId`=VALUES(`securityGroupId`)," +
                                    "`securityTypeId`=VALUES(`securityTypeId`)," +
                                    "`secId`=VALUES(`secId`)," +
                                    "`shortName`=VALUES(`shortName`)," +
                                    "`name`=VALUES(`name`)";
                    mysql.Query(sql);

                    start += 100;
                }

                new Security();
            }
        }
    }

    /// <summary>
    /// Единица данных для Engine, Market, Board, BoardGroup, SecurityGroup, SecurityType
    /// </summary>
    public class MoexUnit
    {
        public MoexUnit(Dictionary<string, string> row)
        {
            Id = Convert.ToInt32(row["id"]);
            Name = row["name"];
            Title = row["title"];

            GroupId  = KeyInt(row, "groupId");
            EngineId = KeyInt(row, "engineId");
            MarketId = KeyInt(row, "marketId");
            SecurityGroupId = KeyInt(row, "securityGroupId");

            IsTraded   = KeyBool(row, "isTraded");
            HasCandles = KeyBool(row, "hasCandles");
            IsPrimary  = KeyBool(row, "isPrimary");
            IsDefault  = KeyBool(row, "isDefault");
            IsHidden = KeyBool(row, "IsHidden");
            IsOrderDriven = KeyBool(row, "isOrderDriven");

            Category = row.ContainsKey("category") ? row["category"] : "";
        }

        int KeyInt(Dictionary<string, string> row, string key) =>
            row.ContainsKey(key) ? Convert.ToInt32(row[key]) : 0;
        bool KeyBool(Dictionary<string, string> row, string key) =>
            row.ContainsKey(key) && row[key] != "0";

        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public int GroupId  { get; set; }
        public int EngineId { get; set; }
        public int MarketId { get; set; }
        public int SecurityGroupId { get; set; }


        public bool IsTraded   { get; set; }
        public bool HasCandles { get; set; }
        public bool IsPrimary  { get; set; }
        public bool IsDefault  { get; set; }
        public bool IsHidden  { get; set; }
        public bool IsOrderDriven { get; set; }
        public string Category { get; set; }

        public int SecurityCount { get; set; }      // Количество бумаг в определённой группе
        public int SecurityCountFilter { get; set; }// Количество бумаг в определённой группе на основании фильтра
        public Visibility SecurityCountVis { get => G.Vis(SecurityCountFilter > 0); }
    }






    /// <summary>
    /// Единица данных списка бумаг
    /// </summary>
    public class SecurityUnit
    {
        public SecurityUnit(string id) => Id = Convert.ToInt32(id);
        public int Id { get; private set; } // Идентификатор

        public int EngineId { get; set; }   // ID Торговой системы
        public int MarketId { get; set; }   // ID Рынка
        public int BoardId { get; set; }    // ID Режима торгов

        public string SecId { get; set; }   // Код ценной бумаги
        public string Name { get; set; }    // Полное наименование
        public string ShortName { get; set; }// Краткое наименование

        public bool IsTraded { get; set; }  // Бумага торгуется или нет
    }


    /// <summary>
    /// Единица данных информации о бумаге
    /// </summary>
    public class SecurityInfoUnit
    { 
        public SecurityInfoUnit(dynamic v)
        {
            Name = v[0];
            Title = v[1];
            Value = v[2];
        }
        public string Name { get; set; }
        string _Title;
        public string Title
        {
            get => $"{_Title}:";
            set => _Title = value;
        }
        public string Value { get; set; }
        public string ValueWeight => Name == "SECID" ? "Medium" : "Normal";
    }


    /// <summary>
    /// Единица данных Режима торгов бумаги
    /// </summary>
    public class BoardUnit
    { 
        public BoardUnit(int num, dynamic v)
        {
            Num = $"{num}.";
            SecId = v[0];
            Name = v[1];
            IsPrimary = v[2];
            ListedFrom = v[3];
            ListedTill = v[4];
            Title = v[5];
            IsTraded = v[6];
            Decimals = v[7];
        }
        public string Num { get; set; }
        public string SecId { get; set; }
        public string EngineName => MOEX.Board.EngineName(Id);
        public string MarketName => MOEX.Board.MarketName(Id);
        public int Id => MOEX.Board.IdOnName(Name);
        public string Name { get; set; }
        public string NameWeight => IsPrimary ? "Bold" : "Normal";
        public string Group => MOEX.Board.Group(Id);
        public string Title { get; set; }
        public string ListedFrom { get; set; }
        public string ListedTill { get; set; }
        public SolidColorBrush ListedColor => format.RGB(IsTraded ? "#000000" : "#AAAAAA");
        public bool IsPrimary { get; set; }
        public bool IsTraded { get; set; }
        public SolidColorBrush ItemBG => format.RGB(IsTraded ? "#DDFFDD" : "#F8F8F8");
        public int Decimals { get; set; }
    }


    /// <summary>
    /// Доступные таймфреймы и даты для загрузки свечной истории
    /// </summary>
    public class BorderUnit
    {
        public BorderUnit(dynamic v)
        {
            Begin = Convert.ToDateTime(v[0]);
            End = Convert.ToDateTime(v[1]);
            Interval = v[2];
        }
        public int Interval { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public string TF => Duration(Interval);


        // Ассоциативный массив таймфреймов с описаниями
        static string Duration(int i)
        {
            var ass = new Dictionary<int, string>();
            ass.Add( 1, "Минута");
            ass.Add(10, "10 минут");
            ass.Add(60, "Час");
            ass.Add(24, "День");
            ass.Add( 7, "Неделя");
            ass.Add(31, "Месяц");
            ass.Add( 4, "Квартал");
            return ass[i];
        }
        // Порядок отображенмя таймфреймов
        public static int[] Sort => new[] { 1, 10, 60, 24, 7, 31, 4 };
    }
}
