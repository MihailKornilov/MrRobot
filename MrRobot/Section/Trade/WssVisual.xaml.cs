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
using System.Diagnostics;

namespace MrRobot.Section
{
	/// <summary>
	/// Логика взаимодействия для WssVisual.xaml
	/// </summary>
	public partial class WssVisual : Window
	{
		public WssVisual()
		{
			G.WssVisual = this;
			InitializeComponent();

			OrderbookTB.Text = OrderbookTopic;
			TradeTB.Text = TradeTopic;
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



		public Dictionary<decimal, WssDepthUnit> PriceUnitASS;

		void DepthUpdate(dynamic json)
		{
			if (json.topic != OrderbookTopic)
				return;

			Snapshot(json);
			Delta(json);
		}

		void Snapshot(dynamic json)
		{
			if (json.type != "snapshot")
				return;

			DepthPanel.Children.Clear();
			PriceUnitASS = new Dictionary<decimal, WssDepthUnit>();

			var Ask = json.data.a;
			var Bid = json.data.b;

			int c = Ask.Count;
			WssDepthUnit.PriceMax = Ask[c-1][0];
			c = Bid.Count;
			WssDepthUnit.PriceMin = Bid[c-1][0];

			WssDepthUnit.PriceAskF = Ask[0][0];
			WssDepthUnit.PriceBidF = Bid[0][0];

			var AskASS = new Dictionary<decimal, decimal>();
			foreach (var v in Ask)
				AskASS.Add((decimal)v[0], (decimal)v[1]);
			
			var BidASS = new Dictionary<decimal, decimal>();
			foreach (var v in Bid)
				BidASS.Add((decimal)v[0], (decimal)v[1]);

			for (decimal price = WssDepthUnit.PriceMax; price >= WssDepthUnit.PriceMin; price -= WssDepthUnit.Step)
			{
				decimal vol = 0;
				var type = DepthType.Spred;

				if (AskASS.ContainsKey(price))
					vol = AskASS[price];
				if (price > WssDepthUnit.PriceAskF)
					type = DepthType.Ask;

				if (BidASS.ContainsKey(price))
					vol = BidASS[price];
				if (price < WssDepthUnit.PriceBidF)
					type = DepthType.Bid;

				var unit = new WssDepthUnit(price, vol, type);
				PriceUnitASS.Add(price, unit);
				DepthPanel.Children.Add(unit.WP);
			}
		}
		void Delta(dynamic json)
		{
			if (json.type != "delta")
				return;

			foreach (var v in json.data.a)
			{
				decimal price = v[0];
				decimal vol = v[1];

				if (PriceUnitASS.ContainsKey(price))
				{
					var unit = PriceUnitASS[price];
					unit.VolumeAsk(vol);
					continue;
				}
				if (price > WssDepthUnit.PriceMax)
				{
					for (decimal prc = WssDepthUnit.PriceMax + WssDepthUnit.Step; prc <= price; prc += WssDepthUnit.Step)
					{
						var unit = new WssDepthUnit(prc, vol, DepthType.Ask);
						PriceUnitASS.Add(prc, unit);
						DepthPanel.Children.Insert(0, unit.WP);
					}
					WssDepthUnit.PriceMax = price;
				}
			}


			foreach (var v in json.data.b)
			{
				decimal price = v[0];
				decimal vol = v[1];

				if (PriceUnitASS.ContainsKey(price))
				{
					var unit = PriceUnitASS[price];
					unit.VolumeBid(vol);
					continue;
				}
				if (price < WssDepthUnit.PriceMin)
				{
					for (decimal prc = WssDepthUnit.PriceMin - WssDepthUnit.Step; prc >= price; prc -= WssDepthUnit.Step)
					{
						var unit = new WssDepthUnit(prc, vol, DepthType.Bid);
						PriceUnitASS.Add(prc, unit);
						DepthPanel.Children.Add(unit.WP);
					}
					WssDepthUnit.PriceMin = price;
				}
			}
		}
	}


