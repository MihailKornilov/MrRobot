using System;
using System.Text;
using System.Linq;
using System.Windows;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using CefSharp;
using Newtonsoft.Json;
using MrRobot.Entity;
using MrRobot.inc;

namespace MrRobot.Section
{
    public static class Depth
    {
        static bool IsWork = false;

        static string Symbol;// Выбранный инструмент
        static int bp;       // Количество нулей после запятой в базовой монете (BasePrecision)
        static int qp;       // Количество нулей после запятой в котировочной монете (NolCount)

        static int DepthRows = 9;  // Количество строк, выводимых в стакане
        static int TradeRows = 100;// Количество строк, выводимых в сделках

        public static CandleUnit CandleFirst { get; set; }
        static SortedDictionary<double, double> Ask,  // Sell, верх стакана
                                                Bid;  // Buy,  низ стакана
        static List<DepthUnit> TradeList;   // Список сделок

        static string DataCut = "";

        static List<ClientWebSocket> CWS = new List<ClientWebSocket>();


        public static void Start(string symbol, List<object> CandleList)
        {
            Stop();

            if(CandleList.Count == 0)
            {
                error.Msg("Невозможно открыть график. Не получен список свечей.");
                return;
            }

            CandleFirst = CandleList[0] as CandleUnit;

            IsWork = true;
            Symbol = symbol;
            Init();
            WebSocket();
        }
        /// <summary>
        /// Обнуление стакана, если была выбрана другая валютная пара
        /// </summary>
        static void Init()
        {
            TradeList = new List<DepthUnit>();
            TradeItems();

            Ask = new SortedDictionary<double, double>();
            Bid = new SortedDictionary<double, double>();

            var Instr = Instrument.UnitOnSymbol(Symbol);
            bp = format.NolCount(Instr.BasePrecision);
            qp = Instr.NolCount;

            global.MW.Trade.DepthHeadPrice.Content = $"Цена({Instr.QuoteCoin})";
            global.MW.Trade.DepthHeadQty.Content = $"Кол-во({Instr.BaseCoin})";
            global.MW.Trade.DepthHeadAmount.Content = $"Всего({Instr.BaseCoin})";

            PriceUpdate(CandleFirst.Close.ToString(), "EF454A");
        }

        static async void WebSocket()
        {
            if (!IsWork)
                return;

            CWS.Add(new ClientWebSocket());
            int index = CWS.Count - 1;
            var CWSactual = CWS[index];
            var uri = new Uri("wss://stream.bybit.com/v5/public/spot");
            var buffer = new byte[5000];


            // ---=== СОЕДИНЕНИЕ ===---
            await CWSactual.ConnectAsync(uri, default);
            WriteLine($"CWS_{index}: Запрос на подключение отправлен.");
            await Task.Run(() =>
            {
                int wait = 20;
                while (CWSactual.State == WebSocketState.Connecting)
                {
                    WriteLine($"CWS_{index}: Выполняется подключение... {wait--}");
                    if(wait == 0)
                    {
                        WriteLine($"CWS_{index}: Время ожидания подключения истекло.");
                        return;
                    }
                    Thread.Sleep(300);
                }
            });

            if(CWSactual.State != WebSocketState.Open)
            {
                WriteLine($"CWS_{index}: Подключение не удалось. State = {CWSactual.State}");
                return;
            }

            WriteLine($"CWS_{index}: Подключение выполнено.");

            // ---=== ПОДПИСКА ===---
            //var message = "{\"op\":\"subscribe\",\"args\":[\"orderbook.50." + Symbol + "\",\"orderbook.50.BTCUSDT\"]}"; // Для подписки на несколько валютных пар
            var message = "{\"op\":\"subscribe\"," +
                           "\"args\":[" +
                                        "\"orderbook.50." + Symbol + "\"," +
                                        "\"publicTrade." + Symbol + "\"" +
                                     "]}";
            await CWSactual.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
                WebSocketMessageType.Text,
                true,
                default
            );

            WebSocketReceiveResult result;

