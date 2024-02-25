using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using System.Collections.Generic;

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


			// Получение CDI-таблиц
			var tables = new List<string>();
			string sql = "SHOW TABLES";
			my.Main.Delegat(sql, res =>
			{
				string tab = res.GetString(0);
				if (tab.Substring(0, 1) != "_")
					tables.Add(tab);
			});

			var ASS = new Dictionary<string, CDIunit>();
			foreach (var unit in Candle.ListAll())
				ASS.Add(unit.Table, unit);

			int num = 1;
			foreach (var tab in tables)
				if (ASS.ContainsKey(tab))
					WriteLine($"{num++}. {tab}: {ASS[tab].Id}");
				else
				{
					WriteLine($"---------- {tab}");
					sql = $"DROP TABLE`{tab}`";
					my.Main.Query(sql);
				}

			// Установка "decimals" всем CDI
			foreach (var cdi in Candle.ListAll())
			{
				if (cdi.Decimals > 0)
					continue;

				var ii = cdi.IUnit;
				if (ii.Decimals == 0)
				{
					sql = $"DROP TABLE IF EXISTS`{cdi.Table}`";
					my.Main.Query(sql);

					sql = $"DELETE FROM`_candle_data_info`WHERE`id`={cdi.Id}";
					my.Main.Query(sql);

					WriteLine($"{cdi.Table} DELETED");
					continue;
				}

				WriteLine($"{cdi.Table}:	{cdi.Decimals}={ii.Decimals}		{ii.TickSize}	{ii.BasePrecision}");

				sql = "UPDATE`_candle_data_info`" +
					 $"SET`decimals`={ii.Decimals} " +
					 $"WHERE`id`={cdi.Id}";
				my.Main.Query(sql);
			}

			new Candle();

			WriteLine();
			// Переименование таблиц
			foreach (var cdi in Candle.ListAll())
			{
				string prefix = G.Exchange.Unit(cdi.ExchangeId).Prefix;
				sql = $"ALTER TABLE`{cdi.Table}`RENAME`{prefix}_{cdi.Id}`";
				my.Main.Query(sql);
				WriteLine(sql);
			}
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
