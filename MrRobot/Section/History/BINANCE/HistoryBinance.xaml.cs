using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Connector;
using System.Linq;
using System;

namespace MrRobot.Section
{
	public partial class HistoryBinance : UserControl
	{
		public HistoryBinance()
		{
			InitializeComponent();
		}

		void Info(object s, RoutedEventArgs e)
		{
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
	}
}
