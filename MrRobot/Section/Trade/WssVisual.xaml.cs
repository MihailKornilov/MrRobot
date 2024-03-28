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
using System.Windows.Documents;

namespace MrRobot.Section
{
	/// <summary>
	/// Логика взаимодействия для WssVisual.xaml
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

			var args = new RoutedEventArgs(Button.ClickEvent);
			WssOpenBut.RaiseEvent(args);
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
		}

		void Snapshot(dynamic json)
		{
			if (json.topic != OrderbookTopic)
				return;
			if (json.type != "snapshot")
				return;

			long cts = json.cts;
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

			for (decimal price = OBunit.PriceMax; price >= OBunit.PriceMin; price -= OBunit.Step)
			{
				decimal vol = 0;
				var type = DepthType.Spred;

				if (AskASS.ContainsKey(price))
					vol = AskASS[price];
				if (price > OBunit.PriceAskF)
					type = DepthType.Ask;

				if (BidASS.ContainsKey(price))
					vol = BidASS[price];
				if (price < OBunit.PriceBidF)
					type = DepthType.Bid;

				var unit = new OBunit(price, vol, type);
				OBunit.puASS.Add(price, unit);
				DepthPanel.Children.Add(unit.WP);
				unit.DBinsert(cts);
			}

			//var PF = OBunit.PriceAskF;
			//WriteLine($"ask	{PF}: {OBunit.puASS[PF].Volume}");
			//PF = OBunit.PriceBidF;
			//WriteLine($"bid	{PF}: {OBunit.puASS[PF].Volume}");
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

			long cts = json.cts;
			var ask = json.data.a;
			var bid = json.data.b;

			var isAsk = ask.Count > 0;
			var isBid = bid.Count > 0;

			DepthAskIncrease(ask);
			DepthBidIncrease(bid);

			//WriteLine();
			//WriteLine(DeltaTestA(ask));
			//WriteLine(DeltaTestB(bid));

			if(isAsk)
				for(int i = ask.Count-1; i >= 0; i--)
				{
					decimal price = ask[i][0];
					decimal vol = ask[i][1];
					OBunit.puASS[price].VolumeUpd(vol, cts);
				}

			if(isBid)
				for(int i = bid.Count-1; i >= 0; i--)
				{
					decimal price = bid[i][0];
					decimal vol = bid[i][1];
					OBunit.puASS[price].VolumeUpd(vol, cts);
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

			if(ffChanged)
				return;


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
		void PublicTrade(dynamic json)
		{
			if (json.topic != TradeTopic)
				return;
			if (json.type != "snapshot")
				return;

			foreach(var v in json.data)
			{
				int dir = v.S == "Buy" ? 1 : 0;
				OBTdb.RowAdd($"({v.T},1,{dir},{v.p},{v.v})");
			}
		}
	}





	public class OBunit
	{
		public OBunit(decimal price, decimal vol = 0, DepthType type = DepthType.Spred)
		{
			Price = price;
			Volume = vol;
			Type = type;
			WPcreate();
		}

		// Очистка статический переменных
		public static void StaticClear()
		{
			puASS = new Dictionary<decimal, OBunit>();
			_askF = 0;
			_bidF = 0;
			PriceMax = 0;
			PriceMin = 0;
		}
		public static Dictionary<decimal, OBunit> puASS { get; set; }
		static decimal _Step;
		public static decimal Step {
			get => _Step;
			set
			{
				if(value == 0)
					throw new Exception("OBunit.Step не может равен 0.");
				_Step = value;
			}
		}
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
		public void VolumeUpd(decimal vol, long cts)
		{
			Volume = vol;
			(WP.Children[0] as Label).Content = VolumeStr;
			DBinsert(cts);
		}

		public decimal Price { get; set; }

		public WrapPanel WP { get; set; }
		// Создание строки с ценой и объёмом для стакана
		void WPcreate()
		{
			WP = new WrapPanel();
			BGset();

			var lb = new Label();
			lb.Width = 80;
			lb.Content = VolumeStr;
			lb.Style = G.WssVisual.Resources["DepthLBL"] as Style;
			WP.Children.Add(lb);

			lb = new Label();
			lb.Width = 62;
			lb.Content = Price;
			lb.Style = G.WssVisual.Resources["DepthLBL"] as Style;
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

		// Внесение данных в базу
		public void DBinsert(long cts)
		{
			int dir = 0;
			switch (Type)
			{
				case DepthType.Spred: return;
				case DepthType.Ask:
				case DepthType.AskFirst: dir = 1; break;
			}

			OBTdb.RowAdd($"({cts},0,{dir},{Price},{Volume})");
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
			int priceScale = info.priceScale;
			OBunit.Step = (decimal)1 / format.Exp(priceScale);

			decimal qtyStep = info.lotSizeFilter.qtyStep;
			int step = format.Decimals(qtyStep);

			Table = $"obt_{symbol}".ToLower();
			Rows = new List<string>();

			my.Obt.Query($"DROP TABLE IF EXISTS `{Table}`");

			string sql =
				$"CREATE TABLE`{Table}`(" +
					"`id` INT UNSIGNED NOT NULL AUTO_INCREMENT," +
					"`ts` BIGINT UNSIGNED DEFAULT 0," +
					"`act` TINYINT(1) UNSIGNED DEFAULT 0," +
					"`dir` TINYINT(1) UNSIGNED DEFAULT 0," +
				   $"`price` DECIMAL(20,{priceScale}) UNSIGNED DEFAULT 0," +
				   $"`vol`   DECIMAL(20,{step}) UNSIGNED DEFAULT 0," +
					"PRIMARY KEY (`id`)" +
				") ENGINE =MyISAM DEFAULT CHARSET=cp1251;";
			my.Obt.Query(sql);

			RowsAll = 0;
			Info = null;
		}
		static string Table { get; set; }
		static int Count => 500;// Количество записей, которые вносятся за раз
		static int RowsAll { get; set; }		// Всего внесено записай
		static List<string> Rows { get; set; }	// Список готовых записей для объединения в один запрос
		public static void RowAdd(string row)
		{
			Rows.Add(row);

			if (Rows.Count < Count)
				return;

			RowsAll += Rows.Count;

			string sql = $"INSERT INTO`{Table}`" +
							"(`ts`,`act`,`dir`,`price`,`vol`)" +
						  "VALUES" +
						   $"{string.Join(",", Rows.ToArray())}";
			Rows.Clear();
			Insert(sql);

			Info?.Invoke(RowsAll);
		}
		async static void Insert(string sql) =>
			await Task.Run(() => my.Obt.Query(sql));
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
