using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    public partial class Converter : UserControl
    {
        public Converter()
        {
            InitializeComponent();
            ConverterInit();
        }

        public void ConverterInit()
        {
            if (global.IsInited(2))
                return;

            ConverterFindBox.Text = position.Val("2_ConverterFindBox_Text");
            SourceListBox.ItemsSource = Candle.List1m(ConverterFindBox.Text);
            SourceListBox.SelectedIndex = position.Val("2_SourceListBox_SelectedIndex", 0);

            global.Inited(2);
        }

        /// <summary>
        /// Быстрый поиск по исходным инструментам
        /// </summary>
        void SourceFind(object sender, TextChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            position.Set("2_ConverterFindBox_Text", box.Text);
            SourceListBox.ItemsSource = Candle.List1m(box.Text);
        }


        /// <summary>
        /// Выбран новый исходный таймфрейм
        /// </summary>
        void SourceListBoxChanged(object sender, MouseButtonEventArgs e)
        {
            var panel = (FrameworkElement)sender as StackPanel;
            var label = panel.Children[0] as Label;

            int content = Convert.ToInt32(label.Content);
            int index = SourceListBox.SelectedIndex;

            if (content != index)
                return;

            SourceListBoxChanged();
        }
        void SourceListBoxChanged(object sender, SelectionChangedEventArgs e) => SourceListBoxChanged();
        void SourceListBoxChanged()
        {
            position.Set("2_SourceListBox_SelectedIndex", SourceListBox.SelectedIndex);
            SourceListBox.ScrollIntoView(SourceListBox.SelectedItem);
            ResultListBox.ItemsSource = null;

            ResultListCreate();
            ChartBrowserShow();
        }

        void ChartBrowserShow()
        {
            if (global.IsAutoProgon)
                return;

            var item = SourceListBox.SelectedItem as CandleDataInfoUnit;
            if (item == null)
                return;

            ConverterBrowser.Address = new Chart("Converter", item.Table).PageHtml;
            ConverterChartHead.Update(item);
        }



        #region Процесс конвертации

        CandleDataParam ConvertParam;
        /// <summary>
        /// Составление массива с выбранными таймфреймами для конвертации
        /// </summary>
        int[] ConvertCheckedTF()
        {
            Dictionary<int, string> CheckTF = format.TFass();
            int count = 0;
            int[] CheckedTF = new int[100];
            foreach (int tf in CheckTF.Keys)
            {
                var check = FindName("CheckTF" + tf) as CheckBox;
                if (check == null)
                    continue;
                if (!(bool)check.IsChecked)
                    continue;

                CheckedTF[count++] = tf;
            }
            Array.Resize(ref CheckedTF, count);

            return CheckedTF;
        }
        /// <summary>
        /// Запуск конвертации
        /// </summary>
        async void ConvertGo(object sender, RoutedEventArgs e)
        {
            if (SourceListBox.SelectedIndex < 0)
                return;

            int[] CheckedTF = ConvertCheckedTF();
            if (CheckedTF.Length == 0)
                return;

            ConvertGoButton.Visibility = Visibility.Collapsed;
            ProcessPanel.Visibility = Visibility.Visible;
            TFpanel.IsEnabled = false;

            var SourceUnit = SourceListBox.SelectedItem as CandleDataInfoUnit;
            ConvertParam = new CandleDataParam()
            {
                Id = SourceUnit.Id,
                SourceTable = SourceUnit.Table,
                Symbol = SourceUnit.Symbol,
                NolCount = SourceUnit.NolCount,
                ConvertedIds = new int[CheckedTF.Length]
            };

            ProgressMain.Value = 0;
            ProgressSub.Value = 0;
            var progress = new Progress<int>(v => {
                ProgressMain.Value = ConvertParam.ProgressMainValue;
                ProgressSub.Value = v;

                // Снятие галочки с очередного сконвертированного таймфрейма
                if (v == 100)
                    (FindName("CheckTF" + ConvertParam.TimeFrame) as CheckBox).IsChecked = false;
            });
            await Task.Run(() => ConvertProcess(ConvertParam, CheckedTF, progress));

            new Candle();
            Instrument.DataCountPlus(SourceUnit.InstrumentId, CheckedTF.Length);
            SectionUpd.All();

            ConvertGoButton.Visibility = Visibility.Visible;
            ProcessPanel.Visibility = Visibility.Collapsed;
            TFpanel.IsEnabled = true;

            AutoProgon.PatternSearchSetup();
        }
        /// <summary>
        /// Процесс концертации в фоновом режиме
        /// </summary>
        void ConvertProcess(CandleDataParam PARAM, int[] CheckedTF, IProgress<int> Progress)
        {
            // Загрузка из базы исходного минутного таймфрейма
            string sql = $"SELECT*FROM`{PARAM.SourceTable}`";
            var SourceData = mysql.CandlesDataCache(sql);

            PARAM.Bar = new ProBar(CheckedTF.Length * SourceData.Count);
            for (int i = 0; i < CheckedTF.Length; i++)
            {
                PARAM.TimeFrame = CheckedTF[i];
                PARAM.TfNum = i;    // Счётчик для Main-прогресс-бара
                ConvertProcessTF(PARAM, SourceData, Progress);

                if (!PARAM.IsProcess)
                    return;
            }

            PARAM.ProgressMainValue = 100;
            Progress.Report(100);
        }
        /// <summary>
        /// Процесс конвертации в выбранные таймфреймы
        /// </summary>
        void ConvertProcessTF(CandleDataParam PARAM, List<CandleUnit> SourceData, IProgress<int> Progress)
        {
            var SubBar = new ProBar(SourceData.Count);
            Progress.Report(0);

            string TableName = Candle.DataTableCreate(PARAM);

            // Определение начала первой свечи согласно таймфрейму
            int iBegin;
            for (iBegin = 0; iBegin < SourceData.Count; iBegin++)
            {
                var src = SourceData[iBegin];
                if (src.Unix == Candle.UnixTF(src.Unix, PARAM.TimeFrame))
                    break;
            }
            var dst = new CandleUnit(SourceData[iBegin++], PARAM.TimeFrame);

            var insert = new List<string>();
            for (int i = iBegin; i < SourceData.Count; i++)
            {
                if (!PARAM.IsProcess)
                    return;

                var src = SourceData[i];

                if (!dst.Upd(src))
                {
                    insert.Add(dst.Insert);
                    Candle.DataInsert(TableName, insert, 500);
                    dst = new CandleUnit(src, PARAM.TimeFrame);
                }

                if (!SubBar.isUpd(i))
                    continue;

                PARAM.Bar.isUpd(PARAM.TfNum * SourceData.Count + i);
                PARAM.ProgressMainValue = PARAM.Bar.Value;
                Progress.Report(SubBar.Value);
            }

            Progress.Report(100);
            Candle.DataInsert(TableName, insert);
            PARAM.ConvertedIds[PARAM.TfNum] = Candle.InfoCreate(TableName, PARAM.Id);
        }
        /// <summary>
        /// Отмена процесса конвертации
        /// </summary>
        void ConvertCancel(object sender, RoutedEventArgs e)
        {
            ConvertParam.IsProcess = false;
        }

        #endregion



        /// <summary>
        /// Список таймфреймов с результатами конвертации
        /// </summary>
        public void ResultListCreate()
        {
            var item = SourceListBox.SelectedItem as CandleDataInfoUnit;
            if (item == null)
            {
                ConverterResultPanel.Visibility = Visibility.Hidden;
                return;
            }

            var list = Candle.ListOnIID(item.InstrumentId, false);
            ResultListBox.ItemsSource = list;
            ConverterResultPanel.Visibility = list.Count == 0 ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>
        /// Показ графика результата конвертации
        /// </summary>
        void ConverterResultChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListBox).SelectedItem as CandleDataInfoUnit;
            if (item == null)
                return;

            ConverterBrowser.Address = new Chart("Converter", item.Table).PageHtml;
            ConverterChartHead.Update(item);
        }

        /// <summary>
        /// Удаление результата конвертации
        /// </summary>
        void ConvertedX(object sender, MouseButtonEventArgs e)
        {
            var panel = ((FrameworkElement)sender).Parent as StackPanel;
            var label = panel.Children[0] as Label;

            int id = Convert.ToInt32(label.Content);
            Candle.InfoUnitDel(id);

            SectionUpd.All();
        }
    }
}
