using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using System.Threading.Tasks;
using System.Threading;
using System;

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

			ApiKey.Text = BYBIT.ApiKey;
			ApiKey.TextChanged += BYBIT.ApiKeyChanged;
			ApiSecret.Password = BYBIT.ApiSecret;
			ApiSecret.PasswordChanged += BYBIT.ApiSecretChanged;
			ApiQueryTB.Text = ApiQuery;

			new ISunit(TradeIS);

			RobotsListBox.ItemsSource = Robots.ListBox();
			RobotsListBox.SelectedIndex = position.Val("5.RobotsListBox.Index", 0);
		}


		string ApiQuery
		{
			get => position.Val("5_ApiQuery_Text");
			set => position.Set("5_ApiQuery_Text", value);
		}
		void QueryGo(object sender, RoutedEventArgs e)
		{
			/*
				/v5/user/query-api - информация о ключах
				/v5/user/get-member-type - тип аккаунта
				/v5/account/wallet-balance?accountType=SPOT
			*/

			//ApiQuery = ApiQueryTB.Text;
			//dynamic res = ByBit.Api(ApiQuery);
			//QueryResult.Text = res.ToString();
		}


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
