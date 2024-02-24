using System;
using System.Collections.Generic;
using static System.Console;

namespace MrRobot.inc
{
	public class position
	{
		// Значения всех позиций
		static Dictionary<string, string> ASS;

        /// <summary>
        /// Получение всех значений позиций
        /// </summary>
		public position()
		{
            string sql = "SELECT `key`,`val` FROM `_position`";
            ASS = mysql.StringAss(sql);
        }

		/// <summary>
		/// Внесение ключа и значения
		/// </summary>
		static bool Insert(string key, string val)
		{
			if (ASS.ContainsKey(key))
				return false;

			string sql = "INSERT INTO `_position`" +
						 "(`key`,`val`)" +
						 "VALUES" +
						$"('{key}','{val}')";
			my.Main.Query(sql);

			ASS.Add(key, val);

			return true;
		}


		/// <summary>
		/// Обновление значения по ключу
		/// </summary>
		public static void Set(string key, string val)
		{
			if (Insert(key, val))
				return;
			if (ASS[key] == val)
				return;

			string sql = $"UPDATE`_position`SET`val`='{val}'WHERE`key`='{key}'";
            if (!G.IsAutoProgon)
                my.Main.Query(sql);

			ASS[key] = val;
		}
		public static void Set(string key, int val)  => Set(key, val.ToString());
		public static void Set(string key, bool val) => Set(key, val ? "1" : "0");


		/// <summary>
		/// Получение значения по ключу
		/// </summary>
		public static string Val(string key, string val="")
		{
			Insert(key, val);
			return ASS[key];
		}
		public static int Val(string key, int val)
		{
			string v = Val(key, val.ToString());
			return Convert.ToInt32(v);
		}
		public static bool Val(string key, bool val)
		{
			string v = val ? "1" : "0";
			return Val(key, v) == "1";
		}

		/// <summary>
		/// Cохранение позиции главного меню
		/// </summary>
		public static int MainMenu(int val = 0)
		{
			string key = "MainMenu";

			if (val > 0)
				Set(key, val);

			if (val == 0)
				val = 1;

			return Val(key, val);
		}
	}
}
