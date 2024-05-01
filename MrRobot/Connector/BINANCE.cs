using System.Net;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;

namespace MrRobot.Connector
{
	public class BINANCE
	{
		public const int ExchangeId = 3;        // ID биржи Binance

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

	}
}
