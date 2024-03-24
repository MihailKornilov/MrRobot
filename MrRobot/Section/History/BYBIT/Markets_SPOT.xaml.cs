using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using static System.Console;

using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.Interface;
using System.Diagnostics;

namespace MrRobot.Section
{
	public partial class Markets_SPOT : Window
	{
		public Markets_SPOT()
		{
			TickersUpdate();
			InitializeComponent();
			SpotListShow();
			var ff = new FastFind(FFpanel);
			ff.Changed += txt =>
			{
				ffTxt = txt;
				ff.Count = SpotListShow();
			};
		}

		async void TickersUpdate()
		{
			await Task.Run(BYBIT.Tickers);
		}

		string ffTxt = "";
		int Limit = 30;
		string Order = "Symbol";
		bool Desc = false;
		void Sort(object sender, MouseButtonEventArgs e)
		{
			var lb = sender as Label;
			string tag = lb.Tag.ToString();
			Desc = Order == tag ? !Desc : false;
			Order = tag;
			SpotListShow();
		}

		int SpotListShow() {
			var list = BYBIT.Instrument.ListLimit(Limit, Order, Desc);
			//var list = BYBIT.Instrument.ListFilterTxt("Symbol", ffTxt);
			SpotList.ItemsSource = list;
			return list.Count;
		}

		void GoSite(object s, MouseButtonEventArgs e)
		{
			var box = s as ListBox;
			var item = box.SelectedItem as SpisokUnit;
			Process.Start($"https://www.bybit.com/ru-RU/trade/spot/{item.SymbolName}");
		}
	}
}
