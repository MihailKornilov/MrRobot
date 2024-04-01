using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;

namespace MrRobot.Section
{
	/// <summary>
	/// Запись стакана и сделок в базу
	/// </summary>
	public partial class WssVisual : Window
	{
		#region GLOBAL
		public WssVisual()
		{
			G.WssVisual = this;
			InitializeComponent();

			SymbolTB.Text = SymbolPos;
			CheckOB50.IsChecked = CheckOB50Pos;
			CheckTrade.IsChecked = CheckTradePos;

			new DepthScroll(DepthPanel.Parent);
			G.ButtonClick(WssOpenBut);

			// Отписка перед закрытием
			Closed += (s, e) =>
			{
				if (SubscrBut.Content.ToString() != "Подписка")
					G.ButtonClick(SubscrBut);
			};
		}
		string SymbolPos
		{
			get => position.Val("5.SymbolPos");
			set => position.Set("5.SymbolPos", value);
		}
		bool CheckOB50Pos
		{
			get => position.Val("5.CheckOB50", false);
			set => position.Set("5.CheckOB50", value);
		}
		bool CheckTradePos
		{
			get => position.Val("5.CheckTrade", false);
			set => position.Set("5.CheckTrade", value);
		}
		string OrderbookTopic => $"orderbook.50.{SymbolPos}";
		string TradeTopic => $"publicTrade.{SymbolPos}";
		#endregion

		#region WSS
		WSS wss;
		async void WssOpen(object sender, RoutedEventArgs e)
		{
			if (wss != null)
				return;

			wss = new WSS();
			wss.DataNew += DepthUpdate;

			WssOpenBut.IsEnabled = false;
			WssOpenBut.Content = "WSS connecting";

			var prgs = new Progress<int>(v => WssOpenBut.Content += ".");
			await Task.Run(() => WssConnecting(prgs));

			WssOpenBut.IsEnabled = true;
			WssOpenBut.Content = "WSS connected";
			WssOpenBut.Style = Application.Current.Resources["ButtonGreen"] as Style;
		}
		// Процесс подключения к WebSocket
		void WssConnecting(IProgress<int> prgs)
		{
			while (wss.State != WebSocketState.Open)
			{
				prgs.Report(0);
				Task.Delay(200).Wait();
			}
		}
		// Подписка на поток
		void Subscr(object s, RoutedEventArgs e)
		{
			if (wss == null) return;

			SymbolPos = SymbolTB.Text;

			var topic = new List<string>();
			if (CheckOB50Pos = (bool)CheckOB50.IsChecked)
				topic.Add(OrderbookTopic);
			if (CheckTradePos = (bool)CheckTrade.IsChecked)
				topic.Add(TradeTopic);

			if (SubscrBut.Content.ToString() == "Подписка")
			{
				OBunit.StaticClear();
				new OBTdb(SymbolPos);
				OBTdb.Info += c => RowsInserted.Content = format.Num(c);
				DepthPanel.Children.Clear();

				wss.Subscribe(topic);
				SubscrBut.Content = "Отписка";
				return;
			}

			SubscrBut.Content = "Подписка";
			wss.Unsubscribe(topic);
		}
		#endregion


		void DepthUpdate(dynamic json)
		{
			Snapshot(json);
			Delta(json);
			PublicTrade(json);
			DepthScroll.Upd();
			OBTdb.Insert();
		}

