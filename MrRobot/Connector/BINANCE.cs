using System;
using System.Net;
using static System.Console;

using Newtonsoft.Json;

using RobotLib;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Interface;
using System.Web.Hosting;
using System.Windows.Documents;

namespace MrRobot.Connector
{
	public class BINANCE
	{
		public const int ExchangeId = 3;        // ID биржи Binance

		public static _Instrument Instrument { get; set; }

		public BINANCE()
		{
			Instrument = new _Instrument();
		}

		public class _Instrument : Spisok
		{
			public override string SQL =>
				"SELECT*" +
				"FROM`_instrument`" +
			   $"WHERE`exchangeId`={ExchangeId} " +
				"ORDER BY`quoteCoin`,`baseCoin`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.Str01 = res.GetString("symbol");
				unit.Str02 = res.GetString("baseCoin");
				unit.Str03 = res.GetString("quoteCoin");
				unit.Str04 = $"{unit.Str02}/{unit.Str03}";

				unit.Dec01 = res.GetDecimal("tickSize");
				unit.Dec02 = res.GetDecimal("qtyMin");
				unit.Dec03 = res.GetDecimal("qtyStep");
				
				unit.Lng01 = res.GetInt64("historyBeginUnix");


				return unit;
			}
		}




		static string API_URL = "https://api.binance.com";


		// Информация об инструментах SPOT
		public static dynamic ExchangeInfo()
		{
			var dur = new Dur();
			string url = $"{API_URL}/api/v3/exchangeInfo?permissions=SPOT";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("symbols"))
				return 0;

			dynamic json = JsonConvert.DeserializeObject(str);

			WriteLine($"{url}   {dur.Second()}");

			return json.symbols;
		}

		// Дата начала истории конкретного инструмента
		public static long HistoryBegin(string symbol)
		{
			var dur = new Dur();
			string url = $"{API_URL}/api/v3/aggTrades?symbol={symbol}&limit=1&fromId=0";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("T"))
				return 0;

			dynamic json = JsonConvert.DeserializeObject(str);
			string unix = json[0].T;
			unix = unix.Substring(0, 10);

			WriteLine($"{url}	{dur.Second()}	{unix}");

			return Convert.ToInt64(unix);
		}

		// Тиковые данные
		public static dynamic Trades(string symbol, long startTime, int limit = 1000)
		{
			var dur = new Dur();
			string url = $"{API_URL}/api/v3/aggTrades?" +
									$"symbol={symbol}" +
								   $"&limit={limit}" +
								   $"&startTime={startTime}";
			string str = new WebClient().DownloadString(url);

			WriteLine($"{url}	{dur.Second()}");

			return JsonConvert.DeserializeObject(str);
		}
	}
}
