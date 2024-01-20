using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

using CefSharp;
using MrRobot.inc;
using MrRobot.Entity;
using static RobotAPI.Robot;

namespace MrRobot.Section
{
    public partial class Tester : UserControl
    {
        /// <summary>
        /// Установка выбранного робота
        /// </summary>
        object ObjInstance { get; set; }
        MethodInfo Init;
        MethodInfo Step;
        MethodInfo Finish;
        int RobotId;        // Сохранение ID выбранного робота, чтобы потом не выбирать того же
        bool RobotApply(bool isElemChange = false)
        {
            if (RobotsListBox.SelectedIndex <= 0)
                return false;
            if(isElemChange && !(bool)VisualCheck.IsChecked)
                return false;

            var robot = RobotsListBox.SelectedItem as RobotUnit;

            if (RobotId == robot.Id)
                return true;

            if (!File.Exists(robot.Path))
            {
                RobotsListBox.SelectedIndex = 0;
                error.Msg("Отсутствует DLL-файл по указанному пути.");
                return false;
            }

            Assembly ASML = Assembly.LoadFrom(robot.Path);
            Type type = ASML.GetType(robot.Name);
            ObjInstance = Activator.CreateInstance(type);
            Init = type.GetMethod("Init");
            Step = type.GetMethod("Step");
            Finish = type.GetMethod("Finish");

            RobotId = robot.Id;

            return true;
        }

        /// <summary>
        /// Установка выбранного инструмента
        /// </summary>
        const double BaseBalance = 0;
        const double QuoteBalance = 100;
        int CandleId;
        bool CandlesLoadNeed { get; set; }
        void InstrumentSet()
        {
            var item = InstrumentListBox.SelectedItem as CandleDataInfoUnit;
            INSTRUMENT = Instrument.Unit(item.InstrumentId);
            INSTRUMENT.TimeFrame = item.TimeFrame;
            INSTRUMENT.RowsCount = item.RowsCount;
            INSTRUMENT.Table = item.Table;

            INSTRUMENT.BaseBalance = BaseBalance;
            INSTRUMENT.QuoteBalance = QuoteBalance;
            INSTRUMENT.BaseCommiss = 0;
            INSTRUMENT.QuoteCommiss = 0;

            // Если свечные данные не менялись, то загружаться из базы не будут
            CandlesLoadNeed = CandleId != item.Id;

            CandleId = item.Id;

            BaseBalanceCoin.Content = INSTRUMENT.BaseCoin;
            QuoteBalanceCoin.Content = INSTRUMENT.QuoteCoin;
        }

        /// <summary>
        /// Загрузка свечных данных
        /// </summary>
        void CandlesDataLoad()
        {
            if (!CandlesLoadNeed)
                return;

            string sql = "SELECT*" +
                        $"FROM`{INSTRUMENT.Table}`" +
                         "ORDER BY`unix`";
            CANDLES_DATA = mysql.CandlesData(sql);
            CANDLES_TF1 = null;
        }

        /// <summary>
        /// Загрузка свечных данных таймфрейма 1m
        /// </summary>
        void CandlesTF1load()
        {
            var item = InstrumentListBox.SelectedItem as CandleDataInfoUnit;

            if (!(bool)UseTF1Check.IsChecked)
                return;
            if (item.ConvertedFromId == 0)
                return;
            if (CANDLES_TF1 != null)
                return;

            var tf1 = Candle.Unit(item.ConvertedFromId);
            if (tf1 == null)
                return;

            string sql = "SELECT*" +
                        $"FROM`{tf1.Table}`" +
                         "ORDER BY`unix`";
            CANDLES_TF1 = mysql.CandlesData(sql);
        }
        void CandlesTF1use()
        {
            CANDLES_TF1_USE = false;
            
            var item = InstrumentListBox.SelectedItem as CandleDataInfoUnit;
            if (item.ConvertedFromId == 0)
                return;
            if (!(bool)UseTF1Check.IsChecked)
                return;

            CANDLES_TF1_USE = CANDLES_TF1 != null;
        }


