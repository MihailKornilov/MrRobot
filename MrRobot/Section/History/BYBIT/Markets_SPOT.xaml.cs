using System.Windows;

namespace MrRobot.Section
{
	public partial class Markets_SPOT : Window
	{
		public Markets_SPOT()
		{
			DataContext = new MWsizeDC();
			WindowState = MWsizeDC.State;

			InitializeComponent();
		}
	}
}
