using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using RobotLib;
using MrRobot.inc;
using MrRobot.Connector;
using MrRobot.Interface;
using MrRobot.Entity;

namespace MrRobot.Section
{
	public partial class HistoryBinance : UserControl
	{
		public HistoryBinance()
		{
			new BINANCE();

			DataContext = new BinanceDC();

			InitializeComponent();

			InstrLB.ItemsSource = BINANCE.Instrument.ListAll;
			InstrLB.SelectionChanged += InstrLBchanged;
			InstrLB.SelectedIndex = BinanceDC.IS_Index;
			InstrLB.ScrollIntoView(InstrLB.SelectedItem);

			new ChartLight(ChartPanel);
		}

		void InstrLBchanged(object s, SelectionChangedEventArgs e)
		{
			var unit = InstrLB.SelectedItem as SpisokUnit;

			BinanceDC.IS_Index = InstrLB.SelectedIndex;
			BinanceDC.IS_SymbolName = unit.Str04;
			BinanceDC.IS_TickSize = format.E(unit.Dec01);
			BinanceDC.IS_QtyMin	  = format.E(unit.Dec02);
			BinanceDC.IS_QtyStep  = format.E(unit.Dec03);
			BinanceDC.IS_History  = format.DTimeFromUnix((int)unit.Lng01);
			BinanceDC.DateBegin   = format.DTFromUnix((int)unit.Lng01);

			DataContext = new BinanceDC();
		}

		// Переход на сайт выбранного инструмента
		void SiteGo(object s, MouseButtonEventArgs e)
		{
			var txt = (s as TextBlock).Text;
			var spl = txt.Split('/');
			Process.Start($"https://www.binance.com/ru/trade/{spl[0]}_{spl[1]}?type=spot");
		}




		CDIparam PARAM;

		// Установка UNIX-даты окончания загрузки
		long UnixFinish()
		{
			var item = SetupPeriod.SelectedItem as ComboBoxItem;
			var today = format.UnixNow_MilliSec() - 60000;
			if (item.TabIndex > 0)
			{
				var finish = format.UnixMsFromDate(SetupDateBegin.Text) + (long)item.TabIndex * 24 * 60 * 60 * 1000;
				return finish > today ? today : finish;
			}

			// По сегодняшний день
			return today;
		}
		async void DownloadGo(object s, RoutedEventArgs e)
		{
			var IUnit = InstrLB.SelectedItem as SpisokUnit;

			PARAM = new CDIparam()
			{
				ExchangeId	 = BINANCE.ExchangeId,
				InstrumentId = IUnit.Id,
				Symbol		 = IUnit.Str01,
				UnixStart	 = format.UnixMsFromDate(SetupDateBegin.Text),
				UnixFinish   = UnixFinish(),
				Decimals	 = format.Decimals(IUnit.Dec01),
				QtyDecimals  = format.Decimals(IUnit.Dec03),
				CC = 0,
				Progress = new Progress<decimal>(v =>
				{
					ProBar.Value = (double)v;
					ProcessText.Text = $"{format.DayFromUnixMs(PARAM.UnixStart)}: " +
									   $"загружено тиков: {PARAM.CC}" +
									   $"   ({v}%)" +
									   $"   {PARAM.Bar.TimeLeft}";
				})
			};

			ElemBlock();
			await Task.Run(DownloadProcess);
			ElemBlock();
		}

		// Блокировка элементов настроек скачивания данных при загрузке
		void ElemBlock()
		{
			ProBar.Value = 0;
			SetupPanel.IsEnabled = !PARAM.IsProcess;
			G.Vis(ProgressPanel, PARAM.IsProcess);
			//			G.Hid(DownloadedPanel);
		}

		// Количество страниц для загрузки
		int BarCount()
		{
			var arr = BINANCE.Trades(PARAM.Symbol, PARAM.UnixStart, 1);
			if (arr.Count == 0)
				return 0;
			decimal idMin = arr[0].a;


			arr = BINANCE.Trades(PARAM.Symbol, PARAM.UnixFinish);
			if (arr.Count == 0)
				return 0;
			int c = arr.Count - 1;
			decimal idMax = arr[c].a;

			return (int)Math.Ceiling((idMax - idMin) / 1000m);
		}

