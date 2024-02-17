using System;
using System.Collections.Generic;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public class Market
    {
        public delegate void Dlgt();
        public static Dlgt Updated = () => { };

        // Список бирж
        static List<MarketUnit> MarketList { get; set; }

        /// <summary>
        /// Ассоциативный массив ID и свечных данных (для быстрого поиска)
        /// </summary>
        static Dictionary<int, MarketUnit> ID_UNIT { get; set; }

        /// <summary>
        /// Загрузка списка бирж из базы
        /// </summary>
        public Market()
        {
            MarketList = new List<MarketUnit>();
            ID_UNIT = new Dictionary<int, MarketUnit>();

            string sql = "SELECT*FROM`_market`ORDER BY`id`";
            foreach (Dictionary<string, string> row in mysql.QueryList(sql))
            {
                var unit = new MarketUnit(row);
                MarketList.Add(unit);
                ID_UNIT.Add(unit.Id, unit);
            }
            Updated();
        }

        public static List<MarketUnit> ListAll() => MarketList;

        /// <summary>
        /// Единица бмржм на основании ID
        /// </summary>
        public static MarketUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;


        /// <summary>
        /// Единица бмржм на основании Prefix
        /// </summary>
        public static MarketUnit UnitOnPrefix(string prefix)
        {
            if (prefix.Length == 0)
                return null;

            foreach(var unit in MarketList)
                if(unit.Prefix == prefix)
                    return unit;

            return null;
        }
    }


    /// <summary>
    /// Шаблон единицы списка биржи
    /// </summary>
    public class MarketUnit
    {
        public MarketUnit(Dictionary<string, string> row)
        {
            Id = Convert.ToInt32(row["id"]);
            Name = row["name"];
            Prefix = row["prefix"];
            Url = row["url"];
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Prefix { get; set; }
        public string Url { get; set; }
    }
}