		void Snapshot(dynamic json)
		{
			if (json.topic != OrderbookTopic)
				return;
			if (json.type != "snapshot")
				return;

			long ts = json.cts;
			var Ask = json.data.a;
			var Bid = json.data.b;

			int c = Ask.Count;
			OBunit.PriceMax = Ask[c-1][0];
			c = Bid.Count;
			OBunit.PriceMin = Bid[c-1][0];

			OBunit.PriceAskF = Ask[0][0];
			OBunit.PriceBidF = Bid[0][0];

			var AskASS = new Dictionary<decimal, decimal>();
			foreach (var v in Ask)
				AskASS.Add((decimal)v[0], (decimal)v[1]);
			
			var BidASS = new Dictionary<decimal, decimal>();
			foreach (var v in Bid)
				BidASS.Add((decimal)v[0], (decimal)v[1]);

			var row = OBTdb.RowCreate(ts);
			for (decimal price = OBunit.PriceMax; price >= OBunit.PriceMin; price -= OBunit.Step)
			{
				decimal vol = 0;
				var type = DepthType.Spred;

				if (AskASS.ContainsKey(price))
				{
					vol = AskASS[price];
					row.Ask.Add($"{price},{vol}");
				}
				if (price > OBunit.PriceAskF)
					type = DepthType.Ask;

				if (BidASS.ContainsKey(price))
				{
					vol = BidASS[price];
					row.Bid.Add($"{price},{vol}");
				}
				if (price < OBunit.PriceBidF)
					type = DepthType.Bid;

				var unit = new OBunit(price, vol, type);
				OBunit.puASS.Add(price, unit);
				DepthPanel.Children.Add(unit.WP);
			}
		}

