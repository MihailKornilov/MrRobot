using System;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Console;

using Newtonsoft.Json;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для History.xaml
    /// </summary>
    public partial class History : UserControl
    {
        public History()
        {
            InitializeComponent();
            HistoryInit();
        }

        public void HistoryInit()
        {
            if (global.IsInited(1))
                return;

            MarketSel(1);

            InstrumentFindBox.Text = position.Val("1_InstrumentFindBox_Text");
            InstrumentListBoxFill();
            InstrumentListBox.SelectedIndex = position.Val("1_InstrumentListBox_SelectedIndex", 0);

            InstrumentCount.Text = Instrument.Count + " инструмент" + format.End(Instrument.Count, "", "а", "ов");

            global.Inited(1);
        }

        /// <summary>
        /// Быстрый поиск по инструментам ByBit
        /// </summary>
        private void FindBoxChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("1_InstrumentFindBox_Text", InstrumentFindBox.Text);
            InstrumentListBoxFill();
        }

        /// <summary>
        /// Заполнение списка инструментов с учётом поиска
        /// </summary>
        public void InstrumentListBoxFill()
        {
            string txt = InstrumentFindBox.Text;
            var list = Instrument.ListBox(txt);
            InstrumentListBox.ItemsSource = list;

            string lfc = list.Count > 0 ? "найдено: " + list.Count : "не найдено";
            LabelFound.Content = txt.Length > 0 ? lfc : "";
        }

        /// <summary>
        /// Выбран инструмент в списке
        /// </summary>
        private void InstrumentListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;

            if (box.SelectedIndex == -1)
                return;

            position.Set("1_InstrumentListBox_SelectedIndex", box.SelectedIndex);

            InstrumentInfoPanel.Visibility = Visibility.Visible;
            InstrumentDownloadPanel.Visibility = Visibility.Visible;

            var item = box.SelectedItem as InstrumentUnit;

            InstrumentListBox.ScrollIntoView(item);

            ByBitInstrumentName.Text = item.Name;
            ByBitInstrumentPrecision.Text = format.E(item.BasePrecision);
            ByBitInstrumentMinOrder.Text = format.E(item.MinOrderQty);
            ByBitInstrumentTickSize.Text = format.E(item.TickSize);
            ByBitInstrumentHistoryBegin.Text = item.HistoryBegin;

            DownloadedListCreate();

            string[] data = item.HistoryBegin.Split(' ');
            string[] sp = data[0].Split('.');
            if (sp.Length < 3)
                return;
            if (sp[2] == "0000")
                return;

            int year = int.Parse(sp[2]);
            int mon = int.Parse(sp[1]);
            int day = int.Parse(sp[0]);
            SetupDateBegin.SelectedDate = new DateTime(year, mon, day);
        }

        /// <summary>
        /// Ссылка на страницу инструмента сайта ByBit
        /// </summary>
        private void InstrumentNamePage(object sender, MouseButtonEventArgs e)
        {
            var block = sender as TextBlock;
            if (block == null)
                return;

            Process.Start("https://www.bybit.com/ru-RU/trade/spot/" + block.Text);
        }





        #region Download Process

        private CandleDataParam DownloadParam;

        /// <summary>
        /// Установка UNIX-даты окончания загрузки
        /// </summary>
        int UnixFinish()
        {
            var item = SetupPeriod.SelectedItem as ComboBoxItem;
            if (item.TabIndex > 0)
                return format.UnixFromDay(SetupDateBegin.Text) + item.TabIndex * 24 * 60 * 60;

            // По сегодняшний день
            return format.UnixNow();
        }

        /// <summary>
        /// Старт загрузки истории
        /// </summary>
        private async void DownloadGo(object sender, RoutedEventArgs e)
        {
            var Iitem = InstrumentListBox.SelectedItem as InstrumentUnit;

            // Таймфрейм
            var TFitem = SetupTimeFrame.SelectedItem as ComboBoxItem;

            DownloadParam = new CandleDataParam()
            {
                Symbol = Iitem.Symbol,
                TimeFrame = format.TimeFrame((string)TFitem.Content),
                UnixStart = format.UnixFromDay(SetupDateBegin.Text),
                UnixFinish = UnixFinish(),
                NolCount = format.NolCount(Iitem.TickSize),
                CC = 0
            };

            DownloadElemDisable();
            var progress = new Progress<int>(v => {
                DownloadProgressBar.Value = v;
                ProcessText.Text = $"{format.DayFromUnix(DownloadParam.UnixStart)}: " +
                                   $"загружено свечей: {DownloadParam.CC}" +
                                   $"   ({v}%)" +
                                   $"   {DownloadParam.Bar.TimeLeft}";
            });
            await Task.Run(() => DownloadProcess(DownloadParam, progress));
            DownloadElemEnable();

            Candle.ListCreate(true);
            Instrument.DataCountPlus(Iitem.Id);
            SectionUpd.All();

            AutoProgon.Converter(DownloadParam.Id);
        }

        /// <summary>
        /// Процесс скачивания исторических данных в фоновом режиме
        /// </summary>
        private void DownloadProcess(CandleDataParam PARAM, IProgress<int> Progress)
        {
            PARAM.Table = Candle.DataTableCreate(PARAM);

            DownloadCheck12(PARAM);

            var wc = new WebClient();
            PARAM.Bar = new ProBar((PARAM.UnixFinish - PARAM.UnixStart) / PARAM.TimeFrame / 60 / 1000);
            int barIndex = 0;
            var insert = new List<string>();
            bool isFinish = false;
            while (!isFinish)
            {
                if (!PARAM.IsProcess)
                    return;

                //Формирование запроса
                string url = "https://api.bybit.com/v5/market/kline?category=spot" +
                            "&symbol=" + PARAM.Symbol +
                            "&interval=" + PARAM.TimeFrame +
                            "&start=" + PARAM.UnixStart + "000" +
                            "&limit=1000";
                string json = wc.DownloadString(url);

                WriteLine($"{url}   {format.DTimeFromUnix(PARAM.UnixStart)}");

                PARAM.Bar.isUpd(barIndex++);
                Progress.Report(PARAM.Bar.Value);

                dynamic arr = JsonConvert.DeserializeObject(json);
                if (arr.retMsg == null)
                    break;
                if (arr.retMsg != "OK")
                    break;

                var list = arr.result.list;
                if (list.Count < 2)
                    break;

                int Unix = 0;
                for (int k = list.Count - 2; k >= 0; k--)
                {
                    var unit = new CandleUnit(list[k]);
                    Unix = unit.Unix;
                    if (Unix > PARAM.UnixFinish)
                    {
                        isFinish = true;
                        break;
                    }
                    insert.Add(unit.Insert);
                }

                PARAM.CC += insert.Count;
                Candle.DataInsert(PARAM.Table, insert);
                PARAM.UnixStart = Unix;
            }

            PARAM.Id = Candle.InfoCreate(PARAM.Table);
        }

        /// <summary>
        /// Проверка на первую половину суток для минутного таймфрейма (если время загрузки начинается после 16:00)
        /// </summary>
        private void DownloadCheck12(CandleDataParam PARAM)
        {
            if (PARAM.TimeFrame != 1)
                return;

            var wc = new WebClient();
            string url = "https://api.bybit.com/v5/market/kline?category=spot" +
                        "&symbol=" + PARAM.Symbol +
                        "&interval=1" +
                        "&start=" + PARAM.UnixStart + "000" +
                        "&limit=1000";

            string json = wc.DownloadString(url);
            dynamic arr = JsonConvert.DeserializeObject(json);

            if (arr.retMsg == null)
                return;
            if (arr.retMsg != "OK")
                return;
            if (arr.result.list.Count > 0)
                return;

            PARAM.UnixStart += 43_200; //прибавление 12 часов
        }

        /// <summary>
        /// Блокировка элементов настроек скачивания данных при начале загрузки
        /// </summary>
        private void DownloadElemDisable()
        {
            InstrumentListPanel.IsEnabled = false;
            DownloadProgressBar.Value = 0;
            SetupPanel.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;
            DownloadedPanel.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Разблокировка элементов настроек скачивания данных при начале загрузки
        /// </summary>
        private void DownloadElemEnable()
        {
            InstrumentListPanel.IsEnabled = true;
            SetupPanel.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Отмена процесса загрузки
        /// </summary>
        private void DownloadCancel(object sender, RoutedEventArgs e)
        {
            DownloadParam.IsProcess = false;
        }

        #endregion




        /// <summary>
        /// Список загруженных свечных данных по конкретному инструменту
        /// </summary>
        /// iid - ID инструмента из `_instrument`
        public void DownloadedListCreate()
        {
            if (InstrumentListBox.SelectedIndex == -1)
                return;

            var item = InstrumentListBox.SelectedItem as InstrumentUnit;
            var list = Candle.ListOnIID(item.Id);
            DownloadedList.ItemsSource = list;
            DownloadedPanel.Visibility = list.Count == 0 ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>
        /// Выбор из списка загруженных свечных данных
        /// </summary>
        private void DowloadedListChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListBox).SelectedItem as CandleDataInfoUnit;
            if (item == null)
                return;
            if (Candle.Unit(item.Id) == null)
                return;

            ChartHead.Update(item);
            var chart = new Chart("History", item.Table);
            HistoryBrowser.Address = chart.PageHtml;
            BrowserGrid.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Нажатие на крестик удаления загруженной истории
        /// </summary>
        private void DownloadedX(object sender, MouseButtonEventArgs e)
        {
            var panel = ((FrameworkElement)sender).Parent as StackPanel;
            var label = panel.Children[0] as Label;

            int id = Convert.ToInt32(label.Content);
            Candle.InfoUnitDel(id);

            SectionUpd.All();
        }
    }
}
