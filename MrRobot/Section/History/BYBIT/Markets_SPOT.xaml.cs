using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using static System.Console;

using MrRobot.Entity;
using MrRobot.Connector;

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
			//SpotList.ItemsSource = BYBIT.Instrument.ListLimit(Limit, Order, Desc);
			var list = BYBIT.Instrument.ListFilterTxt("Symbol", ffTxt);
			SpotList.ItemsSource = list;
			return list.Count;
		}
	}
}
