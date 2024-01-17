using System;
using System.Linq;
using System.Collections.Generic;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public class Patterns
    {
        static List<object> PatternList { get; set; }

        /// <summary>
        /// Загрузка из базы списка свечных данных
        /// </summary>
        public static void ListCreate(bool upd = false)
        {
            if (!upd && PatternList != null && PatternList.Count > 0)
                return;

            PatternList = new List<object>();

            var sql = "SELECT DISTINCT(`searchId`)FROM`_pattern_found`";
            string SearchIds = mysql.Ids(sql);
            if (SearchIds == "0")
                return;

            sql = "SELECT*" +
                  "FROM`_pattern_search`" +
                 $"WHERE`id`IN({SearchIds})";
            var Search = mysql.IdRowAss(sql);

            sql = "SELECT*" +
                  "FROM`_pattern_found`" +
                  "ORDER BY`searchId`,`repeatCount` DESC";
            foreach (Dictionary<string, string> row in mysql.QueryList(sql))
            {
                int sid = Convert.ToInt32(row["searchId"]);
                var PS = Search[sid] as Dictionary<string, string>;
                PatternList.Add(new PatternUnit
                {
                    Id = Convert.ToInt32(row["id"]),
                    SearchId = sid,
                    CdiId = Convert.ToInt32(PS["cdiId"]),

                    PrecisionPercent = Convert.ToInt32(PS["scatterPercent"]),
                    FoundRepeatMin = Convert.ToInt32(PS["foundRepeatMin"]),
                    FoundCount = Convert.ToInt32(PS["foundCount"]),
                    Duration = PS["duration"],
                    Dtime = format.DateOne(PS["dtimeAdd"]),

                    Size = Convert.ToInt32(row["size"]),
                    StructDB = row["structure"],
                    UnixList = Array.ConvertAll(row["unixList"].Split(','), s => int.Parse(s)).ToList(),

                    ProfitCount = Convert.ToInt32(row["profitCount"]),
                    LossCount = Convert.ToInt32(row["lossCount"])
                });
            }
        }

        /// <summary>
        /// Весь список паттернов
        /// </summary>
        public static List<object> All()
        {
            ListCreate();
            return PatternList;
        }
        /// <summary>
        /// Список паттернов определённого поиска
        /// </summary>
        public static List<PatternUnit> List(int searchId)
        {
            ListCreate();

            var list = new List<PatternUnit>();
            int num = 1;
            foreach(PatternUnit unit in PatternList)
                if(unit.SearchId == searchId)
                {
                    unit.Num = num++;
                    list.Add(unit);
                }

            return list;
        }
        /// <summary>
        /// Индекс конкретного паттерна для указания в списке
        /// </summary>
        public static int Index(int searchId, int id)
        {
            if (id == 0)
                return 0;

            int index = 0;
            foreach(var unit in List(searchId))
            {
                if (unit.Id == id)
                    return index;
                index++;
            }

            return index;
        }
    }



    /// <summary>
    /// Единица паттерна
    /// </summary>
    public class PatternUnit
    {
        // ---=== ФОРМИРОВАНИЕ ПАТТЕРНА ===---
        public PatternUnit() { }
        public PatternUnit(List<CandleUnit> list, int cdiId)
        {
            foreach (var cndl in list)
            {
                if (cndl.High == cndl.Low)
                    return;

                PriceMax = cndl.High;
                PriceMin = cndl.Low;
            }

            CdiId = cdiId;
            Size = (int)Math.Round((PriceMax - PriceMin) * Exp);

            CandleList = new List<CandleUnit>();
            foreach (var cndl in list)
                CandleList.Add(new CandleUnit(cndl, PriceMax, PriceMin));

            StructFromCandle();
        }

        // Создание нового паттерна, используя существующий. Применяется в роботах.
        public PatternUnit Create(List<dynamic> list, int cdiId)
        {
            var newList = new List<CandleUnit>();
            foreach (var cndl in list)
                newList.Add(cndl);

            return new PatternUnit(newList, cdiId);
        }

        double _PriceMax;
        double PriceMax
        {
            get { return _PriceMax; }
            set
            {
                if (_PriceMax < value)
                    _PriceMax = value;
            }
        }
        double _PriceMin;
        double PriceMin
        {
            get { return _PriceMin; }
            set
            {
                if (_PriceMin == 0 || _PriceMin > value)
                    _PriceMin = value;
            }
        }
        public int Size { get; set; }   // Размер паттерна в пунктах
        public List<CandleUnit> CandleList { get; set; } // Состав паттерна из свечей
        // Сравнение паттернов
        public bool Compare(PatternUnit PU)
        {
            for (int k = 0; k < Length; k++)
            {
                var src = StructArr[k];
                var dst = PU.CandleList[k];

                if (src[0] != dst.SpaceTopInt)
                    return false;
                if (src[1] != dst.WickTopInt)
                    return false;
                if (src[2] != dst.BodyInt)
                    return false;
                if (src[3] != dst.WickBtmInt)
                    return false;
                if (src[4] != dst.SpaceBtmInt)
                    return false;
            }
            return true;
        }
        // Содержание паттерна: свечи с учётом пустот сверху и снизу
        void StructFromCandle()
        {
            string[] arr = new string[CandleList.Count];

            for (int k = 0; k < CandleList.Count; k++)
                arr[k] = CandleList[k].Struct();

            StructDB = string.Join(";", arr);
        }


        // Длина паттерна
        int _Length;
        public int Length
        {
            get
            {
                if (_Length == 0)
                    _Length = StructDB.Split(';').Length;
                return _Length;
            }
            set { _Length = value; }
        }



        // ---=== НАЙДЕННЫЕ ПАТТЕРНЫ ===---
        public int Id { get; set; }
        public string IdStr { get { return "#" + Id; } }
        public int Num { get; set; }            // Порядковый номер
        // Количество повторений паттерна на графике
        public int Repeat { get { return UnixList.Count; } }
        // Времена найденных паттернов в формате UNIX
        public List<int> UnixList { get; set; } = new List<int>();

        // Структура паттерна: свечи в процентах
        public string Struct { get { return StructDB.Replace(';', '\n'); } }
        public string StructDB { get; set; }

        // Структура паттерна в виде массива процентов
        List<int[]> _StructArr;
        public List<int[]> StructArr
        {
            get
            {
                if(_StructArr != null)
                    return _StructArr;

                _StructArr = new List<int[]>();

                foreach(var cndl in StructDB.Split(';'))
                {
                    int[] prc = Array.ConvertAll(cndl.Split(' '), s => int.Parse(s));
                    _StructArr.Add(prc);
                }

                return _StructArr;
            }
        }

        // Строка внесения паттерна в базу
        public string Insert(int SearchId)
        {
            return "(" +
                $"{SearchId}," +
                $"{Size}," +
                $"'{StructDB}'," +
                $"{Repeat}," +
                $"'{string.Join(",", UnixList.ToArray())}'" +
            ")";
        }




        // ---=== ИНФОРМАЦИЯ О СВЕЧНЫХ ДАННЫХ ===---
        public int CdiId { get; set; }          // ID свечных данных из `_candle_data_info`
        // Название инструмента
        public string Symbol { get { return Candle.Unit(CdiId).Name; } }
        // Таймфрейм
        public int TimeFrame { get { return Candle.Unit(CdiId).TimeFrame; } }
        // Таймфрейм в виде 10m
        public string TF { get { return format.TF(TimeFrame); } }
        // Количество нулей после запятой
        public int NolCount { get { return Candle.Unit(CdiId).NolCount; } }
        public ulong Exp { get { return Candle.Unit(CdiId).Exp; } }
        // Количество свечей в графике
        public string CandlesCountStr
        {
            get
            {
                int count = Candle.Unit(CdiId).RowsCount;
                return Candle.CountTxt(count);
            }
        }




        // ---=== ИНФОРМАЦИЯ О ПОИСКЕ ===---
        public int SearchId { get; set; }       // ID поиска из `_pattern_search`
        public int PrecisionPercent { get; set; }// Точность в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений
        public int FoundCount { get; set; }     // Количество найденных паттернов
        public string Duration { get; set; }    // Время выполнения
        public string Dtime { get; set; }       // Дата и время поиска




        // Результат теста
        public bool IsTested { get { return ProfitCount > 0 || LossCount > 0; } }
        public int ProfitCount { get; set; }    // Количество прибыльных результатов
        public int LossCount { get; set; }      // Количество убыточных результатов
        public int ProfitPercent { get; set; }  // Процент прибыльности
    }
}
