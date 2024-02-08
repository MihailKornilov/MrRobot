using System;
using System.Collections.Generic;
using static System.Console;

using MySqlConnector;

using MrRobot.Entity;
using MrRobot.Section;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Diagnostics;

namespace MrRobot.inc
{
    public class mysql
    {
        static string DataBase = "mrrobot";
        static string Config = "server=127.0.0.1;" +
                               "uid=root;" +
                               "pwd=4909099;" +
                              $"database={DataBase};" +
                               "Convert Zero Datetime=True;";

        static MySqlConnection conn;
        static MySqlCommand cmd;
        static MySqlDataReader res;

        static int SqlCount = 0;    // Общее количество SQL-запросов
        static long UnixLast;       // Время в Unix когда был сделан последний запрос
        static Dur dur;             // Измерение скорости запроса


        public mysql(string sql, bool isRes = false)
        {
            try
            {
                dur = new Dur();
                conn = new MySqlConnection(Config);
                conn.Open();
                cmd = new MySqlCommand(sql, conn);
                res = isRes ? cmd.ExecuteReader() : null;
            }
            catch (MySqlException ex)
            {
                global.LogWrite($"ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ: {ex.Message}");
                Environment.Exit(0);
            }
        }


        /// <summary>
        /// Закрытие соединения и измерение скорости каждого SQL-запроса
        /// </summary>
        static void Finish(string sql = "")
        {
            conn.Close();
            conn = null;
            cmd = null;
            res = null;

            return;

            long Unix = format.UnixNow_MilliSec();
            if (Unix - UnixLast > 1000)
                WriteLine();
            UnixLast = Unix;

            string txt = "SQL." + ++SqlCount + ": " + dur.Second() + " " + sql;
            if (txt.Length > 500)
                txt = txt.Substring(0, 500);
            WriteLine(txt);
        }


        /// <summary>
        /// Получение названия таблицы из запроса
        /// </summary>
        static string TableName(string sql)
        {
            bool isFrom = false;
            foreach (string str in sql.ToLower().Split('`'))
            {
                if (isFrom)
                    return str;
                if(str.Contains("from"))
                    isFrom = true;
            }
            return "";
        }




        /// <summary>
        /// Внесение INSERT, удаление DELETE, обновление UPDATE данных
        /// </summary>
        public static int Query(string sql)
        {
            new mysql(sql);
            cmd.ExecuteNonQuery();
            int InsertedId = Convert.ToInt32(cmd.LastInsertedId);
            Finish(sql);
            return InsertedId;
        }

        /// <summary>
        /// Количество
        /// </summary>
        public static int Count(string sql)
        {
            new mysql(sql, true);

            res.Read();
            int count = res.GetInt32(0);

            Finish(sql);

            return count;
        }

        /// <summary>
        /// Получение списка, состоящего из одного стоблца
        /// </summary>
        public static string[] QueryColOne(string sql)
        {
            new mysql(sql, true);

            if (!res.HasRows)
                return new string[0];

            var list = new List<string>();
            while (res.Read())
                list.Add(res.GetValue(0).ToString());

            int i = 0;
            string[] mass = new string[list.Count];
            foreach (string v in list)
                mass[i++] = v;

            Finish(sql);

            return mass;
        }

        /// <summary>
        /// Идентификаторы через запятую
        /// </summary>
        public static string Ids(string sql)
        {
            new mysql(sql, true);

            if (!res.HasRows)
            {
                Finish(sql);
                return "0";
            }

            var list = new List<string>();
            while (res.Read())
                list.Add(res.GetValue(0).ToString());

            string send = string.Join(",", list.ToArray());

            Finish(sql);

            return send;
        }

        /// <summary>
        /// Получение списка из базы в виде словаря с ключами и значениями
        /// </summary>
        public static List<object> QueryList(string sql)
        {
            new mysql(sql, true);

            List<object> mass = new List<object>();
            while (res.Read())
            {
                var row = new Dictionary<string, string>();

                for (int i = 0; i < res.FieldCount; i++)
                    row.Add(res.GetName(i), res.GetValue(i).ToString());

                mass.Add(row);
            }

            Finish(sql);

            return mass;
        }

