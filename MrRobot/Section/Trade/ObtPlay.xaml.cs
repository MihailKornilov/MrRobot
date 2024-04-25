using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using RobotLib;
using MrRobot.inc;

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

		public List<OBTunit> OBT { get; set; }
		void TablesLoad()
		{
			var list = new List<string>();
			my.Obt.Delegat("SHOW TABLES", res => list.Add(res.GetString(0)));
			TableCB.ItemsSource = list;
		}

		async void TableChange(object s, SelectionChangedEventArgs e)
		{
			DepthPanel.Children.Clear();
			ObtDC.Clear();

			OBT = new List<OBTunit>();
			var table = TableCB.SelectedItem.ToString();

			string sql = $"SELECT COUNT(*)FROM`{table}`";
			ObtDC.RowsCount = my.Obt.Count(sql);
			ObtDC.Upd();

			var prgs = new Progress<decimal>(v => PrgsTB.Text = $"{v}%");
			await Task.Run(() => LoadProcess(table, prgs));
			PrgsTB.Text = "";

			GlobalV(table);
			TssLBupd();
			Snapshot();

			TimeVol.Chart(LineChart);
		}
		void LoadProcess(string table, IProgress<decimal> prgs) {
			prgs.Report(0);
			var bar = new ProBar(ObtDC.RowsCount);

			string sql = $"SELECT" +
							"`ts`," +
							"`allCount`," +
							"`askCount`," +
							"`bidCount`," +
							"`tradeCount`," +

							"`ask`," +
							"`bid`," +
							"`trade`," +		// [7]

							"`askPriceF`," +
							"`bidPriceF`," +
							"`tradePn`" +		// [10]
					  $"FROM`{table}`" +
					   "ORDER BY`ts`" +
					   //"LIMIT 1000" +
					   "";
			my.Obt.Delegat(sql, res =>
			{
				var unit = new OBTunit(res.GetInt64(0));
				unit.Num = OBT.Count;
				ObtDC.AllCount	 += res.GetInt32(1);
				ObtDC.DepthCount += (res.GetInt32(2) + res.GetInt32(3));
				ObtDC.TradeCount += res.GetInt32(4);

				var str = res.GetString(5);
				if(str.Length > 0)
					foreach(var sp in str.Split(';'))
					{
						var v = sp.Split(',');
						decimal price = Convert.ToDecimal(v[0]);
						decimal vol = Convert.ToDecimal(v[1]);
						if (!unit.Ask.ContainsKey(price))
							unit.Ask.Add(price, vol);
					}

				str = res.GetString(6);
				if(str.Length > 0)
					foreach(var sp in str.Split(';'))
					{
						var v = sp.Split(',');
						decimal price = Convert.ToDecimal(v[0]);
						decimal vol = Convert.ToDecimal(v[1]);
						if (!unit.Bid.ContainsKey(price))
							unit.Bid.Add(price, vol);
					}

				unit.AskPriceF = res.GetDecimal(8);
				unit.BidPriceF = res.GetDecimal(9);

				// trade
				str = res.GetString(7);
				if (str.Length > 0)
					foreach (var sp in str.Split(';'))
					{
						var v = sp.Split(',');
						decimal price = Convert.ToDecimal(v[1]);
						decimal vol = Convert.ToDecimal(v[2]);
						if (v[0] == "1")
							unit.Buy.Add(price, vol);
						else
							unit.Sell.Add(price, vol);
					}

				unit.Pn = res.GetInt32(10);

				OBT.Add(unit);
				new TimeVol(unit);
				bar.Val(OBT.Count, prgs);
			});
		}
		// Глобальные значения: количество нулей после запятой, время, длительность
		void GlobalV(string table)
		{
			string sql = "SELECT" +
							"`priceDecimals`," +
							"`volDecimals`," +
							"MIN(`ts`)," +
							"MAX(`ts`)" +
					    $"FROM`{table}`" +
						 "LIMIT 1";
			my.Obt.Delegat(sql, res =>
			{
				OBunit.Decimals = res.GetInt32(0);
				OBunit.VolumeDecimals = res.GetInt32(1);
				long start = res.GetInt64(2);
				long finish = res.GetInt64(3);
				ObtDC.DTimeStart = format.DTimeFromUnix(start);
				ObtDC.DTimeFinish = format.DTimeFromUnix(finish);
				var ts = TimeSpan.FromMilliseconds(finish - start);
				ObtDC.DTimeDur = string.Format("{0}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
			});
		}
		// Список очередных событий
		void TssLBupd()
		{
			var items = new List<OBTunit>();
			int i = ObtDC.Num + 1;
			int c = 50;
			while(c > 0)
			{
				i++;
				//if (OBT[i].Pn < 50)
				//	continue;
				items.Add(OBT[i]);
				c--;
			}

			TssBL.ItemsSource = items;
		}

		// Первая прорисовка стакана
		void Snapshot()
		{
			OBunit.StaticClear();

			OBunit.PriceMax = OBT[0].Ask.Last().Key;
			OBunit.PriceMin = OBT[0].Bid.First().Key;

			OBunit.PriceAskF = OBT[0].AskPriceF;
			OBunit.PriceBidF = OBT[0].BidPriceF;

			for (decimal price = OBunit.PriceMax; price >= OBunit.PriceMin; price -= OBunit.Step)
			{
				decimal vol = 0;
				var type = DepthType.Spred;

				if (OBT[0].Ask.ContainsKey(price))
					vol = OBT[0].Ask[price];
				if (price > OBunit.PriceAskF)
					type = DepthType.Ask;

				if (OBT[0].Bid.ContainsKey(price))
					vol = OBT[0].Bid[price];
				if (price < OBunit.PriceBidF)
					type = DepthType.Bid;

				var unit = new OBunit(price, vol, type);
				OBunit.puASS.Add(price, unit);
				DepthPanel.Children.Add(unit.WP);
			}

			ObtDC.Upd();
			DepthScroll.Upd();
			DeltaBut.DataContext = OBT[1];
		}

		// Расширение стакана с ценами вверх, если появилась более высокая цена
		void DepthAskIncrease()
		{
			var n = ObtDC.Num;
			decimal price = 0;

			if (OBT[n].Ask.Count > 0)
				price = OBT[n].Ask.Last().Key;
			if (OBT[n].Buy.Count > 0)
				if (price < OBT[n].Buy.Last().Key)
					price = OBT[n].Buy.Last().Key;

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
		void DepthBidIncrease()
		{
			var n = ObtDC.Num;
			decimal price = OBunit.PriceMin;
			
			if (OBT[n].Bid.Count > 0)
				if (price > OBT[n].Bid.First().Key)
					price = OBT[n].Bid.First().Key; 
			if (OBT[n].Sell.Count > 0)
				if (price > OBT[n].Sell.First().Key)
					price = OBT[n].Sell.First().Key;

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

		void Delta(object s, RoutedEventArgs e)
		{
			Delta();
			TssLBupd();
			DepthScroll.Upd();
		}
		void Delta()
		{
			var n = ++ObtDC.Num;

			DepthAskIncrease();
			DepthBidIncrease();

			foreach (var v in OBT[n].Ask)
				OBunit.puASS[v.Key].VolumeUpd(v.Value);
			foreach (var v in OBT[n].Bid)
				OBunit.puASS[v.Key].VolumeUpd(v.Value);

			OBunit.PriceAskF = OBT[n].AskPriceF;
			OBunit.PriceBidF = OBT[n].BidPriceF;

			foreach (var v in OBT[n].Buy)
				OBunit.puASS[v.Key].TradePrint(v.Value, true);
			foreach (var v in OBT[n].Sell)
				OBunit.puASS[v.Key].TradePrint(v.Value);

			ObtDC.Upd();
			DeltaBut.DataContext = OBT[n+1];
		}
		// Переход на выбранное событие
		async void DeltaGo(object s, MouseButtonEventArgs e)
		{
			var unit = TssBL.SelectedItem as OBTunit;
			var prgs = new Progress<decimal>(v => Delta());
			while (ObtDC.Num < unit.Num - 1)
				await Task.Run(() => {
					(prgs as IProgress<decimal>).Report(0);
					Task.Delay(1).Wait();
				});
			TssLBupd();
			DepthScroll.Upd();
		}

		public class OBTunit
		{
			public OBTunit(long ts)
			{
				Ts = ts;
				Ask = new SortedDictionary<decimal, decimal>();
				Bid = new SortedDictionary<decimal, decimal>();
				Buy = new SortedDictionary<decimal, decimal>();
				Sell = new SortedDictionary<decimal, decimal>();
			}
			public int Num { get; set; }
			public string NumStr => $"{Num}.";
			public long Ts { get; set; }
			public string TsTime
			{
				get
				{
					var ts = TimeSpan.FromMilliseconds(Ts + format.OffsetMsec);
					return string.Format("{0}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
				}
			}


			public decimal AskPriceF { get; set; }
			public decimal BidPriceF { get; set; }
			public SortedDictionary<decimal, decimal> Ask { get; set; }
			public string AskCountStr =>
				Ask.Count > 0 ? $"a{Ask.Count}" : "-";
			public SortedDictionary<decimal, decimal> Bid { get; set; }
			public string BidCountStr =>
				Bid.Count > 0 ? $"b{Bid.Count}" : "-";


			public SortedDictionary<decimal, decimal> Buy { get; set; }
			public string BuyCountStr =>
				Buy.Count > 0 ? $"B{Buy.Count}" : "";
			public SortedDictionary<decimal, decimal> Sell { get; set; }
			public string SellCountStr =>
				Sell.Count > 0 ? $"S{Sell.Count}" : "";
			public int Pn { get; set; }
			public string PnStr =>
				Pn > 0 ? $"{Pn}" : "";
		}

		// DataContext
		public class ObtDC
		{
			public static void Upd() => G.ObtPlay.DataContext = new ObtDC();
			public static void Clear()
			{
				RowsCount = 0;
				AllCount = 0;
				DepthCount = 0;
				TradeCount = 0;
				Num = 0;
				Upd();
				new TimeVol();
			}

			public static int LbWidth => 95;

			// Количество строк в таблице
			public static Visibility RowsCountVis => G.Vis(RowsCount > 0);
			public static int RowsCount { get; set; }
			public static string RowsCountStr => format.Num(RowsCount);

			// Всего произошедших временных событий (стакан + сделки)
			public static Visibility InfoVis => G.Vis(AllCount > 0);
			public static int AllCount { get; set; }
			public static string AllCountStr => format.Num(AllCount);

			// Количество изменений стакана
			public static int DepthCount { get; set; }
			public static string DepthCountStr => format.Num(DepthCount);

			// Количество совершённых сделок
			public static int TradeCount { get; set; }
			public static string TradeCountStr => format.Num(TradeCount);

			// Время и длительность
			public static string DTimeStart { get; set; }
			public static string DTimeFinish { get; set; }
			public static string DTimeDur { get; set; }
			

			public static int Num { get; set; }
			public static string TsCur
			{
				get
				{
					var obt = G.ObtPlay.OBT;
					if (obt == null)
						return "-";
					if (obt.Count == 0)
						return "-";
					if (Num >= obt.Count)
						return "-";
					return obt[Num].TsTime;
				}
			}
		}


		// Значения объёмов за последний период времени
		public class TimeVol
		{
			static int Period => 3;     // Период в минутах

			public TimeVol()
			{
				Buy		  = new SortedDictionary<int, decimal>();
				BuyLine	  = new SortedDictionary<int, decimal>();
				Sell	  = new SortedDictionary<int, decimal>();
				SellLine  = new SortedDictionary<int, decimal>();
				PriceLine = new SortedDictionary<int, decimal>();
				Next = 0;
			}
			public TimeVol(OBTunit unit)
			{
				var buySum = unit.Buy.Values.Sum();
				var sellSum = unit.Sell.Values.Sum();
				decimal price = 0;

				if (buySum > 0)
					price = unit.Buy.Keys.Max();
				if (sellSum > 0)
					price = unit.Sell.Keys.Min();

				if (price == 0)
					return;

				int sec = (int)(unit.Ts / 1000);

				if (!Buy.ContainsKey(sec))
					 Buy.Add(sec, 0);
				if (!Sell.ContainsKey(sec))
					 Sell.Add(sec, 0);

				LineAdd(sec, price);
				Buy[sec] += buySum;
				Sell[sec] += sellSum;
			}

			static int PeriodSEC => Period * 60;
			public static SortedDictionary<int, decimal> Buy { get; set; }	// Секунда -> объём
			public static SortedDictionary<int, decimal> BuyLine { get; set; }
			public static SortedDictionary<int, decimal> Sell { get; set; }	// Секунда -> объём
			public static SortedDictionary<int, decimal> SellLine { get; set; }
			public static SortedDictionary<int, decimal> PriceLine { get; set; }
			static int Next { get; set; }
			static void LineAdd(int sec, decimal price)
			{
				if (Next == 0)
					Next = (sec / PeriodSEC) * PeriodSEC + PeriodSEC;

				if (sec < Next)
					return;

				int buyEnd  = Buy.Last().Key;
				int sellEnd = Sell.Last().Key;
				
				while (true)
				{
					bool finish = true;

					int begin = Buy.First().Key;
					if (buyEnd - begin > PeriodSEC)
					{
						Buy.Remove(begin);
						finish = false;
					}

					begin = Sell.First().Key;
					if (sellEnd - begin > PeriodSEC)
					{
						Sell.Remove(begin);
						finish = false;
					}

					if (finish)
						break;
				}

				BuyLine.Add(Next, Buy.Values.Sum());
				SellLine.Add(Next, Sell.Values.Sum());
				PriceLine.Add(Next, price);
				Next += 60;
			}
			public static void Chart(dynamic browser)
			{
				var tmp  = Path.GetFullPath($"Browser/Line/tmp.html");
				var html = Path.GetFullPath($"Browser/Line/chart.html");

				var read = new StreamReader(tmp);
				var write = new StreamWriter(html);

				var priceData = new List<string>();
				foreach (var v in PriceLine)
					priceData.Add($"{{time:{v.Key},value:{v.Value}}}");

				var buyData = new List<string>();
				foreach (var v in BuyLine)
					buyData.Add($"{{time:{v.Key},value:{v.Value}}}");

				var sellData = new List<string>();
				foreach (var v in SellLine)
					sellData.Add($"{{time:{v.Key},value:{v.Value}}}");

				string line;
				while ((line = read.ReadLine()) != null)
				{
					line = line.Replace("PRICE_DECIMALS", OBunit.Decimals.ToString());
					line = line.Replace("PRICE_STEP",	  OBunit.Step.ToString());
					line = line.Replace("PRICE_DATA", $"[\n{string.Join(",\n", priceData.ToArray())}]");

					line = line.Replace("VOLUME_DECIMALS", OBunit.VolumeDecimals.ToString());
					line = line.Replace("VOLUME_STEP", OBunit.VolumeStep.ToString());
					line = line.Replace("BUY_DATA",	  $"[\n{string.Join(",\n", buyData.ToArray())}]");
					line = line.Replace("SELL_DATA",  $"[\n{string.Join(",\n", sellData.ToArray())}]");
					write.WriteLine(line);
				}
				read.Close();
				write.Close();

				browser.Address = html;
			}
		}
	}
}
