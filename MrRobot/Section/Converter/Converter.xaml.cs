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
            CDIpanel.Page(2).TBLink = SelectLink.TBLink;
            CDIpanel.Page(2).OutMethod += SourceChanged;
            SourceChanged();
            Candle.Updated += ResultListCreate;
        }


        bool IsSourceChosen => SourceId > 0;        // Свечные данные выбраны
        int SourceId => CDIpanel.Page(2).CdiId;             // ID свечных данных
        CDIunit SourceUnit => Candle.Unit(SourceId);// Единица свечных данных


        public void SourceChanged()
        {
            if (!IsSourceChosen)
                return;
            if (global.IsAutoProgon)
                return;

            ResultListCreate();
            new AdvChart(ChartPanel, SourceUnit);
        }



        #region Процесс конвертации

        CDIparam ConvertParam;
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
            if (!IsSourceChosen)
                return;

            int[] CheckedTF = ConvertCheckedTF();
            if (CheckedTF.Length == 0)
                return;

            ConvertGoButton.Visibility = Visibility.Collapsed;
            ProcessPanel.Visibility = Visibility.Visible;
            TFpanel.IsEnabled = false;

            ProgressMain.Value = 0;
            ProgressSub.Value = 0;

            ConvertParam = new CDIparam()
            {
                Id = SourceUnit.Id,
                Symbol = SourceUnit.Symbol,
                NolCount = SourceUnit.NolCount,
                ConvertedIds = new int[CheckedTF.Length],
                Progress = new Progress<decimal>(v =>
                {
                    ProgressMain.Value = ConvertParam.ProgressMainValue;
                    ProgressSub.Value = (double)v;

                    // Снятие галочки с очередного сконвертированного таймфрейма
                    if (v == 100)
                        (FindName("CheckTF" + ConvertParam.TimeFrame) as CheckBox).IsChecked = false;
                })
            };

            await Task.Run(() => ConvertProcess(ConvertParam, CheckedTF));

            new Candle();
            Instrument.DataCountPlus(SourceUnit.InstrumentId, CheckedTF.Length);

            ConvertGoButton.Visibility = Visibility.Visible;
            ProcessPanel.Visibility = Visibility.Collapsed;
            TFpanel.IsEnabled = true;
            ResultListCreate();

            AutoProgon.PatternSearchSetup();
        }
        /// <summary>
        /// Процесс концертации в фоновом режиме
        /// </summary>
        void ConvertProcess(CDIparam PARAM, int[] CheckedTF)
        {
            var CDI = Candle.Unit(PARAM.Id);
            PARAM.Bar = new ProBar((CheckedTF.Length + 1) * CDI.RowsCount);

            // Загрузка из базы исходного минутного таймфрейма
            string sql = $"SELECT*FROM`{CDI.Table}`";
            var SourceData = mysql.CandlesDataCache(sql, PARAM);
            if (!PARAM.IsProcess)
                return;

            for (int i = 0; i < CheckedTF.Length; i++)
            {
                PARAM.TimeFrame = CheckedTF[i];
                PARAM.TfNum = i;    // Счётчик для Main-прогресс-бара
                ConvertProcessTF(PARAM, SourceData);

                if (!PARAM.IsProcess)
                    return;
            }

            PARAM.ProgressMainValue = 100;
            PARAM.Progress.Report(100);
        }
        /// <summary>
        /// Процесс конвертации в выбранные таймфреймы
        /// </summary>
        void ConvertProcessTF(CDIparam PARAM, List<CandleUnit> SourceData)
        {
            var SubBar = new ProBar(SourceData.Count);
            PARAM.Progress.Report(0);

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
            int MainCount = (PARAM.TfNum + 1) * SourceData.Count;
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

                if (!SubBar.Val(i, PARAM.Progress))
                    continue;

                PARAM.Bar.isUpd(MainCount + i);
                PARAM.ProgressMainValue = (double)PARAM.Bar.Value;
            }

            PARAM.Progress.Report(100);
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
            ResultListBox.ItemsSource = IsSourceChosen ? Candle.ListOnIID(SourceUnit.InstrumentId, false) : null;
        }

        /// <summary>
        /// Показ графика результата конвертации
        /// </summary>
        void ConverterResultChanged(object sender, SelectionChangedEventArgs e)
        {
            if (global.IsAutoProgon)
                return;

            var item = (sender as ListBox).SelectedItem as CDIunit;
            new AdvChart(ChartPanel, item);
        }

        /// <summary>
        /// Удаление результата конвертации
        /// </summary>
        void ConvertedX(object sender, MouseButtonEventArgs e) => Candle.UnitDel((sender as Label).TabIndex);
    }
}