		// Процесс скачивания исторических данных в фоновом режиме
		void DownloadProcess()
		{
			Tick.TDIcreate(PARAM);

			PARAM.Bar = new ProBar(BarCount(), 1000);

			int barIndex = 0;
			var insert = new List<string>();
			var unixList = new List<long>();	// Список Unix для избежания повторов
			while (PARAM.IsProcess)
			{
				var list = BINANCE.Trades(PARAM.Symbol, PARAM.UnixStart);
				int count = list.Count;
				if (count == 0)
					break;

				PARAM.Bar.Val(barIndex++, PARAM.Progress);

				for (int k = 0; k < count; k++)
				{
					var tick = new TickUnit(list[k]);

					while (unixList.Contains(tick.Unix))
						tick.Unix++;

					unixList.Add(tick.Unix);

					if (tick.Unix > PARAM.UnixFinish)
					{
						PARAM.IsProcess = false;
						break;
					}
					insert.Add(tick.Insert);
				}

				PARAM.CC += insert.Count;
				Tick.DataInsert(PARAM.Table, insert);
				PARAM.UnixStart = (long)(list[count - 1].T) + 1;
			}

			Tick.TDIupdate(PARAM);
		}


		// Отмена процесса загрузки
		void DownloadCancel(object s, RoutedEventArgs e) =>
			PARAM.IsProcess = false;









		// Обновление списка инструментов Binance
		void InstrumentListUpd(object s, RoutedEventArgs e)
		{
			return;

			var list = BINANCE.ExchangeInfo();

			string sql = $"DELETE FROM`_instrument`WHERE`exchangeId`={BINANCE.ExchangeId}";
			my.Main.Query(sql);

			var insert = new List<string>();
			for (int i = 0; i < list.Count; i++)
			{
				var unit = list[i];
				
				if (unit.status != "TRADING")
					continue;

				insert.Add("(" +
					$"{BINANCE.ExchangeId}," +
					$"'{unit.symbol}'," +
					$"'{unit.baseAsset}'," +
					$"'{unit.quoteAsset}'," +
					$"{unit.filters[1].minQty}," +
					$"{unit.filters[1].stepSize}," +
					$"{unit.filters[0].tickSize}" +
				")");
			}


			sql = "INSERT INTO`_instrument`(" +
					"`exchangeId`," +
					"`symbol`," +
					"`baseCoin`," +
					"`quoteCoin`," +
					"`qtyMin`," +
					"`qtyStep`," +
					"`tickSize`" +
				  $")VALUES{string.Join(",", insert.ToArray())}";
			my.Main.Query(sql);

			WriteLine($"inserted: {insert.Count}");
		}

		// Обновление даты начала исторических данных инструментов
		void HistoryBeginUpd()
		{
			foreach (var unit in BINANCE.Instrument.ListAll)
			{
				var unix = BINANCE.HistoryBegin(unit.Str01);

				if (unix == 0)
					continue;
				if (unit.Lng01 > 0)
					continue;

				var sql = "UPDATE`_instrument`" +
						 $"SET`historyBeginUnix`={unix} " +
						 $"WHERE`id`={unit.Id}";
				my.Main.Query(sql);
			}
		}


		// DataContext: Обновляемая информация на странице
		class BinanceDC
		{
			// Название биржи в заголовке
			public static string HDname =>
				G.Exchange.Unit(BINANCE.ExchangeId).Name;
			// Количество инструментов в заголовке
			public static string HDinstrCount =>
				$"{BINANCE.Instrument.Count} инструмент{format.End(BINANCE.Instrument.Count, "", "а", "ов")}";

			
			public static int IS_Index
			{
				get => position.Val("1.Binance.IS.Index", 0);
				set => position.Set("1.Binance.IS.Index", value);
			}

			public static string IS_SymbolName { get; set; }
			public static string IS_TickSize { get; set; }
			public static string IS_QtyMin { get; set; }
			public static string IS_QtyStep { get; set; }
			public static string IS_History { get; set; }
			public static DateTime DateBegin { get; set; }
		}

	}
}
