using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;

namespace MrRobot.Section
{
	/// <summary>
	/// Проигрывание записанного стакана и сделок
	/// </summary>
	public partial class ObtPlay : Window
	{
		public ObtPlay()
		{
			G.ObtPlay = this;
			ObtDC.Clear();
			InitializeComponent();

			new DepthScroll(DepthPanel.Parent);
			TablesLoad();
		}

		List<OBTunit> OBT { get; set; }
		int ObtNum { get; set; }
		void TablesLoad()
		{
			var list = new List<string>();
			my.Obt.Delegat("SHOW TABLES", res => list.Add(res.GetString(0)));
			TableCB.ItemsSource = list;
		}

		async void TableChange(object s, SelectionChangedEventArgs e)
		{
			DepthPanel.Children.Clear();

			OBT = new List<OBTunit>();
			var table = TableCB.SelectedItem.ToString();

			string sql = $"SELECT COUNT(*)FROM`{table}`";
			ObtDC.RowsCount = my.Obt.Count(sql);
			ObtDC.DepthCount = 0;
			ObtDC.Upd();

			var prgs = new Progress<decimal>(v => PrgsTB.Text = $"{v}%");
			await Task.Run(() => LoadProcess(table, prgs));
			PrgsTB.Text = "";

			// Количество записей у стакана
			sql = $"SELECT COUNT(*)FROM`{table}`WHERE!`act`";
			ObtDC.DepthCount = my.Obt.Count(sql);

			Snapshot();
		}
		void LoadProcess(string table, IProgress<decimal> prgs) {
			prgs.Report(0);
			var bar = new ProBar(ObtDC.RowsCount);

			string sql = $"SELECT" +
							"`ts`," +
							"`act`," +
							"`dir`," +
							"`price`," +
							"`vol`" +
					  $"FROM`{table}`" +
					   "ORDER BY`ts`";
			my.Obt.Delegat(sql, res =>
			{
				var unit = new OBTunit(res.GetInt64(0));
				unit.Act = res.GetBoolean(1);
				unit.Dir = res.GetBoolean(2);
				unit.Price = res.GetDecimal(3);
				unit.Vol = res.GetDecimal(4);
				OBT.Add(unit);
				bar.Val(OBT.Count, prgs);
			});
		}


		// Первая прорисовка стакана
		void Snapshot()
		{
			OBunit.StaticClear();
			OBunit.Decimals = format.Decimals(Math.Abs(OBT[0].Price - OBT[1].Price));

			ObtNum = 0;
			ObtDC.TsCur = OBT[0].Ts;
			var Ask = new List<OBTunit>();
			var Bid = new List<OBTunit>();
			var AskASS = new Dictionary<decimal, decimal>();
			var BidASS = new Dictionary<decimal, decimal>();
			foreach (var u in OBT)
			{
				if (u.Ts != ObtDC.TsCur)
				{
					ObtDC.TsNext = u.Ts;
					break;
				}

				ObtNum++;

				if (u.Act)
					continue;

				if (u.Dir)
				{
					Ask.Add(u);
					AskASS.Add(u.Price, u.Vol);
					continue;
				}

				Bid.Add(u);
				BidASS.Add(u.Price, u.Vol);
			}

			Ask = Ask.OrderByDescending(u => u.Price).ToList();
			Bid = Bid.OrderBy(u => u.Price).ToList();
			OBunit.PriceMax = Ask[0].Price;
			OBunit.PriceMin = Bid[0].Price;

			Ask.Reverse();
			Bid.Reverse();
			OBunit.PriceAskF = Ask[0].Price;
			OBunit.PriceBidF = Bid[0].Price;

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

			ObtDC.Upd();
			DepthScroll.Upd();
		}

		void Delta(object s, RoutedEventArgs e)
		{
			ObtDC.TsCur = ObtDC.TsNext;
			while (true)
			{
				long ts = OBT[ObtNum].Ts;
				if (ObtDC.TsCur != ts)
				{
					ObtDC.TsNext = ts;
					break;
				}
				ObtNum++;
			}

			ObtDC.Upd();
		}


		public class OBTunit
		{
			public OBTunit(long ts) => Ts = ts;
			public long Ts { get; set; }
			public bool Act { get; set; }
			public bool Dir { get; set; }
			public decimal Price { get; set; }
			public decimal Vol { get; set; }
		}

		// DataContext
		public class ObtDC
		{
			public static void Upd() => G.ObtPlay.DataContext = new ObtDC();
			public static void Clear()
			{
				RowsCount = 0;
				DepthCount = 0;
				Upd();
			}
			public static int LbWidth => 85;
			public static Visibility RowsCountVis => G.Vis(RowsCount > 0);
			public static int RowsCount { get; set; }
			public static string RowsCountStr => format.Num(RowsCount);
			public static Visibility InfoVis => G.Vis(DepthCount > 0);
			public static int DepthCount { get; set; }
			public static string DepthCountStr => format.Num(DepthCount);
			public static int TradeCount => RowsCount - DepthCount;
			public static string TradeCountStr => format.Num(TradeCount);


			public static long TsCur { get; set; }
			public static long TsNext { get; set; }
		}
	}
}