            // ---=== ПОЛУЧЕНИЕ ДАННЫХ ===---
            while (IsWork && CWSactual.State == WebSocketState.Open)
            {
                try
                {
                    result = await CWSactual.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                }
                catch (WebSocketException ex)
                {
                    WriteLine($"CWS_{index}: Подключение было сброшено. State = " + CWSactual.State + ". " + ex.Message);
                    //WebSocket(Symbol);
                    return;
                }
                string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                DataGet(msg);
            }

            WriteLine($"CWS_{index}: Подключение было прервано. State = " + CWSactual.State);
            //WebSocket(Symbol);
        }


        /// <summary>
        /// Закрытие и удаление прошлых подключений
        /// </summary>
        static void WebSocketOldClose()
        {
            if (CWS.Count == 0)
                return;

            for (int i = 0; i < CWS.Count; i++)
            {
                if (CWS[i] == null)
                    continue;
                if (IsWork && CWS[i].State == WebSocketState.Connecting)
                    continue;

                WriteLine($"CWS_{i}: Очередь на закрытие. State = " + CWS[i].State);

                if (CWS[i].State == WebSocketState.Aborted
                 || CWS[i].State == WebSocketState.Closed)
                {
                    CWS[i] = null;
                    continue;
                }

                CWS[i].Dispose();
                //await CWS[i].CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Client closed", default);
            }
        }


        /// <summary>
        /// Проверка полученной строки с данными на корректность
        /// </summary>
        static bool IsDataCorrect(ref string str)
        {
            int len = str.Length;

            if (len == 0)
                return false;

            char begin = str[0];
            char end = str[len - 1];

            if (begin == '{' && end == '}')
                return true;

            if (end != '}')
            {
                DataCut += str;
                return false;
            }

            str = DataCut + str;
            DataCut = "";

            return true;
        }
        /// <summary>
        /// Наполнение данными Ask и Bid
        /// </summary>
        static bool DataFill(dynamic data, ref SortedDictionary<double, double> Book)
        {
            if (data.Count == 0)
                return false;

            for (int i = 0; i < data.Count; i++)
            {
                double price = Convert.ToDouble(data[i][0]);
                price = Math.Round(price, qp);
                double qty = Convert.ToDouble(data[i][1]);

                if (qty == 0)
                {
                    if (Book.ContainsKey(price))
                        Book.Remove(price);
                    continue;
                }

                Book[price] = Math.Round(qty, bp);
            }

            return true;
        }
        /// <summary>
        /// Получение данных из WebSocket
        /// </summary>
        static void DataGet(string str)
        {
            if (!IsDataCorrect(ref str))
                return;
            if (!str.Contains("type"))
                return;

            dynamic json;
            try { json = JsonConvert.DeserializeObject(str); }
            catch
            {
                WriteLine("-------- Ошибка JSON");
                WriteLine(str);
                return;
            }

            OrderBook(json);
            Trade(json);
        }




        /// <summary>
        /// Вывод данных в стакан
        /// </summary>
        static void OrderBook(dynamic json)
        {
            if (json.topic != "orderbook.50." + Symbol)
                return;
            if (DataFill(json.data.a, ref Ask))
                global.MW.Trade.DepthSellListBox.ItemsSource = AskItems();
            if (DataFill(json.data.b, ref Bid))
                global.MW.Trade.DepthBuyListBox.ItemsSource = BidItems();
        }
        static List<DepthUnit> ListItemsCreate(SortedDictionary<double, double> Book, bool isAsk = false)
        {
            if (Book.Count == 0)
                return null;

            var list = new List<DepthUnit>();
            int rows = DepthRows;
            double amount = 0;

            foreach (double price in isAsk ? Book.Keys : Book.Keys.Reverse())
            {
                double quantity = Book[price];
                amount += quantity;

                list.Add(new DepthUnit()
                {
                    IsBuy = !isAsk,
                    Price = format.Price(price, qp),
                    Quantity = format.Price(quantity, bp),
                    Amount = format.Price(amount, bp)
                });

                if (--rows == 0)
                    break;
            }

            if (isAsk)
                list.Reverse();

            return list;
        }
        static List<DepthUnit> AskItems() => ListItemsCreate(Ask, true);
        static List<DepthUnit> BidItems() => ListItemsCreate(Bid);



