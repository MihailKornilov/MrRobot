using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using static System.Console;

namespace MrRobot.inc
{
	public class format
	{
		public static int Offset = (int)DateTimeOffset.Now.Offset.TotalSeconds;

		/// <summary>
		/// Получение начала дня в формате UNIX
		/// </summary>
		/// 
		/// Входящий вид `date`: 01.03.2023
		public static int UnixFromDay(string date)
		{
			int year = Convert.ToInt32(date.Substring(6, 4));
			int mon  = Convert.ToInt32(date.Substring(3, 2));
			int day  = Convert.ToInt32(date.Substring(0, 2));

			DateTimeOffset dt = new DateTime(year, mon, day);
			long unix = dt.ToUnixTimeSeconds();

			return Convert.ToInt32(unix + Offset);
		}
		/// <summary>
		/// Получение даты в формате UNIX
		/// </summary>
		/// 
		/// Входящий вид `date`: 01.03.2023 11:20:00
		///                      2023-03.01 11:20:00
		public static int UnixFromDate(string dt)
		{
			DateTimeOffset dtime = Convert.ToDateTime(dt);
			return (int)dtime.ToUnixTimeSeconds();
		}
		/// <summary>
		/// Получение даты и времени в формате 12.05.2022 12:44
		/// </summary>
		///
		/// Входящий вид `date`: 12.05.2022 12:44:44
		public static string DateOne(string date) =>
			date.Substring(0, 16);


		/// <summary>
		/// Текущее время в виде 12.05.2022 12:45:34
		/// </summary>
		public static string DTimeNow() => DateTime.Now.ToString();
		/// <summary>
		/// Получение даты и времени в формате `12.05.2022 12:44:00` по местному времени
		/// </summary>
		public static string DTimeFromUnix(int unix)
		{
			var dt = new DateTime(1970, 1, 1);
			dt = dt.AddSeconds(unix).ToLocalTime();
			return dt.ToString();
		}
		/// <summary>
		/// Получение даты в формате 12.05.2022
		/// </summary>
		public static string DayFromUnix(int unix)
		{
			return DTimeFromUnix(unix).Substring(0, 10);
		}
		/// <summary>
		/// Получение времени в формате 12:49:34
		/// </summary>
		public static string TimeFromUnix(int unix)
		{
			return DTimeFromUnix(unix).Substring(11, 8);
		}


		/// <summary>
		/// Текущее время в виде 12:45:34
		/// </summary>
		public static string TimeNow()
		{
			return DateTime.Now.ToString().Substring(11, 8);
		}

		/// <summary>
		/// Текущее UTC время в формате Unix в секундах
		/// </summary>
		public static int UnixNow()
		{
			return (int)DateTimeOffset.Now.ToUnixTimeSeconds();
		}
		/// <summary>
		/// Текущее МЕСТНОЕ время в формате Unix в миллисекундах
		/// </summary>
		public static long UnixNow_MilliSec()
		{
			return DateTimeOffset.Now.ToUnixTimeMilliseconds();
		}
		/// <summary>
		/// Текущее время в миллисекундах (последние три цифры)
		/// </summary>
		public static int MilliSec()
		{
			return DateTime.Now.Millisecond;
		}

		/// <summary>
		/// Смещение временной зоны для правильного отображения времени на графиках
		/// </summary>
		public static int TimeZone(int unix)
		{
			return unix + Offset;
		}




		/// <summary>
		/// Форматирование цен, полученных из базы
		/// </summary>
		public static string Num(long num)
		{
			var nfi = new NumberFormatInfo { NumberGroupSeparator = " " };
			return num.ToString("N0", nfi);
		}


		public static string Coin(double num)
		{
			var nfi = new NumberFormatInfo();
			nfi.NumberGroupSeparator = " ";
			nfi.NumberDecimalSeparator = ".";
			return num.ToString("N8", nfi);
		}

		public static string Price(object obj, int precision)
		{
			double val = Convert.ToDouble(obj);
			return Price(val, precision);
		}
		public static string Price(double val, int precision)
		{
			var nfi = new NumberFormatInfo
			{
				NumberGroupSeparator = ",",
				NumberDecimalSeparator = "."
			};
			return val.ToString("N" + precision, nfi);
		}