        /// <summary>
        /// Получение одной строки из базы
        /// </summary>
        public static Dictionary<string, string> QueryOne(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<string, string>();

            if (res.Read())
                for (int i = 0; i < res.FieldCount; i++)
                    send.Add(res.GetName(i), res.GetValue(i).ToString());

            Finish(sql);

            return send;
        }

        /// <summary>
        /// Получение одного текстового поля из базы
        /// </summary>
        public static string QueryString(string sql)
        {
            new mysql(sql);
            string send = cmd.ExecuteScalar()?.ToString();
            Finish(sql);
            return send;
        }

        /// <summary>
        /// Получение ассоциативного массива на основании id
        /// В запросе должно быть обязательно указано только два поля
        /// Например: SELECT `id`,`name` FROM `table`
        /// </summary>
        public static Dictionary<int, int> IntAss(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<int, int>();

            while (res.Read())
            {
                int id = res.GetInt32(0);
                int val = res.GetInt32(1);
                send.Add(id, val);
            }

            Finish(sql);

            return send;
        }

        /// <summary>
        /// Получение ассоциативного массива на основании id
        /// В запросе должно быть обязательно указано только два поля
        /// Например: SELECT `id`,`name` FROM `table`
        /// </summary>
        public static Dictionary<int, string> IntStringAss(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<int, string>();

            while (res.Read())
            {
                int id = res.GetInt32(0);
                string val = res.GetValue(1).ToString();
                send.Add(id, val);
            }

            Finish(sql);

            return send;
        }
        /// <summary>
        /// Получение ассоциативного массива на основании id
        /// В запросе должно быть обязательно указано только два поля
        /// Например: SELECT `id`,`name` FROM `table`
        /// </summary>
        public static Dictionary<string, int> StringIntAss(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<string, int>();

            while (res.Read())
            {
                string id = res.GetValue(0).ToString();
                int val = res.GetInt32(1);
                send.Add(id, val);
            }

            Finish(sql);

            return send;
        }

        /// <summary>
        /// Получение ассоциативного массива на основании двух произвольных полей в базе
        /// В запросе должно быть обязательно указано только два поля
        /// Например: SELECT `name`,`value` FROM `table`
        /// </summary>
        public static Dictionary<string, string> StringAss(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<string, string>();

            while (res.Read())
            {
                string name = res.GetValue(0).ToString();
                string val = res.GetValue(1).ToString();
                send.Add(name, val);
            }

            Finish(sql);

            return send;
        }

        /// <summary>
        /// Словарь, который получает строку по ID
        /// </summary>
        public static Dictionary<int, object> IdRowAss(string sql)
        {
            new mysql(sql, true);

            var send = new Dictionary<int, object>();
            while (res.Read())
            {
                var row = new Dictionary<string, string>();

                for (int i = 0; i < res.FieldCount; i++)
                    row.Add(res.GetName(i), res.GetValue(i).ToString());

                send.Add(res.GetInt32("id"), row);
            }

            Finish(sql);

            return send;
        }



