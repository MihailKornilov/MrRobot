// markets (рынки)
// boards (режимы торгов)
// board_groups (группировка режимов)

using System.Net;
using System.Text;
using System.Collections.Generic;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;
using System;

namespace MrRobot.Entity
{
    public class MOEX
    {
        public class Engine
        {
            static List<EngineUnit> EngineList { get; set; }
            static Dictionary<int, EngineUnit> ID_UNIT { get; set; }
            public Engine()
            {
                EngineList = new List<EngineUnit>();
                ID_UNIT = new Dictionary<int, EngineUnit>();

                string sql = "SELECT*FROM`_moex_engines`ORDER BY`id`";
                foreach (Dictionary<string, string> row in mysql.QueryList(sql))
                {
                    int id = Convert.ToInt32(row["id"]);
                    var unit = new EngineUnit(id, row["name"], row["title"]);
                    EngineList.Add(unit);
                    ID_UNIT.Add(id, unit);
                }
            }

            // Весь список
            public static List<EngineUnit> ListAll() => EngineList;

            // Единица на основании ID
            public static EngineUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;


            // Загрузка данных с биржи
            public static void iss()
            {
                var wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                string url = "https://iss.moex.com/iss/index.json?iss.only=engines";
                string str = wc.DownloadString(url);
                dynamic json = JsonConvert.DeserializeObject(str);

                var data = json.engines.data;

                string[] values = new string[data.Count];
                for(int i = 0; i < data.Count; i++)
                {
                    var v = data[i];
                    values[i] = $"({v[0]},'{v[1]}','{v[2]}')";
                }

                string sql = "INSERT INTO`_moex_engines`" +
                            $"VALUES{string.Join(",", values)}" +
                             "ON DUPLICATE KEY UPDATE" +
                             "`name`=VALUES(`name`)," +
                             "`title`=VALUES(`title`)";
                mysql.Query(sql);

                new Engine();
            }
        }
    }

    public class EngineUnit
    {
        public EngineUnit(int id, string name, string title)
        {
            Id = id;
            Name = name;
            Title = title;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
    }
}
