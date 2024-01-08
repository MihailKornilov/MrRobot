using System;
using System.Collections.Generic;

namespace MrRobot.inc
{
	public class position
	{
		// Значения всех позиций
		private static Dictionary<string, string> AllPos;

		/// <summary>
		/// Получение всех значений позиций
		/// </summary>
		private static void All()
		{
			if(AllPos == null)
				AllPos = new Dictionary<string, string>();
			if (AllPos.Count > 0)
				return;

			string sql = "SELECT `key`,`val` FROM `_position`";
			AllPos = mysql.StringAss(sql);
		}


		/// <summary>
		/// Внесение ключа и значения
		/// </summary>
		private static bool Insert(string key, string val)
		{
			All();
			if (AllPos.ContainsKey(key))
				return false;

			string sql = "INSERT INTO `_position`" +
						 "(`key`,`val`)" +
						 "VALUES" +
						$"('{key}','{val}')";
			mysql.Query(sql);

			AllPos.Add(key, val);

			return true;
		}


		/// <summary>
		/// Обновление значения по ключу
		/// </summary>
		public static void Set(string key, string val)
		{
			if (!global.IsInited())
				return;
			if (Insert(key, val))
				return;
			if (AllPos[key] == val)
				return;

			string sql = $"UPDATE`_position`SET`val`='{val}'WHERE`key`='{key}'";
			mysql.Query(sql);

			AllPos[key] = val;
		}
		public static void Set(string key, int val)
		{
			Set(key, val.ToString());
		}
		public static void Set(string key, bool val)
		{
			Set(key, val ? "1" : "0");
		}


		/// <summary>
		/// Получение значения по ключу
		/// </summary>
		public static string Val(string key, string val="")
		{
			Insert(key, val);

			return AllPos[key];
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

			return Val(key, val);
		}
	}
}
