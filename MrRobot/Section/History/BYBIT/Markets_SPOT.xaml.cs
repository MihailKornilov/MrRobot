using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Diagnostics;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.Interface;
using System.Collections.Generic;
using System.Web.UI.WebControls.WebParts;
using System;
using System.Linq;

namespace MrRobot.Section
{
	public partial class Markets_SPOT : Window
	{
		FastFind ff;
		public Markets_SPOT()
		{
			TickersUpdate();
			InitializeComponent();

			ff = new FastFind(FFpanel, Filter.Txt);
			ff.Changed += txt =>
			{
				Filter.Txt = txt;
				ff.Count = SpotListShow();
			};
			ff.Count = SpotListShow();
		}

		async void TickersUpdate()
		{
			await Task.Run(BYBIT.Tickers);
		}

		void Sort(object sender, MouseButtonEventArgs e)
		{
			var lb = sender as Label;
			string tag = lb.Tag.ToString();
			Filter.Desc = Filter.Order == tag ? !Filter.Desc : false;
			Filter.Order = tag;
			SpotListShow();
		}

		// Скрытие ненужных инструментов
		void Hide(object s, MouseButtonEventArgs e)
		{
			var lb = s as Label;
			string tag = lb.Tag.ToString();
			var hdd = Filter.Hidden;
			Filter.Hidden = $"{hdd}{(hdd.Length > 0 ? "," : "")}{tag}";
			ff.Count = SpotListShow();
		}


		int SpotListShow() {
			var ListLimit = BYBIT.Instrument.ListLimit(Filter.Limit, Filter.Order, Filter.Desc, "SymbolName", Filter.Txt);

			var hdd = Array.ConvertAll(Filter.Hidden.Split(','), x => int.Parse(x));
			var list = new List<SpisokUnit>();
			foreach(var unit in ListLimit)
				if(!hdd.Contains(unit.Id))
					list.Add(unit);

			SpotList.ItemsSource = list;
			return list.Count;
		}

		void GoSite(object s, MouseButtonEventArgs e)
		{
			var box = s as ListBox;
			var item = box.SelectedItem as SpisokUnit;
			Process.Start($"https://www.bybit.com/ru-RU/trade/spot/{item.SymbolName}");
		}


		// Фильтр для отображения списка
		public class Filter
		{
			public static string Txt
			{
				get => position.Val("1.MarketsSpot.Txt");
				set => position.Set("1.MarketsSpot.Txt", value);
			}
			public static int Limit
			{
				get => position.Val("1.MarketsSpot.Limit", 100);
				set => position.Set("1.MarketsSpot.Limit", value);
			}
			public static string Order
			{
				get => position.Val("1.MarketsSpot.Order", "Symbol");
				set => position.Set("1.MarketsSpot.Order", value);
			}
			public static bool Desc
			{
				get => position.Val("1.MarketsSpot.Desc", false);
				set => position.Set("1.MarketsSpot.Desc", value);
			}
			public static string Hidden
			{
				get => position.Val("1.MarketsSpot.Hidden");
				set => position.Set("1.MarketsSpot.Hidden", value);
			}

		}
	}
}