        bool Visualization;             // Флаг включенной визуализации
        void GlobalInit(bool isElemChange = false)
        {
            PanelVisible();
            AutoGoStop();
            RobotLogList.Items.Clear();
            OrderExecuted.ItemsSource = null;

            if (!RobotApply(isElemChange))
                return;

            InstrumentSet();
            CandlesDataLoad();
            CandlesTF1load();
            CandlesTF1use();
            BalanceUpdate();

            new PATTERN(Patterns.ListAll());

            TESTER_GLOBAL_INIT();

            Init.Invoke(ObjInstance, new object[] { new string[]{} });
            RobotLog();

            if (TESTER_FINISHED)
            {
                Finish?.Invoke(ObjInstance, new object[] { });
                RobotLog();
                return;
            }

            OrderExecutedView();

            TesterChartInit();
            TesterBar.Value = 0;
        }
        /// <summary>
        /// Скрытие/отображение панелей
        /// </summary>
        void PanelVisible()
        {
            bool hide = RobotsListBox.SelectedIndex <= 0;
            BalancePanel.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            VisualPanel.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
            ProcessPanel.Visibility = hide || !Visualization ? Visibility.Hidden : Visibility.Visible;
        }
        void TesterChartInit()
        {
            var item = InstrumentListBox.SelectedItem as CandleDataInfoUnit;

            if (!Visualization)
            {
                TesterChartHead.Update(item);

                // Отображение пустой страницы вместо графика
                TesterBrowser.Address = new Chart().PageHtml;
                return;
            }

            TesterChartHead.Period();
            TesterChartHead.CandleCount();

            Chart chart = new Chart("Tester", item.Table);
            chart.PageName = "TesterProcess";
            chart.TesterGraficInit();
            TesterBrowser.Address = chart.PageHtml;
        }
        void BalanceUpdate()
        {
            BaseBalanceSum.Content  = format.Coin(INSTRUMENT.BaseBalance);
            QuoteBalanceSum.Content = format.Coin(INSTRUMENT.QuoteBalance);
            BaseCommissSum.Content  = format.Coin(INSTRUMENT.BaseCommiss);
            QuoteCommissSum.Content = format.Coin(INSTRUMENT.QuoteCommiss);
        }



        #region AutoGo

        DispatcherTimer AutoGoTimer;
        /// <summary>
        /// Запущено или нет автоматическое тестирование
        /// </summary>
        private bool AutoGoStatus()
        {
            return AutoGoTimer != null;
        }
        /// <summary>
        /// Остановка автоматического тестирования
        /// </summary>
        private void AutoGoStop()
        {
            if (!AutoGoStatus())
                return;

            AutoGoTimer.Stop();
            AutoGoTimer = null;
            AutoGoButtonStatus(false);
        }
        /// <summary>
        /// Запуск автоматического тестирования - нажатие на кнопку
        /// </summary>
        private void AutoGo(object sender, RoutedEventArgs e)
        {
            if (!Visualization)
                return;

            if (TESTER_FINISHED)
                return;

            if (AutoGoStatus())
            {
                AutoGoStop();
                BalanceUpdate();
                RobotLog();
                OrderExecutedView();
                return;
            }

            AutoGoTimer = new DispatcherTimer();
            AutoGoTimer.Interval = TimeSpan.FromMilliseconds(SliderV());
            AutoGoTimer.Tick += AutoGoTick;
            AutoGoTimer.Start();
            AutoGoButtonStatus();
            AutoGoSlider.Focus();
        }
        private void AutoGoTick(object sender, EventArgs e) => TesterRobotStep();
        private void AutoGoSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            position.Set("4_TesterSlider.Value", AutoGoSlider.Value.ToString());

            if (!AutoGoStatus())
                return;

