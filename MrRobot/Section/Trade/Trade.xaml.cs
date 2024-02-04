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

            new ISunit(TradeIS);

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
