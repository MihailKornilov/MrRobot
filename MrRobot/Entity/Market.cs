using System;
using System.Collections.Generic;

using MrRobot.inc;
using MrRobot.Section;

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
                int id = Convert.ToInt32(row["id"]);
                var unit = new MarketUnit(id, row["name"]);
                MarketList.Add(unit);
                ID_UNIT.Add(id, unit);
            }
            Updated();
        }

        public static List<MarketUnit> ListAll() => MarketList;

        /// <summary>
        /// Единица бмржм на основании ID
        /// </summary>
        public static MarketUnit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;
    }


    /// <summary>
    /// Шаблон единицы списка биржи
    /// </summary>
    public class MarketUnit
    {
        public MarketUnit(int id, string name)
        {
            Id = id;
            Name = name;
        }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
