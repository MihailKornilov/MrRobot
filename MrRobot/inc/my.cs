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

		public my()
		{
			Main = new my("mrrobot");
			Data = new my("candles");
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
		MySqlDataReader Res(string sql)
		{
			Cmd.CommandText = sql;
			return Cmd.ExecuteReader();
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




		/// <summary>
		/// Внесение INSERT, удаление DELETE, обновление UPDATE данных
		/// </summary>
		public int Query(string sql)
		{
			Cmd.CommandText = sql;
			Cmd.ExecuteNonQuery();
			return Convert.ToInt32(Cmd.LastInsertedId);
		}
		/// <summary>
		/// Запрос списка с использованием делегата -> без возврата значения
		/// </summary>
		public void Delegat(string sql, VOID method)
		{
			var res = Res(sql);
			while (res.Read())
				method(res);
			res.Close();
		}
		/// <summary>
		/// Запрос списка с использованием делегата -> возврат BOOLEAN
		/// </summary>
		public void Delegat(string sql, BOOL method)
		{
			var res = Res(sql);
			while (res.Read() && method(res));
			res.Close();
		}
		/// <summary>
		/// Количество
		/// </summary>
		public int Count(string sql)
		{
			var res = Res(sql);
			if (!res.Read())
				return 0;
			
			int count = res.GetInt32(0);
			res.Close();

			return count;
		}
		/// <summary>
		/// Идентификаторы через запятую
		/// </summary>
		public string Ids(string sql)
		{
			var res = Res(sql);
			if (!res.HasRows)
			{
				res.Close();
				return "0";
			}

			var list = new List<string>();
			while (res.Read())
				list.Add(res.GetValue(0).ToString());

			res.Close();

			return string.Join(",", list.ToArray());
		}
		/// <summary>
		/// Получение ассоциативного массива на основании id
		/// В запросе должно быть обязательно указано только два поля
		/// Например: SELECT `id`,`name` FROM `table`
		/// </summary>
		public Dictionary<int, int> IntAss(string sql)
		{
			var send = new Dictionary<int, int>();
			var res = Res(sql);
			while (res.Read())
			{
				int id = res.GetInt32(0);
				int val = res.GetInt32(1);
				send.Add(id, val);
			}
			res.Close();
			return send;
		}
		/// <summary>
		/// Получение ассоциативного массива на основании двух произвольных полей в базе
		/// В запросе должно быть обязательно указано только два поля
		/// Например: SELECT `name`,`value` FROM `table`
		/// </summary>
		public Dictionary<string, string> StringAss(string sql)
		{
			var send = new Dictionary<string, string>();
			var res = Res(sql);
			while (res.Read())
				send.Add(res.GetString(0), res.GetString(1));
			res.Close();
			return send;
		}
		/// <summary>
		/// Проверка существования строк
		/// </summary>
		public bool HasRows(string table)
		{
			string sql = $"SHOW TABLES LIKE'{table}'";
			var res = Res(sql);
			bool HasRows = res.HasRows;
			res.Close();
			return HasRows;
		}
		/// <summary>
		/// Получение одной строки из базы
		/// </summary>
		public Dictionary<string, string> Row(string sql)
		{
			var send = new Dictionary<string, string>();
			var res = Res(sql);
			if (res.Read())
				for (int i = 0; i < res.FieldCount; i++)
					send.Add(res.GetName(i), res.GetValue(i).ToString());
			res.Close();
			return send;
		}

	}
}