	/*
		
	Ask = 0.013315
	Bid = 0.013314

	
	"a":[["0.013315","23200"],["0.013316","74900"],["0.013317","10400"],["0.013319","50600"],["0.013320","203400"],["0.013321","42000"],["0.013322","149400"],["0.013323","269600"],["0.013324","379800"],["0.013325","590600"],["0.013326","491100"],["0.013327","202400"],["0.013328","247900"],["0.013329","629200"],["0.013330","752500"],["0.013331","668200"],["0.013332","592400"],["0.013333","337200"],["0.013334","387900"],["0.013335","346100"],["0.013336","370200"],["0.013337","422800"],["0.013338","1026500"],["0.013339","259400"],["0.013340","297200"],["0.013341","1799200"],["0.013342","255200"],["0.013343","563500"],["0.013344","417400"],["0.013345","180800"],["0.013346","273100"],["0.013347","543800"],["0.013348","584400"],["0.013349","159500"],["0.013350","136200"],["0.013351","154000"],["0.013352","206000"],["0.013353","2747100"],["0.013354","2949200"],["0.013355","2866200"],["0.013356","199400"],["0.013357","204300"],["0.013358","93500"],["0.013359","273600"],["0.013360","211700"],["0.013361","91200"],["0.013362","122800"],["0.013363","61700"],["0.013364","154500"],["0.013365","165400"]],"u":19995104,"seq":1976974577},"cts":1711299546083}
	"b":[["0.013314","68800"],["0.013313","161400"],["0.013312","81400"],["0.013311","74600"],["0.013310","49800"],["0.013309","98700"],["0.013308","50100"],["0.013307","146700"],["0.013306","172600"],["0.013305","352700"],["0.013304","244300"],["0.013303","328400"],["0.013302","196200"],["0.013301","137400"],["0.013300","263700"],["0.013299","548100"],["0.013298","371800"],["0.013297","841800"],["0.013296","565300"],["0.013295","660500"],["0.013294","316700"],["0.013293","390600"],["0.013292","560200"],["0.013291","908300"],["0.013290","227800"],["0.013289","398700"],["0.013288","734500"],["0.013287","470400"],["0.013286","232100"],["0.013285","480800"],["0.013284","3050300"],["0.013283","247700"],["0.013282","375700"],["0.013281","277600"],["0.013280","643100"],["0.013279","2116000"],["0.013278","409600"],["0.013277","309400"],["0.013276","216200"],["0.013275","211800"],["0.013274","168000"],["0.013273","69700"],["0.013272","404100"],["0.013271","332300"],["0.013270","111800"],["0.013269","29400"],["0.013268","36000"],["0.013267","85900"],["0.013266","184000"],["0.013265","283000"]],

	"a":[["0.013315","0"],["0.013316","0"],["0.013317","0"],["0.013319","0"],["0.013320","0"],["0.013321","0"],["0.013322","0"],["0.013323","0"],["0.013324","154700"],["0.013366","172200"],["0.013367","148100"],["0.013368","459400"],["0.013369","169100"],["0.013370","47300"],["0.013371","42200"],["0.013372","90700"],["0.013373","105400"]],"u":19995130,"seq":1976975018},"cts":1711299547338}

	"b":[["0.013317","33100"],["0.013316","12000"],["0.013315","102600"],["0.013314","145500"],["0.013313","205800"],["0.013312","193200"],["0.013309","58900"],["0.013308","188400"],["0.013307","311100"],["0.013306","548700"],["0.013305","649500"],["0.013304","254200"],["0.013302","196000"],["0.013301","278200"],["0.013299","870500"],["0.013294","1735100"],["0.013291","855900"],["0.013288","788000"],["0.013286","2954800"],["0.013284","327600"],["0.013283","191400"],["0.013281","277700"],["0.013279","3368600"],["0.013278","268700"],["0.013277","168500"],["0.013276","75300"],["0.013275","141300"],["0.013274","95700"],["0.013267","0"],["0.013266","0"],["0.013265","0"]],
	"a":[["0.013323","22600"],["0.013324","49200"],["0.013325","38700"],["0.013326","672700"],["0.013327","52400"],["0.013328","60100"],["0.013329","583400"],["0.013330","316900"],["0.013331","105500"],["0.013332","419700"],["0.013333","196200"],["0.013334","159800"],["0.013335","355200"],["0.013336","304300"],["0.013337","249000"],["0.013341","380800"],["0.013345","193100"],["0.013346","285400"],["0.013347","350300"],["0.013349","300400"],["0.013351","294900"],["0.013352","483100"],["0.013353","94800"],["0.013354","3157600"],["0.013356","1826200"],["0.013358","247900"],["0.013361","104800"],["0.013362","136400"],["0.013370","2782400"],["0.013373","0"]],"u":19995131,"seq":1976975167},"cts":1711299547358}
19:59:07:598	821:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547380,"data":{"s":"BOMEUSDT","b":[["0.013315","147600"],["0.013314","102300"],["0.013313","228000"],["0.013312","376800"],["0.013310","100500"],["0.013309","72500"],["0.013307","166500"],["0.013303","285700"],["0.013302","296600"],["0.013294","1639500"],["0.013292","284400"],["0.013284","346200"],["0.013278","60400"],["0.013276","56600"],["0.013272","424400"]],"a":[["0.013321","6700"],["0.013325","46200"],["0.013328","73700"],["0.013329","177100"],["0.013330","76500"],["0.013332","269100"],["0.013336","353800"],["0.013342","204800"],["0.013348","503000"],["0.013353","165300"],["0.013356","1844800"],["0.013363","75300"],["0.013364","98600"],["0.013366","14400"],["0.013369","231800"],["0.013372","0"]],"u":19995132,"seq":1976975237},"cts":1711299547377}
19:59:07:598	774:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547400,"data":{"s":"BOMEUSDT","b":[["0.013318","22500"],["0.013317","70400"],["0.013314","87500"],["0.013312","412400"],["0.013310","112800"],["0.013309","84800"],["0.013308","202000"],["0.013307","159100"],["0.013305","696600"],["0.013304","395100"],["0.013302","331200"],["0.013301","486600"],["0.013292","340600"],["0.013268","0"]],"a":[["0.013321","29300"],["0.013323","0"],["0.013324","98000"],["0.013326","699200"],["0.013327","78300"],["0.013328","86000"],["0.013329","189800"],["0.013331","117900"],["0.013335","496100"],["0.013336","494700"],["0.013338","1234900"],["0.013340","366500"],["0.013342","239400"],["0.013370","2737200"],["0.013372","90700"]],"u":19995133,"seq":1976975300},"cts":1711299547398}
19:59:07:598	389:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547420,"data":{"s":"BOMEUSDT","b":[["0.013317","76100"],["0.013308","194400"],["0.013306","693300"],["0.013295","559600"],["0.013280","434800"],["0.013279","3279400"]],"a":[["0.013323","2200"],["0.013326","686800"],["0.013329","152700"],["0.013355","2826700"],["0.013372","0"]],"u":19995134,"seq":1976975358},"cts":1711299547418}
19:59:07:598	497:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547440,"data":{"s":"BOMEUSDT","b":[["0.013316","43100"],["0.013290","454000"],["0.013288","560900"],["0.013280","717100"],["0.013279","2997000"],["0.013275","185800"]],"a":[["0.013320","6600"],["0.013324","118000"],["0.013332","38200"],["0.013333","423100"],["0.013334","601300"],["0.013338","1249800"],["0.013341","99700"],["0.013342","520400"],["0.013365","259900"],["0.013371","0"]],"u":19995135,"seq":1976975396},"cts":1711299547438}
19:59:07:598	735:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547460,"data":{"s":"BOMEUSDT","b":[["0.013319","12000"],["0.013315","166900"],["0.013311","104400"],["0.013309","104400"],["0.013308","205100"],["0.013306","644200"],["0.013298","313500"],["0.013290","431500"],["0.013280","646700"],["0.013269","0"]],"a":[["0.013323","0"],["0.013324","120100"],["0.013325","35400"],["0.013326","372900"],["0.013329","127100"],["0.013330","88900"],["0.013331","155000"],["0.013333","928300"],["0.013334","742200"],["0.013341","119700"],["0.013347","362600"],["0.013348","515300"],["0.013354","3298500"],["0.013355","104000"],["0.013356","1796300"],["0.013365","273500"],["0.013371","42200"]],"u":19995136,"seq":1976975455},"cts":1711299547458}
19:59:07:844	548:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547480,"data":{"s":"BOMEUSDT","b":[["0.013319","12200"],["0.013318","22300"],["0.013316","108300"],["0.013315","76900"],["0.013313","198400"],["0.013305","837500"],["0.013301","509100"],["0.013286","2941500"],["0.013285","431400"],["0.013280","576200"],["0.013271","2861500"]],"a":[["0.013321","22600"],["0.013326","364300"],["0.013333","701400"],["0.013334","971200"],["0.013342","239400"],["0.013343","844500"],["0.013360","2860000"]],"u":19995137,"seq":1976975499},"cts":1711299547477}
19:59:07:844	392:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547500,"data":{"s":"BOMEUSDT","b":[["0.013316","83600"],["0.013313","224300"],["0.013312","438300"],["0.013309","116800"],["0.013303","251000"],["0.013299","835900"]],"a":[["0.013325","49000"],["0.013332","72800"],["0.013333","662200"],["0.013342","274100"],["0.013349","312700"]],"u":19995138,"seq":1976975540},"cts":1711299547498}
19:59:07:844	675:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547521,"data":{"s":"BOMEUSDT","b":[["0.013294","1620800"],["0.013281","600700"],["0.013280","293900"],["0.013279","3051200"],["0.013277","115500"],["0.013275","266100"],["0.013273","60100"],["0.013271","2748900"]],"a":[["0.013320","30200"],["0.013321","0"],["0.013324","96400"],["0.013326","325400"],["0.013329","85700"],["0.013331","117900"],["0.013333","978600"],["0.013335","537500"],["0.013354","3288900"],["0.013355","66900"],["0.013356","1805900"],["0.013358","285000"],["0.013359","193800"],["0.013362","192500"],["0.013365","297200"],["0.013372","90700"]],"u":19995139,"seq":1976975604},"cts":1711299547517}
19:59:07:844	305:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547540,"data":{"s":"BOMEUSDT","b":[["0.013274","116000"],["0.013272","404100"]],"a":[["0.013320","23600"],["0.013325","41500"],["0.013332","109900"],["0.013347","207400"],["0.013369","280300"]],"u":19995140,"seq":1976975621},"cts":1711299547535}
19:59:07:844	352:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547560,"data":{"s":"BOMEUSDT","b":[["0.013291","1082100"],["0.013290","205300"],["0.013288","547600"],["0.013287","457100"],["0.013283","296600"],["0.013272","298900"]],"a":[["0.013339","332000"],["0.013352","521800"],["0.013368","309800"]],"u":19995141,"seq":1976975634},"cts":1711299547556}
19:59:07:844	197:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547580,"data":{"s":"BOMEUSDT","b":[["0.013274","95700"],["0.013272","319200"]],"a":[],"u":19995142,"seq":1976975637},"cts":1711299547569}
19:59:07:844	198:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547601,"data":{"s":"BOMEUSDT","b":[["0.013306","678900"],["0.013303","285600"]],"a":[],"u":19995143,"seq":1976975645},"cts":1711299547596}
19:59:07:844	197:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547620,"data":{"s":"BOMEUSDT","b":[["0.013276","245200"]],"a":[["0.013337","288200"]],"u":19995144,"seq":1976975652},"cts":1711299547613}
19:59:07:844	176:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547641,"data":{"s":"BOMEUSDT","b":[],"a":[["0.013349","467900"]],"u":19995145,"seq":1976975663},"cts":1711299547636}
19:59:07:844	198:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547660,"data":{"s":"BOMEUSDT","b":[],"a":[["0.013339","287100"],["0.013353","210200"]],"u":19995146,"seq":1976975672},"cts":1711299547656}
19:59:07:844	609:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547680,"data":{"s":"BOMEUSDT","b":[["0.013316","12000"],["0.013315","31900"],["0.013314","15900"],["0.013313","149500"],["0.013312","396600"],["0.013311","62800"],["0.013310","96000"],["0.013309","75100"],["0.013308","188500"],["0.013307","94700"],["0.013306","280000"],["0.013305","679500"],["0.013304","344300"],["0.013303","241500"],["0.013302","270500"],["0.013301","467400"],["0.013300","536500"],["0.013299","560400"],["0.013298","219900"],["0.013297","447700"],["0.013296","418800"]],"a":[],"u":19995147,"seq":1976975712},"cts":1711299547670}
19:59:07:844	262:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":1711299547700,"data":{"s":"BOMEUSDT","b":[["0.013315","12000"],["0.013314","130400"],["0.013304","309600"],["0.013300","501900"]],"a":[["0.013330","123500"]],"u":19995148,"seq":1976975732},"cts":1711299547696}
19:59:08:093	263:	{"topic":"orderbook.50.BOMEUSDT","type":"delta","ts":
	1711299547721,"data":{"s":"BOMEUSDT","b":[["0.013315","106700"],["0.013313","319400"],["0.013309","286200"]],"a":[["0.013328","402400"],["0.013333","662200"]],"u":19995149,"seq":1976975742},"cts":
	1711299547715}
	*/




