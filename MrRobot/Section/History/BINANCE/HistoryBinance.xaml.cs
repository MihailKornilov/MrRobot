using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Diagnostics;
using System.Collections.Generic;
using static System.Console;

using RobotLib;
using MrRobot.inc;
using MrRobot.Connector;
using MrRobot.Interface;

namespace MrRobot.Section
{
	public partial class HistoryBinance : UserControl
	{
		public HistoryBinance()
		{
			DataContext = new BinanceDC();

			InitializeComponent();

			InstrLB.ItemsSource = BINANCE.Instrument.ListAll;
			InstrLB.SelectionChanged += InstrLBchanged;
		}

		void InstrLBchanged(object s, SelectionChangedEventArgs e)
		{
			var unit = InstrLB.SelectedItem as SpisokUnit;

			BinanceDC.IS_SymbolName = unit.Str04;
			BinanceDC.IS_TickSize = format.E(unit.Dec01);
			BinanceDC.IS_QtyMin	  = format.E(unit.Dec02);
			BinanceDC.IS_QtyStep  = format.E(unit.Dec03);
			BinanceDC.IS_History  = format.DTimeFromUnix((int)unit.Lng01);

			DataContext = new BinanceDC();
		}

		// Переход на сайт выбранного инструмента
		void SiteGo(object s, MouseButtonEventArgs e)
		{
			var txt = (s as TextBlock).Text;
			var spl = txt.Split('/');
			Process.Start($"https://www.binance.com/ru/trade/{spl[0]}_{spl[1]}?type=spot");
		}





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

			public static string IS_SymbolName { get; set; }
			public static string IS_TickSize { get; set; }
			public static string IS_QtyMin { get; set; }
			public static string IS_QtyStep { get; set; }
			public static string IS_History { get; set; }
		}
	}
}
