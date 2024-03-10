using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Security.Cryptography;
using static System.Console;

using Newtonsoft.Json;

using MrRobot.inc;
using MrRobot.Interface;
using MrRobot.Entity;

namespace MrRobot.Connector
{
	public class BYBIT
	{
		public const int ExchangeId = 1;        // ID биржи ByBit

		public static _Instrument Instrument { get; set; }
		// Ассоциативный массив с количеством скачанных свечных данных для каждого инструмента
		static Dictionary<int, int> CCASS { get; set; }
		// Формирование ассоциативного массива
		void CCASScreate()
		{
			if (CCASS != null)
				return;

			string sql = "SELECT" +
							"`instrumentId`," +
							"COUNT(`id`)" +
						 "FROM`_candle_data_info`" +
						$"WHERE`exchangeId`={ExchangeId} " +
						 "GROUP BY`instrumentId`";
			CCASS = my.Main.IntAss(sql);
		}

		public BYBIT()
		{
			CCASScreate();
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
				unit.Symbol			= res.GetString("symbol");
				unit.BaseCoin		= res.GetString("baseCoin");
				unit.QuoteCoin		= res.GetString("quoteCoin");
				unit.HistoryBegin	= res.GetMySqlDateTime("historyBegin").ToString();
				unit.BasePrecision	= res.GetDecimal("basePrecision");
				unit.MinOrderQty	= res.GetDouble("minOrderQty");
				unit.TickSize		= res.GetDouble("tickSize");
				unit.IsTrading		= res.GetInt16("isTrading") == 1;
				unit.CdiCount		= CCASS.ContainsKey(unit.Id) ? CCASS[unit.Id] : 0;

				unit.Dbl01			= res.GetDouble("lastPrice");
				unit.Dbl05			= res.GetDouble("price24hPcnt");
				unit.Dbl05str		= $"{(unit.Dbl05 > 0 ? "+" : "")}{unit.Dbl05}%";
				unit.Lng01			= res.GetInt64("turnover24h");
				unit.Lng01str		= format.Num(unit.Lng01);

				unit.Str01 = "≈-.--$";
				if (unit.QuoteCoin == "USDT")
					unit.Str01 = $"≈{format.Price(unit.MinOrderQty * unit.Dbl01, 2)}$";

				return unit;
			}

			/// <summary>
			/// Обновление количества свечных данных инструмента
			/// </summary>
			public void CdiCountUpd(int id) => Unit(id).CdiCount = Candle.CdiCount(id);
		}




		#region API

		static string API_URL = "https://api.bybit.com";
		public static string ApiKey
		{
			get => position.Val("5_ApiKey_Text");
			set => position.Set("5_ApiKey_Text", value);
		}
		public static string ApiSecret
		{
			get => position.Val("5_ApiSecret_Text");
			set => position.Set("5_ApiSecret_Text", value);
		}
		public static void ApiKeyChanged(object s, TextChangedEventArgs e) =>
			ApiKey = (s as TextBox).Text;
		public static void ApiSecretChanged(object s, RoutedEventArgs e) =>
			ApiSecret = (s as PasswordBox).Password;