	public class WssDepthUnit
	{
		public WssDepthUnit(decimal price, decimal vol = 0, DepthType type = DepthType.Spred)
		{
			Price = price;
			Volume = vol;
			Type = type;
			WPcreate();
		}


		public static decimal Step = 0.000001m;
		public static decimal PriceMax { get; set; }

		static decimal _askF;
		public static decimal PriceAskF
		{
			get => _askF;
			set
			{
				_askF = value;
				if (!G.WssVisual.PriceUnitASS.ContainsKey(value))
					return;
				
				var unit = G.WssVisual.PriceUnitASS[value];
				unit.WP.Background = unit.BG;
			}
		}
		public static decimal PriceBidF { get; set; }
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
		public string VolumeStr =>
			Volume == 0 ? "" : Volume.ToString();
		public void VolumeAsk(decimal vol)
		{
			Volume = vol;
			Type = DepthType.Ask;

			if (vol == 0)
			{
				Type = DepthType.Spred;
				if (PriceAskF == Price)
					PriceAskF += Step;
			}
			else
				if (PriceAskF > Price)
					PriceAskF = Price;

			WP.Background = BG;
			(WP.Children[0] as Label).Content = VolumeStr;
		}
		public void VolumeBid(decimal vol)
		{
			Volume = vol;
			Type = DepthType.Bid;

			if (vol == 0 && PriceBidF == Price)
			{
				PriceBidF -= Step;
				Type = DepthType.Spred;
			}

			WP.Background = BG;
			(WP.Children[0] as Label).Content = VolumeStr;
		}

		public decimal Price { get; set; }

		public WrapPanel WP { get; set; }
		// Создание строки с ценой и объёмом для стакана
		void WPcreate()
		{
			WP = new WrapPanel();
			WP.Background = BG;

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
		public SolidColorBrush BG
		{
			get
			{
				switch (Type)
				{
					case DepthType.Ask:		 return format.RGB("#EDC6C6");
					case DepthType.AskFirst: return format.RGB("#FFA09E");
					case DepthType.BidFirst: return format.RGB("#69CBAB");
					case DepthType.Bid:		 return format.RGB("#A0DBC6");
				}
				return format.RGB("#FFFFFF");
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
