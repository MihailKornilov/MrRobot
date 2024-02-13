// markets (рынки)
// boards (режимы торгов)
// board_groups (группировка режимов)

using System;
using System.Net;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;

namespace MrRobot.Connector
{
    public class MOEX
    {
        public MOEX()
        {
            new Engine();
            new Market();
            new Board();
            new BoardGroup();
            new Security();
        }




        public class Engine
        {
            static List<MoexUnit> EngineList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public Engine()
            {
                EngineList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT DISTINCT(`engineId`),COUNT(*)FROM`_moex_securities`GROUP BY`engineId`";
                var ASS = mysql.IntAss(sql);

                sql = "SELECT*FROM`_moex_engines`ORDER BY`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    if (ASS.ContainsKey(unit.Id))
                        unit.SecurityCount = ASS[unit.Id];
                    EngineList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Весь список
            public static List<MoexUnit> ListAll() => EngineList;

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;


            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                string url = "https://iss.moex.com/iss/engines.json?iss.meta=off";
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);

                var data = json.engines.data;

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




        public class Market
        {
            static List<MoexUnit> MarketList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public Market()
            {
                MarketList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT DISTINCT(`marketId`),COUNT(*)FROM`_moex_securities`GROUP BY`marketId`";
                var ASS = mysql.IntAss(sql);

                sql = "SELECT*FROM`_moex_markets`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    if(ASS.ContainsKey(unit.Id))
                        unit.SecurityCount = ASS[unit.Id];
                    MarketList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Весь список
            public static List<MoexUnit> ListAll() => MarketList;
            // Рынки выбранного режима торгов
            public static List<MoexUnit> ListEngine(int engineId)
            {
                var send = new List<MoexUnit>();
                for (int i = 0; i < MarketList.Count; i++)
                {
                    var unit = MarketList[i];
                    if (unit.EngineId == engineId)
                        send.Add(unit);
                }
                return send;
            }

            // Единица на основании ID
            public static MoexUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;

            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                string url = "https://iss.moex.com/iss/index.json?iss.only=markets&iss.meta=off";
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);

                var data = json.markets.data;

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






        public class Board
        {
            static List<MoexUnit> BoardList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            static Dictionary<string, int> NAME_ID { get; set; }
            public Board()
            {
                BoardList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();
                NAME_ID = new Dictionary<string, int>();

                string sql = "SELECT*FROM`_moex_boards`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    BoardList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                    NAME_ID.Add(unit.Name, unit.Id);
                }
            }
            public static int IdOnName(string name) => NAME_ID.ContainsKey(name) ? NAME_ID[name] : 0;


            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                string url = "https://iss.moex.com/iss/index.json?iss.only=boards&iss.meta=off";
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);
                var data = json.boards.data;

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







        public class BoardGroup
        {
            static List<MoexUnit> GroupList { get; set; }
            static Dictionary<int, MoexUnit> ID_UNIT { get; set; }
            public BoardGroup()
            {
                GroupList = new List<MoexUnit>();
                ID_UNIT = new Dictionary<int, MoexUnit>();

                string sql = "SELECT*FROM`_moex_boardgroups`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new MoexUnit(row);
                    GroupList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }

            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;

                string url = "https://iss.moex.com/iss/index.json?iss.only=boardgroups&iss.meta=off";
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);
                var data = json.boardgroups.data;

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







        public class Security
        {
            static List<SecurityUnit> SecurityList { get; set; }
            static Dictionary<int, SecurityUnit> ID_UNIT { get; set; }
            public Security()
            {
                SecurityList = new List<SecurityUnit>();
                ID_UNIT = new Dictionary<int, SecurityUnit>();

                string sql = "SELECT*FROM`_moex_securities`ORDER BY`engineId`,`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    var unit = new SecurityUnit(Convert.ToInt32(row["id"]));
                    unit.EngineId = Convert.ToInt32(row["engineId"]);
                    unit.MarketId = Convert.ToInt32(row["marketId"]);
                    unit.Name = row["name"];
                    unit.ShortName = row["shortName"];
                    unit.SecId = row["secId"];
                    unit.IsTraded = row["isTraded"] == "1";
                    SecurityList.Add(unit);
                    ID_UNIT.Add(unit.Id, unit);
                }
            }
            
            // Обработка поискового текста
            static bool UnitFind(SecurityUnit unit, string find)
            {
                if (find.Length == 0)
                    return true;
                if (unit.Id.ToString() == find)
                    return true;
                if (unit.Name.ToLower().Contains(find))
                    return true;
                if (unit.ShortName.ToLower().Contains(find))
                    return true;
                return false;
            }
            public static int FoundCount(string find)
            {
                find = find.ToLower();
                int count = 0;
                for (int i = 0; i < SecurityList.Count; i++)
                    if (UnitFind(SecurityList[i], find))
                        count++;
                return count;
            }
            public static List<SecurityUnit> List1000(string find)
            {
                find = find.ToLower();
                var send = new List<SecurityUnit>();
                for (int i = 0; i < SecurityList.Count; i++)
                {
                    if (!UnitFind(SecurityList[i], find))
                        continue;

                    send.Add(SecurityList[i]);
                    if (send.Count >= 1000)
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
                        values[i] = "(" +
                                        $"{v[0]}," +    // id
                                        $"'{v[1]}'," +  // secId
                                        $"'{v[2].ToString().Replace("'", "")}'," +  // shortName
                                        $"'{v[4].ToString().Replace("'", "")}'," +  // name
                                        $"'{v[5]}'," +  // isin
                                        $"{v[6]}," +    // isTraded
                                        $"{Board.IdOnName(v[14].ToString())}" +    // boardId
                                    ")";
                    }

                    string sql = "INSERT INTO`_moex_securities`(" +
                                    "`id`," +
                                    "`secId`," +
                                    "`shortName`," +
                                    "`name`," +
                                    "`isin`," +
                                    "`isTraded`," +
//                                    "``," +
//                                    "``," +
                                    "`boardId`" +
                                $")VALUES{string.Join(",", values)}" +
                                 "ON DUPLICATE KEY UPDATE" +
                                 "`secId`=VALUES(`secId`)," +
                                 "`shortName`=VALUES(`shortName`)," +
                                 "`name`=VALUES(`name`)," +
                                 "`isin`=VALUES(`isin`)," +
                                 "`isTraded`=VALUES(`isTraded`)," +
                                 "`boardId`=VALUES(`boardId`)";
                    mysql.Query(sql);

                    start += 100;
                }

                new Security();
            }
        }
    }

    /// <summary>
    /// Единица данных для Engine, Market, Board, BoardGroup
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

            IsTraded   = KeyBool(row, "isTraded");
            HasCandles = KeyBool(row, "hasCandles");
            IsPrimary  = KeyBool(row, "isPrimary");
            IsDefault  = KeyBool(row, "isDefault");
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


        public bool IsTraded   { get; set; }
        public bool HasCandles { get; set; }
        public bool IsPrimary  { get; set; }
        public bool IsDefault  { get; set; }
        public bool IsOrderDriven { get; set; }
        public string Category { get; set; }

        public int SecurityCount { get; set; }
        public Visibility SecurityCountVis { get => global.Vis(SecurityCount > 0); }
    }







    public class SecurityUnit
    {
        public SecurityUnit(int id) => Id = id;
        public int Id { get; set; }
        public int EngineId { get; set; }
        public int MarketId { get; set; }
        public int BoardId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string SecId { get; set; }
        public string Isin { get; set; }
        public bool IsTraded { get; set; }
    }
}
