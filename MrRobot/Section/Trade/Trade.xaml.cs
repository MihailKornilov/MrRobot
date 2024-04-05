using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;

namespace MrRobot.Section
{
	/// <summary>
	/// Логика взаимодействия для Trade.xaml
	/// </summary>
	public partial class Trade : UserControl
	{
		public Trade() => G.Trade = this;

		public void TradeInit()
		{
			InitializeComponent();

			ApiKey.Text = BYBIT.API_KEY;
			ApiKey.TextChanged += (s, e) => BYBIT.API_KEY = (s as TextBox).Text;
			ApiSecret.Password = BYBIT.API_SECRET;
			ApiSecret.PasswordChanged += (s, e) => BYBIT.API_SECRET = (s as PasswordBox).Password;

			new ISunit(TradeIS);

			RobotsListBox.ItemsSource = Robots.ListBox();
			RobotsListBox.SelectedIndex = position.Val("5.RobotsListBox.Index", 0);

			FundLB.ItemsSource = BYBIT.FundBalance();
			UnifiedLB.ItemsSource = BYBIT.UnifiedBalance();
		}



		void QueryGo(object sender, RoutedEventArgs e)
		{
			BYBIT.BuyLimit("DOTUSDT", 8, 2m);
		}


		/// <summary>
		/// Открытие окна WebStream для записи стакана и сделок
		/// </summary>
		void WssOpen(object s, RoutedEventArgs e) =>
			new WssVisual().ShowDialog();

		void ObtOpen(object s, RoutedEventArgs e) =>
			new ObtPlay().ShowDialog();














		void MenuChanged(object sender, SelectionChangedEventArgs e)
		{
			var TC = sender as TabControl;
			if (TC.SelectedIndex == 2)
			{
				int c = LogList.Items.Count;
				if (c > 0)
					LogList.ScrollIntoView(LogList.Items[c - 1]);
			}
		}

		/// <summary>
		/// Выбран робот
		/// </summary>
		void RobotListChanged(object sender, SelectionChangedEventArgs e)
		{
			var box = sender as ComboBox;
			RobotButton.Visibility = box.SelectedIndex == 0 ? Visibility.Hidden : Visibility.Visible;
			position.Set("5.RobotsListBox.Index", box.SelectedIndex);
		}

		/// <summary>
		/// Запуск робота по кнопке
		/// </summary>
		void RobotButtonGo(object sender, RoutedEventArgs e) => GlobalInit();
	}
}