        /// <summary>
        /// Получение данных о свечах для графика
        /// </summary>
        public static List<string> ChartCandles(string sql, bool msec = false)
        {
            new mysql(sql, true);

            var CandlesData = new List<string>();
            var VolumesData = new List<string>();
            while (res.Read())
            {
                var cndl = new CandleUnit(res);
                CandlesData.Add(cndl.CandleToChart(msec: msec));
                VolumesData.Add(cndl.VolumeToChart());
            }

            CandlesData.Reverse();
            VolumesData.Reverse();

            Finish(sql);

            return new List<string>() {
                string.Join(",\n", CandlesData.ToArray()),
                string.Join(",\n", VolumesData.ToArray())
            };
        }
        /// <summary>
        /// Получение данных о свечах для Робота
        /// </summary>
        public static List<object> CandlesData(string sql, CDIparam param = null)
        {
            ProBar bar = null;
            if (param != null)
            {
                var CDI = Candle.Unit(param.Id);
                bar = new ProBar(CDI.RowsCount);
            }

            new mysql(sql, true);
            var Data = new List<object>();
            while (res.Read())
            {
                Data.Add(new CandleUnit(res));

                if (param != null)
                    bar.Val(Data.Count, param.Progress);
            }

            Finish(sql);

            return Data;
        }
        /// <summary>
        /// Проверка свечных данных выбранного таймфрейма, чтобы присутствовала каждая свеча одна за другой
        /// </summary>
        public static bool CandleDataCheck(string TableName)
        {
            int tf = Convert.ToInt32(TableName.Split('_')[2]);
            int step = tf * 60;

            string sql = "SELECT`unix`" +
                        $"FROM`{TableName}`" +
                         "ORDER BY`unix`";
            new mysql(sql, true);

            res.Read();
            int unix = res.GetInt32(0) + step;
            while (res.Read())
            {
                if (unix != res.GetInt32(0))
                    return false;
                unix += step;
            }
            
            Finish(sql);

            return true;
        }




        static Dictionary<int, List<CandleUnit>> ConvertCandles_Cache;
        /// <summary>
        /// Получение данных о свечах для Конвертера
        /// </summary>
        public static List<CandleUnit> CandlesDataCache(string sql, CDIparam param = null)
        {
            if(ConvertCandles_Cache == null)
                ConvertCandles_Cache = new Dictionary<int, List<CandleUnit>>();

            int hash = sql.GetHashCode();

            if(ConvertCandles_Cache.ContainsKey(hash))
                return ConvertCandles_Cache[hash];

            ProBar SubBar = null;
            int i = 0;
            if (param != null)
            {
                var CDI = Candle.Unit(param.Id);
                SubBar = new ProBar(CDI.RowsCount);
            }

            new mysql(sql, true);
            var Data = new List<CandleUnit>();
            bool CacheSave = true;
            while (res.Read())
            {
                Data.Add(new CandleUnit(res));

                if (param == null)
                    continue;
                if (!param.IsProcess)
                {
                    CacheSave = false;
                    break;
                }
                if (!SubBar.Val(i++, param.Progress))
                    continue;

                param.Bar.isUpd(i);
                param.ProgressMainValue = (double)param.Bar.Value;
            }

            if(CacheSave)
                ConvertCandles_Cache[hash] = Data;

            Finish(sql);

            return Data;
        }



        /// <summary>
        /// Загрузка свечей и формирование массивов для поиска паттернов
        /// </summary>
        public static List<PatternUnit> PatternSearchMass(string sql, PatternSearchParam PARAM, int count)
        {
            PARAM.ProсessInfo = "Загрузка свечных данных...";
            PARAM.PBar.Report(0);

            var bar = new ProBar(count);
            int i = 0;

            new mysql(sql, true);

            var CandleList = new List<CandleUnit>();
            var PatternList = new List<PatternUnit>();
            int PatternLength = PARAM.PatternLength;

            while (res.Read())
            {
                if (bar.isUpd(i++))
                {
                    if (!PARAM.IsProcess)
                        break;

                    PARAM.PBar.Report(bar.Value);
                }

                CandleList.Add(new CandleUnit(res));

                if (CandleList.Count > PatternLength)
                    CandleList.RemoveRange(0, 1);
                if (CandleList.Count != PatternLength)
                    continue;

                var patt = new PatternUnit(CandleList, PARAM.CdiId, PARAM.PrecisionPercent);
                if(patt.Size > 0)
                    PatternList.Add(patt);
            }

            Finish(sql);

            PARAM.ProсessInfo = "";

            return PatternList;
        }
    }
}
