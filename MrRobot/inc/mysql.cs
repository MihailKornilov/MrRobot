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


        public delegate void DLGT(MySqlDataReader rs);


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
                G.LogWrite($"ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ: {ex.Message}\n{sql}");
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
            G.LogWrite(txt);
        }













        /// <summary>
        /// Получение списка из базы в виде словаря с ключами и значениями
        /// </summary>
        public static List<object> QueryList(string sql)
        {
            new mysql(sql, true);

            var mass = new List<object>();
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
    }
}