		/// <summary>
		/// Ассоциативный массив таймфреймов
		/// </summary>
		public static Dictionary<int, string> TFass()
		{
			var ass = new Dictionary<int, string>();

			ass.Add(1, "1m");
			ass.Add(2, "2m");
			ass.Add(3, "3m");
			ass.Add(4, "4m");
			ass.Add(5, "5m");
			ass.Add(6, "6m");
			ass.Add(7, "7m");
			ass.Add(8, "8m");
			ass.Add(9, "9m");
			ass.Add(10, "10m");
			ass.Add(11, "11m");
			ass.Add(12, "12m");
			ass.Add(13, "13m");
			ass.Add(14, "14m");
			ass.Add(15, "15m");
			ass.Add(16, "16m");
			ass.Add(17, "17m");
			ass.Add(18, "18m");
			ass.Add(19, "19m");
			ass.Add(20, "20m");
			ass.Add(21, "21m");
			ass.Add(22, "22m");
			ass.Add(23, "23m");
			ass.Add(24, "24m");
			ass.Add(25, "25m");
			ass.Add(26, "26m");
			ass.Add(27, "27m");
			ass.Add(28, "28m");
			ass.Add(29, "29m");
			ass.Add(30, "30m");
			ass.Add(60, "1h");
			ass.Add(120, "2h");
			ass.Add(180, "3h");
			ass.Add(240, "4h");
			ass.Add(300, "5h");
			ass.Add(360, "6h");

			ass.Add(1440,  "1D");
			ass.Add(10080, "1W");
			ass.Add(43200, "1M");

			return ass;
		}
		/// <summary>
		/// Перевод таймфрейма из минут в текст
		/// </summary>
		public static string TF(int key)
		{
			var ass = TFass();

			if (!ass.ContainsKey(key))
				return "-";

			return ass[key];
		}
		/// <summary>
		/// Перевод таймфрейма из текстовых минут в числовые
		/// </summary>
		public static int TimeFrame(string key)
		{
			var source = TFass();
			var ass = new Dictionary<string, int>();

			foreach (var src in source)
				ass.Add(src.Value, src.Key);

			if (!ass.ContainsKey(key))
				return 0;

			return ass[key];
		}


		/// <summary>
		/// Откидывание лишних нулей справа в дробных числах
		/// </summary>
		public static string NolDrop(string num)
		{
			int i;
			for(i = num.Length-1; i >= 0; i--)
				if (num[i] != '0' && num[i] != '.')
					break;

			return num.Substring(0, i+1);
		}

		/// <summary>
		/// Количество нулей после запятой
		/// </summary>
		public static int Decimals(string num)
		{
			num = NolDrop(num);
			string[] split = num.Split('.');

			if(split.Length < 2)
				return 0;

			return split[1].Length;
		}
		public static int Decimals(double num)
		{
			string[] split = E(num).Split('.');

			if(split.Length < 2)
				return 0;

			return split[1].Length;
		}


		public static double TickSize(int decimals) =>
			1.0 / Exp(decimals);


		/// <summary>
		/// Избавление от E в маленьких числах
		/// </summary>
		public static string E(dynamic num)
		{
			return E(Convert.ToDouble(num));
		}
		public static string E(decimal num)
		{
			return E(Convert.ToDouble(num));
		}
		public static string E(string num)
		{
			return E(Convert.ToDouble(num));
		}
		public static string E(double num)
		{
			string numS = num.ToString();
			if (numS.IndexOf('E') == -1)
				return numS;

			return NolDrop(num.ToString("N12"));
		}

		/// <summary>
		/// Возведение в степень числа 10
		/// </summary>
		public static ulong Exp(int v)
		{
			double pow = Math.Pow(10, v);
			return Convert.ToUInt64(pow);
		}

		/// <summary>
		/// Количество знаков после запятой для округления
		/// </summary>
		public static int Round(double v)
		{
			double d = 100 / v % 1;
			if(d == 0)
				return 0;

			int c = d.ToString().Length - 2;
			
			return c == 1 ? 1 : 2;
		}

		/// <summary>
		/// Формирование окончаний в словах
		/// </summary>
		public static string End(int count, string o1, string o2, string o5=null)
		{
			o5 = o5 == null ? o2 : o5;

			//Цифры 11-19
			if (count / 10 % 10 == 1)
				return o5;

			switch(count % 10)
			{
				case 1: return o1;
				case 2:
				case 3:
				case 4: return o2;
			}

			return o5;
		}



		/// <summary>
		/// Цвет для элементов WPF
		/// </summary>
		public static SolidColorBrush RGB(string str)
		{
			if (str.Contains("#"))
				str = str.Substring(1, 6);

			byte R = Convert.ToByte(str.Substring(0, 2), 16);
			byte G = Convert.ToByte(str.Substring(2, 2), 16);
			byte B = Convert.ToByte(str.Substring(4, 2), 16);

			return new SolidColorBrush(Color.FromArgb(255, R, G, B));
		}
	}
}
