using System;
using System.Collections.Generic;
using static System.Console;

using MySqlConnector;
using MrRobot.Entity;

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
            global.LogWrite(sql);

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
        public static List<string> ChartCandles(string sql)
        {
            new mysql(sql, true);

            var CandlesData = new List<string>();
            var VolumesData = new List<string>();
            while (res.Read())
            {
                var cndl = new CandleUnit
                {
                    Unix = res.GetInt32("unix"),
                    High = res.GetDouble("high"),
                    Open = res.GetDouble("open"),
                    Close = res.GetDouble("close"),
                    Low = res.GetDouble("low"),
                    Volume = res.GetDouble("vol")
                };

                CandlesData.Add(cndl.CandleToChart());
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
        public static List<object> CandlesData(string sql, ulong Exp = 0)
        {
            new mysql(sql, true);

            var Data = new List<object>();
            while (res.Read())
                Data.Add(new CandleUnit
                {
                    Unix = res.GetInt32("unix"),
                    High = res.GetDouble("high"),
                    Open = res.GetDouble("open"),
                    Close = res.GetDouble("close"),
                    Low = res.GetDouble("low"),
                    Volume = res.GetDouble("vol"),
                    Exp = Exp
                });

            Finish(sql);

            return Data;
        }




        static Dictionary<int, List<CandleUnit>> ConvertCandles_Cache;
        /// <summary>
        /// Получение данных о свечах для Конвертера
        /// </summary>
        public static List<CandleUnit> ConvertCandles(string sql)
        {
            if(ConvertCandles_Cache == null)
                ConvertCandles_Cache = new Dictionary<int, List<CandleUnit>>();

            int hash = sql.GetHashCode();

            if(ConvertCandles_Cache.ContainsKey(hash))
                return ConvertCandles_Cache[hash];

            new mysql(sql, true);

            var Data = new List<CandleUnit>();
            while (res.Read())
                Data.Add(new CandleUnit
                {
                    Unix = res.GetInt32("unix"),
                    High = res.GetDouble("high"),
                    Open = res.GetDouble("open"),
                    Close = res.GetDouble("close"),
                    Low = res.GetDouble("low"),
                    Volume = res.GetDouble("vol")
                });

            ConvertCandles_Cache[hash] = Data;
            Finish(sql);

            return Data;
        }



        /// <summary>
        /// Загрузка свечей и формирование массивов для поиска паттернов
        /// </summary>
        public static void PatternSearchMass(string sql, ulong Exp, int[] Unix, double[] Price, int[] WickTop, int[] Body, int[] WickBtm)
        {
            new mysql(sql, true);

            int i = 0;
            while (res.Read())
            {
                Unix[i] = res.GetInt32(0);

                double high = res.GetDouble(1),
                       open = res.GetDouble(2),
                       low = res.GetDouble(4);

                Price[i] = res.GetDouble(3); // close

                WickTop[i] = Convert.ToInt32((open > Price[i] ? high - open : high - Price[i]) * Exp);
                Body[i] = Convert.ToInt32((Price[i] - open) * Exp);
                WickBtm[i] = Convert.ToInt32((open < Price[i] ? open - low : Price[i] - low) * Exp);

                i++;
            }

            Finish(sql);
        }
    }
}
