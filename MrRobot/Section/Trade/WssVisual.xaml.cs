using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

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

			OrderbookTB.Text = OrderbookTopic;
			TradeTB.Text = TradeTopic;

			var args = new RoutedEventArgs(Button.ClickEvent);
			WssOpenBut.RaiseEvent(args);
		}
		string OrderbookTopic
		{
			get => position.Val("5.OrderbookTopic");
			set => position.Set("5.OrderbookTopic", value);
		}
		string TradeTopic
		{
			get => position.Val("5.TradeTopic");
			set => position.Set("5.TradeTopic", value);
		}
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
		void OrderbookSub(object s, RoutedEventArgs e)
		{
			if (wss == null) return;

			OrderbookTopic = OrderbookTB.Text;

			if(OrderbookBut.Content.ToString() == "Подписка")
			{
				OrderbookBut.Content = "Отписка";
				wss.Subscribe(OrderbookTopic);
				return;
			}

			OrderbookBut.Content = "Подписка";
			wss.Unsubscribe(OrderbookTopic);
		}
		void TradeSub(object s, RoutedEventArgs e)
		{
			if (wss == null) return;

			TradeTopic = TradeTB.Text;

			if (TradeBut.Content.ToString() == "Подписка")
			{
				TradeBut.Content = "Отписка";
				wss.Subscribe(TradeTopic);
				return;
			}

			TradeBut.Content = "Подписка";
			wss.Unsubscribe(TradeTopic);
		}
		#endregion


		public bool Stop = false;
		void DepthUpdate(dynamic json)
		{
			if (json.topic != OrderbookTopic)
				return;
			if (Stop)
				return;

			Snapshot(json);
			Delta(json);
		}

		void Snapshot(dynamic json)
		{
			if (json.type != "snapshot")
				return;

			DepthPanel.Children.Clear();
			OBunit.puASS = new Dictionary<decimal, OBunit>();

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
			}

			var PF = OBunit.PriceAskF;
			WriteLine($"ask	{PF}: {OBunit.puASS[PF].Volume}");
			PF = OBunit.PriceBidF;
			WriteLine($"bid	{PF}: {OBunit.puASS[PF].Volume}");
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
			if (json.type != "delta")
				return;

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
					OBunit.puASS[price].VolumeUpd(vol);
				}

			if(isBid)
				for(int i = bid.Count-1; i >= 0; i--)
				{
					decimal price = bid[i][0];
					decimal vol = bid[i][1];
					OBunit.puASS[price].VolumeUpd(vol);
				}


			bool ffChanged = false;
			if (isAsk && isBid)
			{
				decimal pa = ask[0][0];
				decimal pb = bid[0][0];
				if (pa <= pb)
				{
					WriteLine($"!  !  !  !  !  !  !  !  !  !  !  ! ! ! ! ! ! !!!!!!!!!! A{pa} <= B{pb} !!!!!!!!!! ! ! ! ! ! !  !  !  !  !  !  !  !  !  !  !  !");
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


		public static Dictionary<decimal, OBunit> puASS { get; set; }
		public static decimal Step => 0.1m;
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

				WriteLine($".............ASKF: {old} -> {value}");

				while (puASS[_askF].Volume == 0)
					_askF += Step;

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

				WriteLine($".............BIDF: {old} -> {value}");

				while (puASS[_bidF].Volume == 0)
					_bidF -= Step;

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
		public void VolumeUpd(decimal vol)
		{
			Volume = vol;
			(WP.Children[0] as Label).Content = VolumeStr;
		}
		// Изменение объёмов в ценах Ask
		public void VolumeAsk(decimal vol)
		{
			VolumeUpd(vol);

			if (vol > 0)
			{
				// Объём не коснулся первой цены
				if (PriceAskF <= Price)
					return;

				// Цена была продавлена вниз
				decimal last = PriceAskF;
				PriceAskF = Price;
				for (decimal p = Price; p <= last; p += Step)
				{
					puASS[p].Type = DepthType.Ask;
					puASS[p].BGset();
				}
				return;
			}

			if (Price != PriceBidF)
				return;

			// Цена отлетела наверх
			while (true)
			{
				decimal p = PriceAskF;
				PriceAskF += Step;
				puASS[p].Type = puASS[p].Volume == 0 ? DepthType.Spred : DepthType.Ask;
				puASS[p].BGset();
				if (puASS[PriceAskF].Volume > 0)
				{
					puASS[PriceAskF].BGset();
					break;
				}
			}
		}
		// Изменение объёмов в ценах Bid
		public void VolumeBid(decimal vol)
		{
			Volume = vol;
			(WP.Children[0] as Label).Content = VolumeStr;

			if (vol > 0)
			{
				// Объём не коснулся первой цены
				if (PriceBidF >= Price)
					return;

				// Цена подскочила вверх
				decimal last = PriceBidF;
				PriceBidF = Price;
				for (decimal p = last; p <= Price; p += Step)
				{
					puASS[p].Type = DepthType.Bid;
					puASS[p].BGset();
				}
//				G.WssVisual.Stop = true;
				return;
			}

			if (Price != PriceBidF)
				return;

			// Цена провалилась вниз
			while (true)
			{
				decimal p = PriceBidF;
				PriceBidF -= Step;
				puASS[p].Type = puASS[p].Volume == 0 ? DepthType.Spred : DepthType.Bid;
				puASS[p].BGset();
				if (puASS[PriceBidF].Volume > 0)
				{
					puASS[PriceBidF].BGset();
					break;
				}
			}
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
