using System;
using static System.Console;

using MySqlConnector;
using System.Collections.Generic;

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

		public delegate void DLGT(MySqlDataReader rs);

		MySqlCommand Cmd { get; set; }

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

		MySqlDataReader Res(string sql)
		{
			Cmd.CommandText = sql;
			return Cmd.ExecuteReader();
		}

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
		/// Запрос списка с использованием делегата
		/// </summary>
		public void Delegat(string sql, DLGT method)
		{
			var res = Res(sql);
			while (res.Read())
				method(res);
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
				return "0";

			var list = new List<string>();
			while (res.Read())
				list.Add(res.GetValue(0).ToString());

			string ids = string.Join(",", list.ToArray());
			res.Close();

			return ids;
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
		public bool HasRows(string tableName)
		{
			string sql = $"SHOW TABLES LIKE'{tableName}'";
			var res = Res(sql);
			bool HasRows = res.HasRows;
			res.Close();
			return HasRows;
		}
	}
}
