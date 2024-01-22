using System;
using System.Text;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Security.Cryptography;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Trade.xaml
    /// </summary>
    public partial class Trade : UserControl
    {
        public Trade()
        {
            InitializeComponent();
            TradeInit();
        }

        public void TradeInit()
        {
            if (global.IsInited(5))
                return;

            ApiKey.Text = position.Val("5_ApiKey_Text");
            ApiSecret.Text = position.Val("5_ApiSecret_Text");

            InstrumentQuoteCoin();
            InstrumentListBox.ItemsSource = Instrument.ListBox();
            InstrumentFindBox.Text = position.Val("5.InstrumentFindBox.Text");
            RobotsListBox.ItemsSource = Robots.ListBox();
            RobotsListBox.SelectedIndex = position.Val("5.RobotsListBox.Index", 0);

            global.Inited(5);
        }


        dynamic ByBitConnector(string query)
        {
            string API_KEY = ApiKey.Text;
            string API_SECRET = ApiSecret.Text;
            string URL = "https://api.bybit.com";
            long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long recvWindow = 5000;

            string signature = "";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(API_SECRET)))
            {
                string msg = $"{timestamp}{API_KEY}{recvWindow}";
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(msg));
                signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            }

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-BAPI-API-KEY", API_KEY);
            client.DefaultRequestHeaders.Add("X-BAPI-TIMESTAMP", timestamp.ToString());
            client.DefaultRequestHeaders.Add("X-BAPI-RECV-WINDOW", recvWindow.ToString());
            client.DefaultRequestHeaders.Add("X-BAPI-SIGN", signature);

            HttpResponseMessage res = client.GetAsync(URL + query).Result;
            string content = res.Content.ReadAsStringAsync().Result;
            dynamic array = JsonConvert.DeserializeObject(content);

            return array;
        }

        void ApiKeyChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("5_ApiKey_Text", ApiKey.Text);
        }
        void ApiSecretChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("5_ApiSecret_Text", ApiSecret.Text);
        }
        void ApiQuery(object sender, RoutedEventArgs e)
        {
            //dynamic res = ByBitConnector("/v5/user/query-api");
            //dynamic res = ByBitConnector("/v5/account/wallet-balance?accountType=UNIFIED&coin=USDT");
            //WriteLine(res);
        }




        /// <summary>
        /// Нажатие на "выбор" для выбора инструмента
        /// </summary>
        void InstrumentSelect(object sender, MouseButtonEventArgs e)
        {
            bool toHide = InstrumentSelectPanel.Visibility == Visibility.Visible;
            InstrumentSelectPanel.Visibility = toHide ? Visibility.Collapsed : Visibility.Visible;

            if (toHide)
                return;

            int id = position.Val("5.InstrumentListBox.Id", 0);
            if (id > 0)
                InstrumentListBox.ScrollIntoView(Instrument.Unit(id));

            InstrumentFindBox.Focus();
        }
        /// <summary>
        /// Произведён поиск в текстовом поле
        /// </summary>
        void FindBoxChanged(object sender, TextChangedEventArgs e)
        {
            string txt = InstrumentFindBox.Text;
            position.Set("5.InstrumentFindBox.Text", txt);
            InstrumentListBox.ItemsSource = Instrument.ListBox(txt);
        }
        /// <summary>
        /// Список котировочных монет с количествами
        /// </summary>
        void InstrumentQuoteCoin()
        {
            string sql = "SELECT" +
                            "`quoteCoin`," +
                            "COUNT(*)`count`" +
                         "FROM`_instrument`" +
                         "GROUP BY`quoteCoin`" +
                         "ORDER BY`count`DESC " +
                         "LIMIT 4";
            var list = mysql.QueryList(sql);
            var items = new List<FindUnit>();
            foreach (dynamic row in list)
            {
                items.Add(new FindUnit
                {
                    Coin = row["quoteCoin"],
                    Count = " (" + row["count"] + ")"
                });
            }

            QuoteCoinBox.ItemsSource = items;
        }
        /// <summary>
        /// Поиск по котировочной монете
        /// </summary>
        void QuoteCoinChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuoteCoinBox.SelectedIndex == -1)
                return;
            var item = QuoteCoinBox.SelectedItem as FindUnit;
            InstrumentFindBox.Text = "/" + item.Coin;
            QuoteCoinBox.SelectedIndex = -1;
        }
        /// <summary>
        /// Установка инструмента после полной загрузки приложения
        /// </summary>
        public void InstrumentSelect()
        {
            if (position.MainMenu() != 5)
                return;

            int id = position.Val("5.InstrumentListBox.Id", 0);
            if (id == 0)
                return;

            var item = InstrumentListBox.SelectedItem as InstrumentUnit;
            if (item != null)
                if (item.Id == id)
                    return;

            InstrumentListBox.SelectedItem = Instrument.Unit(id);
            InstrumentChanged();
        }
        /// <summary>
        /// Выбран инструмент
        /// </summary>
        void InstrumentChanged(object sender, MouseButtonEventArgs e) => InstrumentChanged();
        void InstrumentChanged()
        {
            InstrumentSelectPanel.Visibility = Visibility.Collapsed;

            var item = InstrumentListBox.SelectedItem as InstrumentUnit;
            if (item == null)
                return;

            InstrumentSelectBlock.Text = item.Name;
            InstrumentSelectBlock.Foreground = format.RGB("004481");
            InstrumentSelectBlock.FontWeight = FontWeights.Medium;

            InstrumentCancelLabel.Visibility = Visibility.Visible;

            position.Set("5.InstrumentListBox.Id", item.Id);

            TradeChartHead.Symbol(item.Name);

            var chart = new Chart("Trade");
            chart.PageName = "ChartActual";
            var CandleList = chart.TradeCandlesActual(item);
            TradeBrowser.Address = chart.PageHtml;

            Depth.Start(item.Symbol, CandleList);
        }

        /// <summary>
        /// Отмена выбора инструмента
        /// </summary>
        void InstrumentCancel(object sender, MouseButtonEventArgs e)
        {
            Depth.Stop();

            InstrumentSelectBlock.Text = "выбрать";
            InstrumentSelectBlock.Foreground = format.RGB("A0B5C8");
            InstrumentSelectBlock.FontWeight = FontWeights.Normal;

            InstrumentListBox.SelectedIndex = -1;

            InstrumentCancelLabel.Visibility = Visibility.Hidden;

            position.Set("5.InstrumentListBox.Id", 0);

            TradeChartHead.Symbol();
            TradeBrowser.Address = new Chart().PageHtml;
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


    /// <summary>
    /// Шаблон для котировочных монет (для быстрого поиска инструментов)
    /// </summary>
    public class FindUnit
    {
        public string Coin { get; set; }
        public string Count { get; set; }
    }
}
