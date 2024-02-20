using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using static System.Console;

using Newtonsoft.Json;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.Interface;

namespace MrRobot.Section
{
    public partial class History : UserControl
    {
        /// <summary>
        /// Обновление валютных пар для ByBit
        /// </summary>
        async void InstrumentUpdateGo(object sender, RoutedEventArgs e)
        {
            InstrumentUpdateButton.Visibility = Visibility.Collapsed;
            InstrumentUpdateBar.Value = 0;
            InstrumentUpdateBarText.Content = "";
            InstrumentUpdateBarPanel.Visibility = Visibility.Visible;

            var progress = new Progress<decimal>(v => {
                InstrumentUpdateBar.Value = (double)v;
                InstrumentUpdateBarText.Content = v + "%";
            });
            await Task.Run(() => InstrumentUpdateProcess(progress));
            new BYBIT();
            HeadCountWrite();

            await Task.Run(() => Candle.DataControl(prgs: progress));
            new Candle();
            InstrumentUpdateButton.Visibility = Visibility.Visible;
            InstrumentUpdateBarPanel.Visibility = Visibility.Collapsed;
        }
        void InstrumentUpdateProcess(IProgress<decimal> Progress)
        {
            /*
            "symbol":"BTCUSDT",
            "baseCoin":"BTC",
            "quoteCoin":"USDT",
            "innovation":"0",
            "status":"Trading",
            "marginTrading":"both",
            "lotSizeFilter":{
                "basePrecision":"0.000001",
                "quotePrecision":"0.00000001",
                "minOrderQty":"0.000048",
                "maxOrderQty":"71.73956243",
                "minOrderAmt":"1",
                "maxOrderAmt":"2000000"},
            "priceFilter":{"tickSize":"0.01"}
             */

            // Ассоциативный массив инструментов по Symbol
            var mass = BYBIT.Instrument.FieldASS("Symbol");

            var wc = new WebClient();
            string json = wc.DownloadString("https://api.bybit.com/v5/market/instruments-info?category=spot");
            dynamic array = JsonConvert.DeserializeObject(json);
            var list = array.result.list;

            var bar = new ProBar(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if(bar.isUpd(i))
                    Progress.Report(bar.Value);

                var v = list[i];
                var lsf = v.lotSizeFilter;
                string symbol = v.symbol;

                //Инструмент присутствует в списке
                if (mass.ContainsKey(symbol))
                {
                    var unit = mass[symbol];
                    InstrumentValueCheck(unit, "basePrecision", unit.BasePrecision, lsf.basePrecision);
                    InstrumentValueCheck(unit, "minOrderQty", unit.MinOrderQty, lsf.minOrderQty);
                    InstrumentValueCheck(unit, "tickSize", unit.TickSize, v.priceFilter.tickSize);
                    InstrumentValueCheck(unit, "isTrading", unit.IsTrading, v.status == "Trading" ? "1" : "0");
                    InstrumentHistoryBeginUpdate(unit);
                    continue;
                }

                //Если отсутствует, внесение нового инструмента в базу
                string sql = "INSERT INTO`_instrument`(" +
								"`exchangeId`," +
                                "`symbol`," +
                                "`baseCoin`," +
                                "`quoteCoin`," +
                                "`basePrecision`," +
                                "`minOrderQty`," +
                                "`tickSize`" +
                             ")VALUES(" +
                               $"{BYBIT.ExchangeId}," +
                               $"'{symbol}'," +
                               $"'{v.baseCoin}'," +
                               $"'{v.quoteCoin}'," +
                               $"{lsf.basePrecision}," +
                               $"{lsf.minOrderQty}," +
                               $"{v.priceFilter.tickSize}" +
                              ")";
                var instr = new SpisokUnit(mysql.Query(sql));
                instr.Symbol = symbol;
                InstrumentLogInsert(instr, "Новый инструмент", "", (v.baseCoin + "/" + v.quoteCoin).ToString());
                InstrumentHistoryBeginUpdate(instr);
            }
        }

        /// <summary>
        /// Проверка и обновление изменённых параметров в инструменте
        /// </summary>
        void InstrumentValueCheck(SpisokUnit unit, string param, dynamic oldV, dynamic newV)
        {
            string oldS = format.E(oldV);
            string newS = format.E(newV);

            if (oldS == newS)
                return;

            string sql = "UPDATE`_instrument`" +
                        $"SET`{param}`='{newS}'" +
                        $"WHERE`id`={unit.Id}";
            mysql.Query(sql);

            InstrumentLogInsert(unit, $"Изменился параметр \"{param}\"", oldS, newS);
        }

        /// <summary>
        /// Внесение лога изменения в инструменте
        /// </summary>
        void InstrumentLogInsert(SpisokUnit unit, string about, string oldV, string newV)
        {
            string sql = "INSERT INTO `_instrument_log`(" +
							"`exchangeId`," +
                            "`instrumentId`," +
                            "`about`," +
                            "`old`," +
                            "`new`" +
                         ") VALUES (" +
                            $"{BYBIT.ExchangeId}," +
                            $"{unit.Id}," +
                            $"'{about}'," +
                            $"'{oldV}'," +
                            $"'{newV}'" +
                         ")";
            mysql.Query(sql);
        }

        /// <summary>
        /// Обновление начала истории по каждому инструменту ByBit
        /// </summary>
        void InstrumentHistoryBeginUpdate(SpisokUnit unit)
        {
            if (unit.HistoryBegin != null && !unit.HistoryBegin.Contains("0001"))
                return;

            string start = "1577826000";    //2020-01-01 - начало истории для всех инструментов

            var wc = new WebClient();

            // Сначала получение списка по неделям
            string str = $"https://api.bybit.com/v5/market/kline?category=spot&symbol={unit.Symbol}&interval=W&start={start}000&limit=1000";
            string json = wc.DownloadString(str);
            dynamic arr = JsonConvert.DeserializeObject(json);
            if (arr.retMsg == null)
                return;
            if (arr.retMsg != "OK")
                return;

            int count = arr.result.list.Count;
            if (count == 0)
                return;



            // Затем список по 15 мин., начиная с первого дня недели
            string last = arr.result.list[count - 1][0];
            str = $"https://api.bybit.com/v5/market/kline?category=spot&symbol={unit.Symbol}&interval=15&start={last}&limit=1000";
            json = wc.DownloadString(str);
            arr = JsonConvert.DeserializeObject(json);

            if (arr.retMsg == null)
                return;
            if (arr.retMsg != "OK")
                return;

            count = arr.result.list.Count;
            if (count == 0)
                return;



            // Затем список по 1 мин. для максимально точного получения начала истории
            last = arr.result.list[count - 1][0];
            str = $"https://api.bybit.com/v5/market/kline?category=spot&symbol={unit.Symbol}&interval=1&start={last}&limit=1000";
            json = wc.DownloadString(str);
            arr = JsonConvert.DeserializeObject(json);

            if (arr.retMsg == null)
                return;
            if (arr.retMsg != "OK")
                return;

            count = arr.result.list.Count;
            if (count == 0)
                return;

            last = arr.result.list[count - 1][0];
            last = last.Substring(0, 10);

            string sql = "UPDATE `_instrument`" +
                        $"SET `historyBegin`=FROM_UNIXTIME({last})" +
                        $"WHERE `id`={unit.Id}";
            mysql.Query(sql);
        }
    }
}
