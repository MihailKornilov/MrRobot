using System.Threading.Tasks;
using System.Windows;
using MrRobot.Connector;

namespace MrRobot.Section
{
	public partial class Markets_SPOT : Window
	{
		public Markets_SPOT()
		{
			TickersUpdate();
			InitializeComponent();
			SpotList.ItemsSource = BYBIT.Instrument.ListLimit(50);
		}

		async void TickersUpdate()
		{
			await Task.Run(BYBIT.Tickers);
		}
	}
}
