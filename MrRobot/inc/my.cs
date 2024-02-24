using System;
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

	}
}
