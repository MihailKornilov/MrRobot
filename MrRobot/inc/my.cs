using System;
using System.Collections.Generic;
using static System.Console;

using MySqlConnector;

namespace MrRobot.inc
{
	public class my
	{
		string Server = "127.0.0.1";
		string Uid = "root";
		string Pwd = "4909099";

		string CFG(string db) =>
			$"server={Server};" +
			$"uid={Uid};" +
			$"pwd={Pwd};" +
			$"database={db};" +
			 "Convert Zero Datetime=True;";

		public static my Main { get; set; }
		public static my Data { get; set; }
		public static my Obt { get; set; }	// OrderBookTrade

		public my()
		{
			Main = new my("mrrobot");
			Data = new my("candles");
			Obt  = new my("orderbooktrade");
		}
		my(string db)
		{
			try
			{
				Cmd = new MySqlCommand();
				Cmd.Connection = new MySqlConnection(CFG(db));
				Cmd.Connection.Open();
			}
			catch (MySqlException ex)
			{
				G.LogWrite($"Не удалось подключиться к базе {db}: {ex.Message}");
				Environment.Exit(0);
			}
		}


		MySqlCommand Cmd { get; set; }
		MySqlDataReader Res { get; set; }
		void   DataReader(string sql)
		{
			Log(sql);
			Cmd.CommandText = sql;
			Res = Cmd.ExecuteReader();
		}
		void   DataReaderClose(string sql)
		{
			Res.Close();
			Log(sql);
		}
		bool   DataReaderClose(string sql, bool val)
		{
			DataReaderClose(sql);
			return val;
		}
		int	   DataReaderClose(string sql, int val)
		{
			DataReaderClose(sql);
			return val;
		}
		string DataReaderClose(string sql, string val)
		{
			DataReaderClose(sql);
			return val;
		}
		Dictionary<int, int> DataReaderClose(string sql, Dictionary<int, int> val)
		{
			DataReaderClose(sql);
			return val;
		}
		Dictionary<string, string> DataReaderClose(string sql, Dictionary<string, string> val)
		{
			DataReaderClose(sql);
			return val;
		}




		public delegate void VOID(MySqlDataReader rs);
		public delegate bool BOOL(MySqlDataReader rs);

		/// <summary>
		/// Закрытие соединения с Базой данных
		/// </summary>
		public static void Close(object s, EventArgs e)
		{
			Main.Cmd.Connection.Close();
			Main.Cmd.Dispose();
			Data.Cmd.Connection.Close();
			Data.Cmd.Dispose();
		}



		#region СТАТИСТИКА ЗАПРОСОВ
		public static bool IS_LOG = false;
		static int SQL_COUNT = 0;   // Общее количество SQL-запросов
		Dur dur { get; set; }       // Измерение скорости запроса
		// Подсчёт количества запросов и скорости выполнения
		void Log(string sql)
		{
			if (!IS_LOG)
				return;

			if (dur == null)
			{
				dur = new Dur();
				return;
			}
			string txt = $"SQL.{++SQL_COUNT}: {dur.Second()} {sql}";
			if (txt.Length > 500)
				txt = txt.Substring(0, 500);
			WriteLine(txt);
			G.LogWrite(txt);

			dur = null;
		}
		#endregion




		/// <summary>
		/// Внесение INSERT, удаление DELETE, обновление UPDATE данных
		/// </summary>
		public int Query(string sql)
		{
			Log(sql);
			Cmd.CommandText = sql;
			Cmd.ExecuteNonQuery();
			Log(sql);
			return Convert.ToInt32(Cmd.LastInsertedId);
		}
		/// <summary>
		/// Запрос списка с использованием делегата -> без возврата значения
		/// </summary>
		public void Delegat(string sql, VOID method)
		{
			DataReader(sql);
			while (Res.Read())
				method(Res);
			DataReaderClose(sql);
		}
		/// <summary>
		/// Запрос списка с использованием делегата -> возврат BOOLEAN
		/// </summary>
		public void Delegat(string sql, BOOL method)
		{
			DataReader(sql);
			while (Res.Read() && method(Res));
			DataReaderClose(sql);
		}
		/// <summary>
		/// Количество
		/// </summary>
		public int Count(string sql)
		{
			DataReader(sql);
			return DataReaderClose(sql, Res.Read() ? Res.GetInt32(0) : 0);
		}
		/// <summary>
		/// Идентификаторы через запятую
		/// </summary>
		public string Ids(string sql)
		{
			DataReader(sql);
			if (!Res.HasRows)
				return DataReaderClose(sql, "0");

			var list = new List<string>();
			while (Res.Read())
				list.Add(Res.GetValue(0).ToString());

			return DataReaderClose(sql, string.Join(",", list.ToArray()));
		}
		/// <summary>
		/// Проверка существования таблицы
		/// </summary>
		public bool HasTable(string table)
		{
			string sql = $"SHOW TABLES LIKE'{table}'";
			DataReader(sql);
			return DataReaderClose(sql, Res.HasRows);
		}
		/// <summary>
		/// Получение ассоциативного массива на основании id
		/// В запросе должно быть обязательно указано только два поля
		/// Например: SELECT `id`,`name` FROM `table`
		/// </summary>
		public Dictionary<int, int> IntAss(string sql)
		{
			DataReader(sql);
			var send = new Dictionary<int, int>();
			while (Res.Read())
			{
				int id = Res.GetInt32(0);
				int val = Res.GetInt32(1);
				send.Add(id, val);
			}
			return DataReaderClose(sql, send);
		}
		/// <summary>
		/// Получение ассоциативного массива на основании двух произвольных полей в базе
		/// В запросе должно быть обязательно указано только два поля
		/// Например: SELECT `name`,`value` FROM `table`
		/// </summary>
		public Dictionary<string, string> StringAss(string sql)
		{
			DataReader(sql);
			var send = new Dictionary<string, string>();
			while (Res.Read())
				send.Add(Res.GetString(0), Res.GetString(1));
			return DataReaderClose(sql, send);
		}
		/// <summary>
		/// Получение одной строки из базы
		/// </summary>
		public Dictionary<string, string> Row(string sql)
		{
			DataReader(sql);
			var send = new Dictionary<string, string>();
			if (Res.Read())
				for (int i = 0; i < Res.FieldCount; i++)
					send.Add(Res.GetName(i), Res.GetValue(i).ToString());
			return DataReaderClose(sql, send);
		}
	}
}