		// Защищённые запросы к бирже
		public static dynamic Api(string query)
		{
			string API_KEY = ApiKey;
			string API_SECRET = ApiSecret;
			long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			long recvWindow = 5000;

			string signature = "";
			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(API_SECRET)))
			{
				string msg = $"{timestamp}{API_KEY}{recvWindow}";
				byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(msg));
				signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
			}

			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("X-BAPI-API-KEY", API_KEY);
			client.DefaultRequestHeaders.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
			client.DefaultRequestHeaders.Add("X-BAPI-RECV-WINDOW", recvWindow.ToString());
			client.DefaultRequestHeaders.Add("X-BAPI-SIGN", signature);

			var res = client.GetAsync(API_URL + query).Result;
			string content = res.Content.ReadAsStringAsync().Result;

			if (content.Length == 0)
				return "пусто..";

			dynamic json = JsonConvert.DeserializeObject(content);

			return json;
		}




		/*
		"symbol":"DOTUSDT",
		"contractType":"LinearPerpetual",
		"status":"Trading",
		"baseCoin":"DOT",
		"quoteCoin":"USDT",
		"launchTime":"1616060040000",
		"deliveryTime":"0",
		"deliveryFeeRate":"",
		"priceScale":"3",
		"leverageFilter":
			"minLeverage":"1",
			"maxLeverage":"50.00",
			"leverageStep":"0.01"
		"priceFilter":{
			"minPrice":"0.001",
			"maxPrice":"1999.998",
			"tickSize":"0.001"},
		"lotSizeFilter":{
			"maxOrderQty":"91420.0",
			"minOrderQty":"0.1",
			"qtyStep":"0.1",
			"postOnlyMaxOrderQty":"91420.0",
			"maxMktOrderQty":"32900.0"},
		"unifiedMarginTrade":true,
		"fundingInterval":480,
		"settleCoin":"USDT",
		"copyTrading":"both",
		"upperFundingRate":"0.0075",
		"lowerFundingRate":"-0.0075"
		 */



		// Последние цены и объёмы за 24 часа
		public static void Tickers()
		{
/*
	"symbol":"VEGAUSDT",
	"bid1Price":"0.921",
	"bid1Size":"15.58",
	"ask1Price":"0.9232",
	"ask1Size":"67.48",
	"lastPrice":"0.9232",
	"prevPrice24h":"0.964",
	"price24hPcnt":"-0.0423",
	"highPrice24h":"0.9795",
	"lowPrice24h":"0.8869",
	"turnover24h":"120369.08654",
	"volume24h":"128651.67"
*/
			var dur = new Dur();
			string url = $"{API_URL}/v5/market/tickers?category=spot";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("spot"))
				return;

			dynamic json = JsonConvert.DeserializeObject(str);
			dynamic list = json.result.list;

			WriteLine($"{url}   {list.Count}   {dur.Second()}");

			var insert = new List<string>();
			for (int k = 0; k < list.Count; k++)
			{
				var item = list[k];
				int id = Instrument.FieldToId("Symbol", item.symbol.ToString());
				if (id > 0)
				{
					decimal price24 = Convert.ToDecimal(item.price24hPcnt) * 100;
					insert.Add($"({id}," +
							   $"{item.lastPrice}," +
							   $"{price24}," +
							   $"{item.turnover24h})");
				}
			}
			string sql = "INSERT INTO `_instrument`" +
							"(`id`,`lastPrice`,`price24hPcnt`,`turnover24h`)" +
						$"VALUES{string.Join(",", insert.ToArray())}" +
						 "ON DUPLICATE KEY UPDATE" +
							"`lastPrice`=VALUES(`lastPrice`)," +
							"`price24hPcnt`=VALUES(`price24hPcnt`)," +
							"`turnover24h`=VALUES(`turnover24h`)";
			my.Main.Query(sql);

			WriteLine($"Updated: {insert.Count}");
		}

		// Получение свечных данных по указанному инструменту
		public static dynamic Kline(string symbol, int interval, int start) =>
			Kline(symbol, interval.ToString(), start.ToString());
		public static dynamic Kline(string symbol, string interval, string start)
		{
			var dur = new Dur();
			string url = $"{API_URL}/v5/market/kline" +
										$"?category=spot" +
										$"&symbol={symbol}" +
										$"&interval={interval}" +
										$"&start={start}000" +
										 "&limit=1000";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("spot"))
				return null;

			dynamic json = JsonConvert.DeserializeObject(str);
			dynamic list = json.result.list;
			WriteLine($"{url}   {list.Count}   {dur.Second()}");

			return list;
		}



		#endregion
	}
}



/*
 "retCode": 0,
  "retMsg": "",
  "result": {
	"id": "28844402",
	"note": "StockMarket",
	"apiKey": "ECrLHflpIkeCnuo2oZ",
	"readOnly": 0,
	"secret": "",
	"permissions": {
	  "ContractTrade": [
		"Order",
		"Position"
	  ],
	  "Spot": [
		"SpotTrade"
	  ],
	  "Wallet": [
		"AccountTransfer",
		"SubMemberTransfer"
	  ],
	  "Options": [
		"OptionsTrade"
	  ],
	  "Derivatives": [],
	  "CopyTrading": [],
	  "BlockTrade": [],
	  "Exchange": [
		"ExchangeHistory"
	  ],
	  "NFT": [],
	  "Affiliate": []
	},
	"ips": [
	  "*"
	],
	"type": 1,
	"deadlineDay": 27,
	"expiredAt": "2024-03-02T22:51:42Z",
	"createdAt": "2023-12-02T22:51:42Z",
	"unified": 0,
	"uta": 0,
	"userID": 7362350,
	"inviterID": 0,
	"vipLevel": "No VIP",
	"mktMakerLevel": "0",
	"affiliateID": 0,
	"rsaPublicKey": "",
	"isMaster": true,
	"parentUid": "0",
	"kycLevel": "LEVEL_1",
	"kycRegion": "RUS"
  },
  "retExtInfo": {},
  "time": 1707039874333
 */