		// Расширение стакана с ценами вверх, если появилась более высокая цена
		void DepthAskIncrease(dynamic arr)
		{
			int c = arr.Count;
			if (c == 0)
				return;

			decimal price = arr[c-1][0];
			if (price <= OBunit.PriceMax)
				return;

			for (decimal prc = OBunit.PriceMax + OBunit.Step; prc <= price; prc += OBunit.Step)
			{
				var unit = new OBunit(prc, 0, DepthType.Ask);
				OBunit.puASS.Add(prc, unit);
				DepthPanel.Children.Insert(0, unit.WP);
			}
			OBunit.PriceMax = price;
		}
		// Расширение стакана с ценами вниз, если появилась более низкая цена
		void DepthBidIncrease(dynamic arr)
		{
			int c = arr.Count;
			if (c == 0)
				return;

			decimal price = arr[c-1][0];
			if (price >= OBunit.PriceMin)
				return;

			for (decimal prc = OBunit.PriceMin - OBunit.Step; prc >= price; prc -= OBunit.Step)
			{
				var unit = new OBunit(prc, 0, DepthType.Bid);
				OBunit.puASS.Add(prc, unit);
				DepthPanel.Children.Add(unit.WP);
			}
			OBunit.PriceMin = price;
		}
		string DeltaTestA(dynamic ask)
		{
			var PF = OBunit.PriceAskF;
			if (ask.Count == 0)
				return $"ask	{PF}: {OBunit.puASS[PF].Volume}		ask empty";

			var A = new List<string>();
			decimal last = 0;
			foreach (var v in ask)
			{
				decimal price = v[0];
				decimal vol = v[1];

				if (A.Count == 0)
				{
					string fp = "";
					if (PF == price && vol == 0)
						fp = $"---== ask /\\ ==--- F{PF} == {price} vol=0";
					if (PF > price)
						fp = $"---== ask \\/ ==--- F{PF} > {price}";
					A.Add($"ask	{PF}: {OBunit.puASS[PF].Volume} {fp}");
				}

				A.Add($"	{price}: {vol} {(last > price ? "### ask error ###" : "")}");
				last = price;
			}

			A.Reverse();
			return string.Join("\n", A.ToArray());
		}
		string DeltaTestB(dynamic bid)
		{
			var PF = OBunit.PriceBidF;
			if (bid.Count == 0)
				return $"bid	{PF}: {OBunit.puASS[PF].Volume}		bid empty";

			var B = new List<string>();
			decimal last = 100;
			foreach (var v in bid)
			{
				decimal price = v[0];
				decimal vol = v[1];

				if (B.Count == 0)
				{
					var fp = "";
					if (PF == price && vol == 0)
						fp = $"---== bid \\/ ==--- F{PF} == {price} vol=0";
					if (PF < price)
						fp = $"---== bid /\\ ==--- F{PF} < {price}";
					B.Add($"bid	{PF}: {OBunit.puASS[PF].Volume} {fp}");
				}

				B.Add($"	{price}: {vol} {(last < price ? "### bid error ###" : "")}");
				last = price;
			}

			return string.Join("\n", B.ToArray());
		}
		void Delta(dynamic json)
		{
			if (json.topic != OrderbookTopic)
				return;
			if (json.type != "delta")
				return;

			long ts = json.cts;
			var ask = json.data.a;
			var bid = json.data.b;

			var isAsk = ask.Count > 0;
			var isBid = bid.Count > 0;

			DepthAskIncrease(ask);
			DepthBidIncrease(bid);

			//WriteLine();
			//WriteLine(DeltaTestA(ask));
			//WriteLine(DeltaTestB(bid));

			var row = OBTdb.RowCreate(ts);

			if (isAsk)
				for(int i = ask.Count-1; i >= 0; i--)
				{
					decimal price = ask[i][0];
					decimal vol = ask[i][1];
					OBunit.puASS[price].VolumeUpd(vol);
					row.Ask.Add($"{price},{vol}");
				}

			if(isBid)
				for(int i = bid.Count-1; i >= 0; i--)
				{
					decimal price = bid[i][0];
					decimal vol = bid[i][1];
					OBunit.puASS[price].VolumeUpd(vol);
					row.Bid.Add($"{price},{vol}");
				}


			bool ffChanged = false;
			if (isAsk && isBid)
			{
				decimal pa = ask[0][0];
				decimal pb = bid[0][0];
				if (pa <= pb)
				{
					//WriteLine($"!  !  !  !  !  !  !  !  !  !  !  ! ! ! ! ! ! !!!!!!!!!! A{pa} <= B{pb} !!!!!!!!!! ! ! ! ! ! !  !  !  !  !  !  !  !  !  !  !  !");
					ffChanged = true;

					decimal va = ask[0][1];
					decimal vb = bid[0][1];
					if (va == 0)
					{
						OBunit.PriceAskF = pb + OBunit.Step;
						OBunit.PriceBidF = pb;
					}
					if (vb == 0)
					{
						OBunit.PriceBidF = pa - OBunit.Step;
						OBunit.PriceAskF = pa;
					}
				}
			}

			if (!ffChanged)
			{
				if (isAsk)
				{
					decimal price = ask[0][0];
					decimal vol = ask[0][1];
					if (OBunit.PriceAskF == price)
					{
						if (vol == 0)
						{
							// Цена отлетела наверх
							decimal p = OBunit.PriceAskF;
							while (OBunit.puASS[p].Volume == 0)
								p += OBunit.Step;

							OBunit.PriceAskF = p;
						}
					}
					else
						if (OBunit.PriceAskF > price)
						OBunit.PriceAskF = price;
				}

				if (isBid)
				{
					decimal price = bid[0][0];
					decimal vol = bid[0][1];
					if (OBunit.PriceBidF == price)
					{
						if (vol == 0)
						{
							// Цена провалилась вниз
							decimal p = OBunit.PriceBidF;
							while (OBunit.puASS[p].Volume == 0)
								p -= OBunit.Step;

							OBunit.PriceBidF = p;
						}
					}
					else
						// Цена отскочила вверх
						if (OBunit.PriceBidF < price)
						OBunit.PriceBidF = price;
				}
			}

			row.askPriceF = OBunit.PriceAskF;
			row.bidPriceF = OBunit.PriceBidF;
		}
		void PublicTrade(dynamic json)
		{
			if (json.topic != TradeTopic)
				return;
			if (json.type != "snapshot")
				return;

			foreach(var v in json.data)
			{
				long ts = v.T;
				decimal price = v.p;
				decimal vol = v.v;
				var row = OBTdb.RowCreate(ts);
				row.TradeAdd(v.S == "Buy", price, vol);
			}
		}
	}





	public class OBunit
	{
		public OBunit(decimal price, decimal vol = 0, DepthType type = DepthType.Spred)
		{
			Price = Math.Round(price, Decimals);
			Volume = vol;
			Type = type;
			WPcreate();
		}

		// Очистка статических переменных
		public static void StaticClear()
		{
			puASS = new Dictionary<decimal, OBunit>();
			_askF = 0;
			_bidF = 0;
			PriceMax = 0;
			PriceMin = 0;
		}
		public static Dictionary<decimal, OBunit> puASS { get; set; }

		static int _Decimals;
		public static int Decimals
		{
			get => _Decimals;
			set
			{
				_Decimals = value;
				Step = (decimal)1 / format.Exp(value);
			}
		}
		public static decimal Step { get; private set; }
		
		static int _VolumeDecimals;
		public static int VolumeDecimals
		{
			get => _VolumeDecimals;
			set
			{
				_VolumeDecimals = value;
				VolumeStep = (decimal)1 / format.Exp(value);
			}
		}
		public static decimal VolumeStep { get; private set; }
		
		public static decimal PriceMax { get; set; }
		static decimal _askF;
		public static decimal PriceAskF
		{
			get => _askF;
			set
			{
				if (_askF == value)
					return;

				decimal old = _askF;
				_askF = value;

				if (old == 0)
					return;

				while (puASS[_askF].Volume == 0)
					_askF += Step;

				//WriteLine($".............ASKF: {old} -> {_askF}");

				puASS[_askF].Type = DepthType.Ask;
				puASS[_askF].BGset();

				var type = old < _askF ? DepthType.Spred : DepthType.Ask;
				var step = Step * (old < _askF ? 1 : -1);

				while (old != _askF)
				{
					puASS[old].Type = type;
					puASS[old].BGset();
					old += step;
				}
			}
		}

		static decimal _bidF;
		public static decimal PriceBidF
		{
			get => _bidF;
			set
			{
				if (_bidF == value)
					return;

				decimal old = _bidF;
				_bidF = value;

				if (old == 0)
					return;


				while (puASS[_bidF].Volume == 0)
					_bidF -= Step;

				//WriteLine($".............BIDF: {old} -> {_bidF}");

				puASS[_bidF].Type = DepthType.Bid;
				puASS[_bidF].BGset();

				var type = old > _bidF ? DepthType.Spred : DepthType.Bid;
				var step = Step * (old > _bidF ? -1 : 1);

				while (old != _bidF)
				{
					puASS[old].Type = type;
					puASS[old].BGset();
					old += step;
				}
			}
		}
		public static decimal PriceMin { get; set; }

		public decimal Price { get; set; }

		DepthType _Type;
		public DepthType Type
		{
			get => _Type;
			set
			{
				if(Price == PriceAskF)
				{
					_Type = DepthType.AskFirst;
					return;
				}
				if(Price == PriceBidF)
				{
					_Type = DepthType.BidFirst;
					return;
				}
				_Type = value;
			}
		}

		public decimal Volume { get; set; }
		string VolumeStr =>
			Volume == 0 ? "" : Volume.ToString();
		public void VolumeUpd(decimal vol)
		{
			Volume = vol;
			(WP.Children[1] as Label).Content = VolumeStr;
		}

		public void TradePrint(decimal vol, bool isBuy = false)
		{
			var lb = WP.Children[0] as Label;
			lb.Foreground = format.RGB(isBuy ? "#229922" : "992222");
			lb.Content = vol;
		}

		public WrapPanel WP { get; set; }
		// Создание строки с ценой и объёмом для стакана
		void WPcreate()
		{
			WP = new WrapPanel();
			BGset();

			var lb = new Label();
			lb.Style = Application.Current.Resources["DepthLBL"] as Style;
			lb.Background = format.RGB("#FFFFFF");
			lb.Padding = new Thickness(0, 0, 5, 0);
			WP.Children.Add(lb);

			lb = new Label();
			lb.Content = VolumeStr;
			lb.Style = Application.Current.Resources["DepthLBL"] as Style;
			WP.Children.Add(lb);

			lb = new Label();
			lb.Content = Price;
			lb.Style = Application.Current.Resources["DepthLBL"] as Style;
			WP.Children.Add(lb);
		}

		// Окраска цен стакана
		public void BGset()
		{
			string bg;
			switch (Type)
			{
				default:
				case DepthType.Spred:	 bg = "#FFFFFF"; break;
				case DepthType.Ask:		 bg = "#EDC6C6"; break;
				case DepthType.AskFirst: bg = "#FFA09E"; break;
				case DepthType.BidFirst: bg = "#69CBAB"; break;
				case DepthType.Bid:		 bg = "#A0DBC6"; break;
			}
			WP.Background = format.RGB(bg);
		}
	}

	/// <summary>
	/// OrderBook Trade: внесение данных в базу
	/// </summary>
	public class OBTdb
	{
		public delegate void INF(int c);
		public static INF Info { get; set; }

		public OBTdb(string symbol)
		{
			dynamic info = BYBIT.LinearInfo(symbol);
			OBunit.Decimals = format.Decimals((decimal)info.priceFilter.tickSize);
			int qtyDecimals = format.Decimals((decimal)info.lotSizeFilter.qtyStep);

			Table = $"bybit_linear_{symbol}".ToLower();

			my.Obt.Query($"DROP TABLE IF EXISTS`{Table}`");

			string sql =
				$"CREATE TABLE`{Table}`(" +
					"`id` INT UNSIGNED NOT NULL AUTO_INCREMENT," +
				   $"`priceDecimals` TINYINT UNSIGNED DEFAULT {OBunit.Decimals}," +
				   $"`volDecimals` TINYINT UNSIGNED DEFAULT {qtyDecimals}," +
					"`ts` BIGINT UNSIGNED DEFAULT 0," +
					"`allCount` INT UNSIGNED DEFAULT 0," +
					"`askCount` INT UNSIGNED DEFAULT 0," +
					"`bidCount` INT UNSIGNED DEFAULT 0," +
					"`tradeCount` INT UNSIGNED DEFAULT 0," +
					"`ask`TEXT," +
					"`bid`TEXT," +
					"`trade`TEXT," +
				   $"`askPriceF` DECIMAL(20,{OBunit.Decimals}) UNSIGNED DEFAULT 0," +
				   $"`bidPriceF` DECIMAL(20,{OBunit.Decimals}) UNSIGNED DEFAULT 0," +
				   $"`tradePriceMin` DECIMAL(20,{OBunit.Decimals}) UNSIGNED DEFAULT 0," +
				   $"`tradePriceMax` DECIMAL(20,{OBunit.Decimals}) UNSIGNED DEFAULT 0," +
					"`tradePn` INT UNSIGNED DEFAULT 0," +
					"PRIMARY KEY (`id`)" +
				") ENGINE=MyISAM DEFAULT CHARSET=cp1251";
			my.Obt.Query(sql);

			InsertedRows = 0;
			Info = null;
			ASS = new Dictionary<long, Row>();
			isBusy = false;
		}
		static string Table { get; set; }


		static Dictionary<long, Row> ASS { get; set; }
		public static Row RowCreate(long ts) =>
			ASS.ContainsKey(ts) ? ASS[ts] : new Row(ts);
		public class Row
		{
			public Row(long ts)
			{
				Ts = ts;
				Ask = new List<string>();
				Bid = new List<string>();
				askPriceF = OBunit.PriceAskF;
				bidPriceF = OBunit.PriceBidF;
				TradeBuy = new Dictionary<decimal, decimal>();
				TradeSell = new Dictionary<decimal, decimal>();
				_tradePriceMin = 0;
				_tradePriceMax = 0;
				ASS.Add(ts, this);
			}

			long Ts { get; set; }
			public List<string> Ask { get; set; }
			public List<string> Bid { get; set; }
			public decimal askPriceF { get; set; }
			public decimal bidPriceF { get; set; }

			decimal _tradePriceMin;
			decimal TradePriceMin
			{
				get => _tradePriceMin;
				set
				{
					if(_tradePriceMin == 0 || _tradePriceMin > value)
						_tradePriceMin = value;
				}
			}
			decimal _tradePriceMax;
			decimal TradePriceMax
			{
				get => _tradePriceMax;
				set
				{
					if (_tradePriceMax == 0 || _tradePriceMax < value)
						_tradePriceMax = value;
				}
			}
			int TradePn => (int)((TradePriceMax - TradePriceMin) * format.Exp(OBunit.Decimals));

			Dictionary<decimal, decimal> TradeBuy { get; set; }
			Dictionary<decimal, decimal> TradeSell { get; set; }
			public void TradeAdd(bool dir, decimal p, decimal v)
			{
				TradePriceMin = p;
				TradePriceMax = p;

				if (dir)
				{
					if (!TradeBuy.ContainsKey(p))
						TradeBuy.Add(p, v);
					else
						TradeBuy[p] += v;
					return;
				}

				if (!TradeSell.ContainsKey(p))
					TradeSell.Add(p, v);
				else
					TradeSell[p] += v;
			}

			// Получение строки для внесения в базу
			public string Get()
			{
				int tradeCount = TradeBuy.Count + TradeSell.Count;
				int allCount = Ask.Count + Bid.Count + tradeCount;

				var trade = new List<string>();
				foreach(var t in TradeBuy)
					trade.Add($"1,{t.Key},{t.Value}");
				foreach(var t in TradeSell)
					trade.Add($"0,{t.Key},{t.Value}");

				return "(" +
					$"{Ts}," +
					$"{allCount}," +
					$"{Ask.Count}," +
					$"{Bid.Count}," +
					$"{tradeCount}," +
					$"'{string.Join(";", Ask.ToArray())}'," +
					$"'{string.Join(";", Bid.ToArray())}'," +
					$"'{string.Join(";", trade.ToArray())}'," +
					$"{askPriceF}," +
					$"{bidPriceF}," +
					$"{TradePriceMin}," +
					$"{TradePriceMax}," +
					$"{TradePn}" +
				")";
			}
		}

		static int InsertAtOnce => 100;			// Количество записей, которые вносятся за раз
		static int InsertedRows { get; set; }	// Всего внесено записей
		static bool isBusy { get; set; }
		async public static void Insert()
		{
			if (isBusy)
				return;
			if (ASS.Count < InsertAtOnce + 5)
				return;

			isBusy = true;

			var tss = new List<long>();
			foreach (var a in ASS)
			{
				tss.Add(a.Key);
				if (tss.Count >= InsertAtOnce)
					break;
			}
			await Task.Run(() =>
			{
				var list = new List<string>();
				foreach (var ts in tss)
				{
					list.Add(ASS[ts].Get());
					ASS.Remove(ts);
				}

				var sql =
					$"INSERT INTO`{Table}`(" +
						"`ts`," +
						"`allCount`," +
						"`askCount`," +
						"`bidCount`," +
						"`tradeCount`," +
						"`ask`," +
						"`bid`," +
						"`trade`," +
						"`askPriceF`," +
						"`bidPriceF`," +
						"`tradePriceMin`," +
						"`tradePriceMax`," +
						"`tradePn`" +
					 ")" +
					$"VALUES{string.Join(",", list.ToArray())}";
				my.Obt.Query(sql);
			});

			InsertedRows += tss.Count;
			Info?.Invoke(InsertedRows);
			isBusy = false;
		}
	}


	public class DepthScroll
	{
		public DepthScroll(DependencyObject scroll)
		{
			TopLast = 0;
			isBusy = false;
			Scroll = scroll as ScrollViewer;
		}

		static ScrollViewer Scroll { get; set; }
		static int TopLast { get; set; }
		static bool isBusy { get; set; }
		async public static void Upd()
		{
			if (isBusy)
				return;

			int ask = (int)((OBunit.PriceMax - OBunit.PriceAskF) / OBunit.Step);
			int spred = (int)((OBunit.PriceAskF - OBunit.PriceBidF) / OBunit.Step / 2);
			int top = (ask + spred) * 10 - 500;

			if (top < 0)
				top = 0;

			if (TopLast == top)
				return;
			if (Math.Abs(TopLast - top) < 450)
				return;

			isBusy = true;
			var prgs = new Progress<int>(v => Scroll.ScrollToVerticalOffset(v));
			await Task.Run(() => UpdProcess(prgs, top));
			TopLast = top;
			isBusy = false;
		}
		static void UpdProcess(IProgress<int> prgs, int top)
		{
			int step = 30;
			int count = Math.Abs(TopLast - top) / step;
			int start = TopLast;
			if (TopLast > top)
				step *= -1;
			while (count > 0)
			{
				count--;
				start += step;
				prgs.Report(start);
				Task.Delay(7).Wait();
			}
		}
	}

	public enum DepthType
	{
		Spred,
		Ask,
		AskFirst,
		BidFirst,
		Bid
	};
}