        /// <summary>
        /// Формирование и вывод списка сделок
        /// </summary>
        static void Trade(dynamic json)
        {
            if (json.topic != "publicTrade." + Symbol)
                return;
            if (json.data.Count == 0)
                return;

            int unix = 0;
            double price = 0;
            double volume = 0;
            for (int i = 0; i < json.data.Count; i++)
            {
                dynamic data = json.data[i];
                unix = Convert.ToInt32(data.T.ToString().Substring(0, 10));
                price = Convert.ToDouble(data.p);
                volume += Convert.ToDouble(data.v);

                var unit = new DepthUnit()
                {
                    IsBuy = data.S == "Buy",
                    Price = format.Price(data.p, qp),
                    Amount = format.Price(data.v, bp),
                    Time = format.TimeFromUnix(unix)
                };
                TradeList.Insert(0, unit);
                global.MW.Trade.TradeListAdd(unit);
            }

            CandleFirst.Update(unix, price, volume);
            global.MW.Trade.Candles_0_upd(CandleFirst);

            PriceUpdate(TradeList[0].Price, TradeList[0].PriceColor);
            ChartUpdate();
            TradeItems();

            if (TradeList.Count > TradeRows + 100)
                TradeList.RemoveRange(TradeRows, TradeList.Count - TradeRows);
        }
        static void TradeItems()
        {
            var list = new List<DepthUnit>();
            int rows = TradeList.Count < TradeRows ? TradeList.Count : TradeRows;

            for (int i = 0; i < rows; i++)
                list.Add(TradeList[i]);

            global.MW.Trade.TradeListBox.ItemsSource = list;
        }


        /// <summary>
        /// Обновление цены в стакане (которая крупным шрифтом)
        /// </summary>
        static void PriceUpdate(string price = "", string color = "000000")
        {
            global.MW.Trade.DepthPrice.Text = IsWork ? format.Price(price, qp) : "";
            global.MW.Trade.DepthPrice.Foreground = format.RGB(color);
        }
        /// <summary>
        /// Обновление свечи в графике
        /// </summary>
        static void ChartUpdate()
        {
            string script = $"candles.update({CandleFirst.CandleToChart()});" +
                            $"Volumes.update({CandleFirst.VolumeToChart()});";
            global.MW.Trade.EChart.Script(script);
        }

        /// <summary>
        /// Изменение размера стакана при изменении размера приложения
        /// </summary>
        public static void SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (position.MainMenu() != 5)
                return;

            double pph = global.MW.Trade.PricePanel.ActualHeight;
            double hph = global.MW.Trade.HeadPanel.ActualHeight;
            double height = Math.Floor((global.MW.Trade.DepthPanel.ActualHeight - pph - hph) / 2) - 2;
            DepthRows = (int)(height / 20);

            if (!IsWork)
                return;

            global.MW.Trade.DepthSellListBox.ItemsSource = AskItems();
            global.MW.Trade.DepthBuyListBox.ItemsSource = BidItems();
        }

        /// <summary>
        /// Остановка получения данных WebSocket
        /// </summary>
        public static void Stop()
        {
            IsWork = false;
            Symbol = null;

            WebSocketOldClose();
            PriceUpdate();

            global.MW.Trade.DepthSellListBox.ItemsSource = null;
            global.MW.Trade.DepthBuyListBox.ItemsSource = null;
            global.MW.Trade.TradeListBox.ItemsSource = null;
        }
    }

    /// <summary>
    /// Строка с данными стакана
    /// </summary>
    public class DepthUnit
    {
        public bool IsBuy { get; set; } // Ask = Sell, верх стакана красный
                                        // Bid = Buy,  низ стакана зелёный  
        public string Price { get; set; }
        public string PriceColor
        {
            get { return IsBuy ? "#20B26C" : "#EF454A"; }
        }
        public string Quantity { get; set; }
        public string Amount { get; set; }
        public string AmountColor
        {
            get { return IsBuy ? "#DBF3E7" : "#FCE1E2"; }
        }

        // ---=== Для Trade ===---
        public string Time { get; set; }
    }
}
