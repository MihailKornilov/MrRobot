using System;
using System.Net;
using System.Text;
using System.Linq;
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
using System.Runtime.CompilerServices;

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
			new Param();
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
				unit.Symbol = res.GetString("symbol");
				unit.BaseCoin = res.GetString("baseCoin");
				unit.QuoteCoin = res.GetString("quoteCoin");
				unit.HistoryBegin = res.GetMySqlDateTime("historyBegin").ToString();
				unit.BasePrecision = res.GetDecimal("basePrecision");
				unit.MinOrderQty = res.GetDouble("minOrderQty");
				unit.TickSize = res.GetDouble("tickSize");
				unit.IsTrading = res.GetInt16("isTrading") == 1;
				unit.CdiCount = CCASS.ContainsKey(unit.Id) ? CCASS[unit.Id] : 0;

				unit.Dbl01 = res.GetDouble("lastPrice");
				unit.Dbl05 = res.GetDouble("price24hPcnt");
				unit.Dbl05str = $"{(unit.Dbl05 > 0 ? "+" : "")}{unit.Dbl05}%";
				unit.Str02 = unit.Dbl05 >= 0 ? "#20B26C" : "#EF454A";
				unit.Lng01 = res.GetInt64("turnover24h");
				unit.Lng01str = format.Num(unit.Lng01);

				unit.Str01 = "≈-.--$";
				if (unit.QuoteCoin == "USDT")
					unit.Str01 = $"≈{format.Price(unit.MinOrderQty * unit.Dbl01, 2)}$";

				unit.DTime01 = res.GetDateTime("historyBegin");

				return unit;
			}

			/// <summary>
			/// Обновление количества свечных данных инструмента
			/// </summary>
			public void CdiCountUpd(int id) => Unit(id).CdiCount = Candle.CdiCount(id);
		}





		static string API_URL = "https://api.bybit.com";
		public static string API_KEY
		{
			get => position.Val("5_ApiKey_Text");
			set => position.Set("5_ApiKey_Text", value);
		}
		public static string API_SECRET
		{
			get => position.Val("5_ApiSecret_Text");
			set => position.Set("5_ApiSecret_Text", value);
		}
		static string RECV_WINDOW => "20000";

		class Param
		{
			public Param()
			{
				IsPost = false;
				prm = new Dictionary<string, object>();
				ts = 0;
			}
			public static bool IsPost { get; set; }
			static Dictionary<string, object> prm { get; set; }
			static long ts { get; set; }
			public static string Timestamp =>
				(ts == 0 ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : ts).ToString();
			public static void Add(string key, object val) => prm.Add(key, val);
			public static string Query =>
				string.Join("&", prm.Select(p => $"{p.Key}={p.Value}"));
			public static string Sign
			{
				get
				{
					var msg = $"{Timestamp}{API_KEY}{RECV_WINDOW}{(IsPost ? Json : Query)}";
					var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(API_SECRET));
					byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(msg));
					return BitConverter.ToString(hash).Replace("-", "").ToLower();
				}
			}
			public static string Json =>
				JsonConvert.SerializeObject(prm);
		}

		// Отправка сообщения об ошибке защищённого запроса
		static dynamic PrivError(string url, string content, Dur dur)
		{
			var msg = $"PRIVATE API ERROR: {url}\n{content}\n{dur.Second()}";
			WriteLine(msg);
			G.LogWrite(msg);
			return "";
		}
		// Защищённые запросы к бирже
		static dynamic PRIVATE_GET(string endpoint)
		{
			var dur = new Dur();

			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("X-BAPI-API-KEY", API_KEY);
			client.DefaultRequestHeaders.Add("X-BAPI-TIMESTAMP", Param.Timestamp);
			client.DefaultRequestHeaders.Add("X-BAPI-RECV-WINDOW", RECV_WINDOW);
			client.DefaultRequestHeaders.Add("X-BAPI-SIGN", Param.Sign);

			var url = $"{API_URL}{endpoint}?{Param.Query}";
			var res = client.GetAsync(url).Result;
			new Param();

			if (res.ReasonPhrase != "OK")
				return PrivError(url, res.ToString(), dur);

			string content = res.Content.ReadAsStringAsync().Result;
			if (!content.Contains("retCode"))
				return PrivError(url, content, dur);

			dynamic json = JsonConvert.DeserializeObject(content);
			if (json.retCode > 0)
				return PrivError(url, content, dur);

			WriteLine($"BYBIT Private Api: {dur.Second()}");
			
			return json;
		}

		static dynamic PRIVATE_POST(string endpoint)
		{
			var dur = new Dur();

			Param.IsPost = true;
			var client = new HttpClient();
			var url = $"{API_URL}{endpoint}";
			var req = new HttpRequestMessage
			{
				Method = HttpMethod.Post,
				RequestUri = new Uri(url),
				Content = new StringContent(Param.Json, Encoding.UTF8, "application/json")
			};

			WriteLine(Param.Json);

			req.Headers.Add("X-BAPI-API-KEY", API_KEY);
			req.Headers.Add("X-BAPI-TIMESTAMP", Param.Timestamp);
			req.Headers.Add("X-BAPI-RECV-WINDOW", RECV_WINDOW);
			req.Headers.Add("X-BAPI-SIGN", Param.Sign);
			new Param();

			var res = client.SendAsync(req).Result;
			if (res.ReasonPhrase != "OK")
				return PrivError(url, res.ToString(), dur);

			var content = res.Content.ReadAsStringAsync().Result;
			if (!content.Contains("retCode"))
				return PrivError(url, content, dur);

			dynamic json = JsonConvert.DeserializeObject(content);
			if (json.retCode > 0)
				return PrivError(url, content, dur);

			WriteLine($"BYBIT Private Api: {dur.Second()}");
			
			return json;
		}

		// Балансы монет на счёте финансирования
		public static List<CoinSumUnit> FundBalance()
		{
			Param.Add("accountType", "FUND");

			var wallet = new List<CoinSumUnit>();
			var res = PRIVATE_GET("/v5/asset/transfer/query-account-coins-balance");
			if (res.Length == 0)
				return wallet;

			var list = res.result.balance;
			foreach (var v in list)
				wallet.Add(new CoinSumUnit(v));

			return wallet.OrderBy(x => x.Coin).ToList();
		}

		// Балансы монет на едином торговом счёте
		public static List<CoinSumUnit> UnifiedBalance()
		{
			Param.Add("accountType", "UNIFIED");

			var wallet = new List<CoinSumUnit>();
			var res = PRIVATE_GET("/v5/account/wallet-balance");
			if (res.Length == 0)
				return wallet;

			var list = res.result.list[0];
			foreach (var v in list.coin)
				wallet.Add(new CoinSumUnit(v));

			return wallet.OrderBy(x => x.Coin).ToList();
		}

		// Размещение лимитного ордера на покупку
		public static void BuyLimit(string symbol, decimal price, decimal qty)
		{
			Param.Add("category",	"spot");
			Param.Add("symbol",		symbol);
			Param.Add("side",		"Buy");
			Param.Add("orderType",	"Limit");
			Param.Add("price",	   $"{price}");
			Param.Add("qty",	   $"{qty}");

			var res = PRIVATE_POST("/v5/order/create");

			WriteLine(res);
		}





		#region API: открытые запросы

		// Информация об инструменте Linear
		public static dynamic LinearInfo(string symbol)
		{
/*
	"retCode":0,
	"retMsg":"OK",
	"result":{
		"category":"linear",
		"list":[
			{"symbol":"BTCUSDT",
			 "contractType":"LinearPerpetual",
			 "status":"Trading",
			 "baseCoin":"BTC",
			 "quoteCoin":"USDT",
			 "launchTime":"1584230400000",
			 "deliveryTime":"0",
			 "deliveryFeeRate":"",
			 "priceScale":"2",
			 "leverageFilter":{
				"minLeverage":"1",
				"maxLeverage":"100.00",
				"leverageStep":"0.01"},
			"priceFilter":{
				"minPrice":"0.10",
				"maxPrice":"199999.80",
				"tickSize":"0.10"
			},
			"lotSizeFilter":{
				"maxOrderQty":"1190.000",
				"minOrderQty":"0.001",
				"qtyStep":"0.001",
				"postOnlyMaxOrderQty":"1190.000",
				"maxMktOrderQty":"119.000"
			},
			"unifiedMarginTrade":true,
			"fundingInterval":480,
			"settleCoin":"USDT",
			"copyTrading":"both",
			"upperFundingRate":"0.00375",
			"lowerFundingRate":"-0.00375"
			}
		],
		"nextPageCursor":""
	},
	"retExtInfo":{},
	"time":1711556411709}
*/
			var dur = new Dur();
			string url = $"{API_URL}/v5/market/instruments-info?category=linear&symbol={symbol.ToUpper()}";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("linear"))
				return 0;

			dynamic json = JsonConvert.DeserializeObject(str);

			WriteLine($"{url}   {dur.Second()}");

			return json.result.list[0];
		}

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

	// Баланс монеты
	public class CoinSumUnit
	{
		public CoinSumUnit(dynamic v)
		{
			Coin = v.coin;
			Sum = v.walletBalance;
		}
		public string Coin { get; set; }
		public string CoinStr => $"{Coin}:";
		public decimal Sum { get; set; }
		//public decimal Usdt { get; set; }		// Перерасчёт монеты в USDT
	}
}