            AutoGoTimer.Interval = TimeSpan.FromMilliseconds(SliderV());
        }
        /// <summary>
        /// Значения слайдера для изменения скорости движения графика во время тестирования
        /// </summary>
        private double SliderV()
        {
            Dictionary<double, double> ass = new Dictionary<double, double>();

            ass.Add(0, 1000);
            ass.Add(1, 850);
            ass.Add(2, 600);
            ass.Add(3, 350);
            ass.Add(4, 200);
            ass.Add(5, 150);
            ass.Add(6, 100);
            ass.Add(7, 55);
            ass.Add(8, 33);
            ass.Add(9, 17);
            ass.Add(10, 0);

            return ass[AutoGoSlider.Value];
        }
        /// <summary>
        /// Изменение состояния кнопки запуска теста
        /// </summary>
        private void AutoGoButtonStatus(bool go = true)
        {
            CandleAddButton.Visibility = go ? Visibility.Hidden : Visibility.Visible;
            AutoGoButton.Content = go ? "Стоп" : "Старт";
            TESTER_AUTO = go;

            if(go)
            {
                AutoGoButton.Background = new SolidColorBrush(Color.FromArgb(255, 0xCC, 0x6F, 0x6F)); // CC6F6F
                AutoGoButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x7B, 0x1C, 0x1C)); // 7B1C1C
                return;
            }

            AutoGoButton.Background = new SolidColorBrush(Color.FromArgb(255, 0x1A, 0xA5, 0x73)); // 1AA573
            AutoGoButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x0A, 0x42, 0x2E)); // 0A422E
        }

        #endregion



        /// <summary>
        /// Добавление очередной свечи в график - нажатие на кнопку
        /// </summary>
        void CandleAdd(object sender, RoutedEventArgs e) => TesterRobotStep();
        /// <summary>
        /// Очередной шаг: добавление новой свечи
        /// </summary>
        void TesterRobotStep()
        {
            if (!TESTER_GLOBAL_STEP())
                return;

            // Остановка авто-теста со стороны робота
            if (!TESTER_AUTO && AutoGoStatus())
            {
                AutoGoStop();
                BalanceUpdate();
                RobotLog();
                OrderExecutedView();
                return;
            }

            // Выполнение очередного шага в Роботе
            Step.Invoke(ObjInstance, new object[] { });

            // Обновление прогресс-бара над графиком
            TesterBar.Value = (double)CANDLES.Count / (double)INSTRUMENT.RowsCount * 100;

            // Отображение даты последней свечи в заголовке графика
            TesterChartHead.Period(DATE_TIME);

            // Отображение количества свечей в заголовке графика
            TesterChartHead.CandleCount(CANDLES.Count);

            // Вставка очередной свечи в график
            TesterBrowser.ExecuteScriptAsync($"candles.update({CANDLES[0].CandleToChart()})");

            if (!AutoGoStatus())
            {
                BalanceUpdate();
                RobotLog();
                OrderExecutedView();
            }
 
            RobotLine();

            if (!TESTER_FINISHED)
                return;

            AutoGoStop();
            Finish?.Invoke(ObjInstance, new object[] { });
            BalanceUpdate();
            RobotLog();
            OrderExecutedView();
        }




        #region NO VISUAL

        bool IsNoVisualProcess { get; set; } // Флаг процесса тестирования без визуализации
        /// <summary>
        /// Блокировка полей настроек перед запуском теста
        /// </summary>
        void NoVisualLock(string ButtonContent = "", int ButtonWidth = 0)
        {
            bool unlock = ButtonWidth > 0;
            NoVisualButton.Content = unlock ? ButtonContent : "Остановить";
            NoVisualButton.Width = unlock ? ButtonWidth : 80;
            InstrumentListBox.IsEnabled = unlock;
            RobotsListBox.IsEnabled = unlock;
            RobotAddButton.IsEnabled = unlock;
            UseTF1Check.IsEnabled = unlock;
            VisualCheck.IsEnabled = unlock;
            IsNoVisualProcess = !unlock;
        }
        /// <summary>
        /// Тестирование без визуализации - нажатие на кнопку
        /// </summary>
        async void NoVisualStart(object sender, RoutedEventArgs e)
        {
            string ButtonContent = NoVisualButton.Content.ToString();
            int ButtonWidth = (int)NoVisualButton.Width;

            if (!TESTER_FINISHED && IsNoVisualProcess)
            {
                NoVisualLock(ButtonContent, ButtonWidth);
                return;
            }

            NoVisualLock();
            GlobalInit();
            if (TESTER_FINISHED)
            {
                NoVisualLock(ButtonContent, ButtonWidth);
                AutoProgon.RobotTest();
                return;
            }


            var progress = new Progress<decimal>(v => { TesterBar.Value = (double)v; });
            await Task.Run(() => NoVisualProcess(progress));
            object res = Finish.Invoke(ObjInstance, new object[] { });

            BalanceUpdate();
            RobotLog();
            OrderExecutedView();

            NoVisualLock(ButtonContent, ButtonWidth);

            AutoProgon.RobotTest();
        }
        void NoVisualProcess(IProgress<decimal> Progress)
        {
            var bar = new ProBar(INSTRUMENT.RowsCount);

            while (TESTER_GLOBAL_STEP())
            {
                // Выполнение очередного шага в Роботе
                Step.Invoke(ObjInstance, new object[] { });

                if (bar.isUpd(CANDLES.Count))
                    Progress.Report(bar.Value);

                if (!IsNoVisualProcess)
                    return;
            }
            Progress.Report(100);
        }

        #endregion



        /// <summary>
        /// Вывод информации в лог Тестера
        /// </summary>
        void RobotLog()
        {
            var list = LOG_LIST();

            if (list == null)
                return;
            if (list.Count == 0)
                return;

            int count = RobotLogList.Items.Count;

            foreach(var unit in list)
                RobotLogList.Items.Insert(count++, unit);

            count = list.Count - 1;
            RobotLogList.ScrollIntoView(list[count]);
        }

        /// <summary>
        /// Рисование линии покупки/продажи на графике
        /// </summary>
        void RobotLine()
        {
            string[] line = LineGet();

            if (line == null)
                return;
            if (line.Length == 0)
                return;

            string script = "";
            for(int i = 0; i < line.Length; i++)
            {
                string[] spl = line[i].Split(';');
                string type = spl[0].ToUpper();

                script += $"Line{type}.price={spl[1]};candles.createPriceLine(Line{type});";
            }

            TesterBrowser.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Вывод исполненных SPOT ордеров
        /// </summary>
        void OrderExecutedView()
        {
            OrderExecuted.ItemsSource = null;

            if (ORDERS.Count == 0)
                return;

            OrderExecuted.ItemsSource = ORDERS;

            int c = ORDERS.Count - 1;
            OrderExecuted.ScrollIntoView(ORDERS[c]);
        }
    }
}
