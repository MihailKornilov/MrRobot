using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Pattern.xaml
    /// </summary>
    public partial class Pattern : UserControl
    {
        public Pattern()
        {
            InitializeComponent();
            PatternInit();
        }

        public void PatternInit()
        {
            if(global.IsInited(3))
                return;

            SourceListBox.ItemsSource = Candle.ListAll();
            SourceListBox.SelectedIndex = position.Val("3_ChartListBox_SelectedIndex", 0);
            LengthSlider.Value = position.Val("3_CandlesCountForSearch", 1);
            PrecisionPercentSlider.Value = position.Val("3_ScatterPercent", 100);
            FoundRepeatMin.Text = position.Val("3_FoundRepeatMin", "0");

            global.Inited(3);
            //SearchResultCheck();
        }


        /// <summary>
        /// Выбор нового графика
        /// </summary>
        void SourceListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
            if(box == null)
                return;

            var item = box.SelectedItem as CDIunit;
            if(item == null)
                return;


            // Прокручивание списка, пока не появится в представлении
            SourceListBox.ScrollIntoView(item);

            position.Set("3_ChartListBox_SelectedIndex", box.SelectedIndex);

            DBcandlesCount.Text = format.Num(item.RowsCount);
            DBtimeframe.Text = item.TF;
            DBdateBegin.Text = item.DateBegin;
            DBdateEnd.Text = item.DateEnd;

            // Вывод графика
            PatternChartHead.Update(item);
            if (global.IsAutoProgon)
            {
                SearchBrowser.Address = new Chart().PageHtml;
                return;
            }
            SearchBrowser.Address = new Chart("Pattern", item.Table).PageHtml;
            SearchResultCheck();
        }


        /// <summary>
        /// Изменение параметра Длина паттерна (сохранение позиции)
        /// </summary>
        void LengthSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string v = LengthSlider.Value.ToString();
            PatternLengthBox.Text = v;
            position.Set("3_CandlesCountForSearch", v);
            SearchResultCheck((sender as Slider).IsFocused);
        }

        /// <summary>
        /// Изменение параметра Точность в % (сохранение позиции)
        /// </summary>
        void PrecisionPercentChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string v = PrecisionPercentSlider.Value.ToString();
            PrecisionPercentBox.Text = v;
            position.Set("3_ScatterPercent", v);
            SearchResultCheck((sender as Slider).IsFocused);
        }

        /// <summary>
        /// Изменение параметра "Исключать менее N нахождений"
        /// </summary>
        void FoundRepeatMinChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_FoundRepeatMin", FoundRepeatMin.Text);
            SearchResultCheck((sender as TextBox).IsFocused);
        }

        void SearchResultCheck(bool isCheck = true)
        {
            if (!global.IsInited(3))
                return;
            if (!isCheck)
                return;

            var CDI = SourceListBox.SelectedItem as CDIunit;
            var param = new PatternSearchParam
            {
                CdiId = CDI.Id,
                PatternLength = (int)LengthSlider.Value,
                PrecisionPercent = (int)PrecisionPercentSlider.Value,
                FoundRepeatMin = Convert.ToInt32(FoundRepeatMin.Text)
            };

            PatternSearchExist(param);
        }




        #region SEARCH PROCESS

        PatternSearchParam SPARAM;
        /// <summary>
        /// Поиск паттернов в выбранном графике
        /// </summary>
        async void PatternSearchGo(object sender, RoutedEventArgs e)
        {
            var CDI = SourceListBox.SelectedItem as CDIunit;
            SPARAM = new PatternSearchParam()
            {
                IsProcess = true,

                CdiId = CDI.Id,
                PatternLength = (int)LengthSlider.Value,
                PrecisionPercent = (int)PrecisionPercentSlider.Value,
                FoundRepeatMin = Convert.ToInt16(FoundRepeatMin.Text),

                PBar = new Progress<decimal>(v => {
                    SearchProgress.Value = (double)v;
                    ProgressPrc.Content = v + "%";
                    ProсessInfo.Text = SPARAM.ProсessInfo;
                })
            };

            if (PatternSearchExist(SPARAM))
                return;

            await Task.Run(() => SearchProcess());

            if (!PatternFoundBaseInsert())
                return;

            global.MW.Pattern.PatternArchive.SearchList();
            if (!AutoProgon.PatternSearch())
                PatternSearchExist(SPARAM);
        }
        /// <summary>
        /// Поиск свечных паттернов без разброса в процентах
        /// </summary>
        void SearchProcess()
        {
            var dur = new Dur();
            var CDI = Candle.Unit(SPARAM.CdiId);

            string sql = $"SELECT COUNT(*)FROM`{CDI.Table}`";
            int count = mysql.Count(sql);

            sql = $"SELECT*FROM`{CDI.Table}`";
            var MASS = mysql.PatternSearchMass(sql, SPARAM, count);

            int CountSearch = MASS.Count - SPARAM.PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var FNDass = new Dictionary<string, int>();

            // Предварительный подсчёт общего количества итераций
            long Iterations = 0;
            for (int i = 1; i < CountSearch + 1; i++)
                Iterations += i;

            var bar = new ProBar(Iterations, 1000);
            SPARAM.PBar.Report(0);

            for (int i = 0; i < CountSearch; i++)
            {
                if (!SPARAM.IsProcess)
                    return;

                // Установка значения для Прогресс-бара
                if (bar.isUpd(SPARAM.Iterations))
                {
                    SPARAM.ProсessInfo = $"Прошло времени: {bar.TimePass}    Осталось: {bar.TimeLeft}";
                    int c = SPARAM.FoundList.Count;
                    if (c > 0)
                        SPARAM.ProсessInfo += $"\nНайдено {c} паттерн{format.End(c, "", "а", "ов")}";
                    SPARAM.PBar.Report(bar.Value);
                }

                for (int n = i + SPARAM.PatternLength; n < CountSearch + SPARAM.PatternLength; n++)
                {
                    SPARAM.Iterations++;

                    if (!MASS[i].Compare(MASS[n]))
                        continue;

                    string key = MASS[i].StructDB;
                    int UnixN = MASS[n].CandleList[0].Unix;

                    if (FNDass.ContainsKey(key))
                    {
                        int index = FNDass[key];
                        var list = SPARAM.FoundList[index].UnixList;
                        if (!list.Contains(UnixN))
                             list.Add(UnixN);
                        continue;
                    }

                    MASS[i].UnixList = new List<int> { MASS[i].CandleList[0].Unix, UnixN };
                    SPARAM.FoundList.Add(MASS[i]);

                    int c = SPARAM.FoundList.Count - 1;
                    FNDass.Add(key, c);
                }
            }

            SPARAM.IsProcess = false;
            SPARAM.PBar.Report(100);
            SPARAM.Duration = dur.Minutes();
        }
        /// <summary>
        /// Проверка поиска: если был, то берётся из базы
        /// </summary>
        public bool PatternSearchExist(PatternSearchParam param)
        {
            string sql = "SELECT*" +
                         "FROM`_pattern_search`" +
                        $"WHERE`cdiId`={param.CdiId}" +
                         $" AND`patternLength`={param.PatternLength}" +
                         $" AND`scatterPercent`={param.PrecisionPercent} " +
                        $"LIMIT 1";
            var row = mysql.QueryOne(sql);

            if (row.Count == 0)
            {
                SearchStatistic(param);
                return false;
            }

            param.Iterations = Convert.ToInt64(row["iterations"]);
            param.Duration = row["duration"];

            if (row["foundCount"] == "0")
            {
                SearchStatistic(param);
                AutoProgon.PatternSearch();
                return true;
            }

            int SearchId = Convert.ToInt32(row["id"]);

            param.FoundList = Patterns.List(SearchId);
            FoundListBox.ItemsSource = param.FoundList;
            FoundListBox.SelectedIndex = Patterns.Index(SearchId, param.FoundId);
            var item = FoundListBox.SelectedItem;
            FoundListBox.ScrollIntoView(item);

            SearchStatistic(param);
            AutoProgon.PatternSearch();
            return true;
        }
        /// <summary>
        /// Показ краткой статистики найденного паттерна
        /// </summary>
        public void SearchStatistic(PatternSearchParam param)
        {
            bool isSearchNew = param.Duration == null;
            SourceListBox.IsEnabled   = !param.IsProcess;
            SetupPanel.IsEnabled      = !param.IsProcess;
            SearchPanel.Visibility    = isSearchNew ? Visibility.Visible : Visibility.Collapsed;
            SearchGoButton.Visibility = param.IsProcess ? Visibility.Collapsed : Visibility.Visible;
            ProgressPanel.Visibility  = param.IsProcess ? Visibility.Visible : Visibility.Collapsed;
            ResultPanel.Visibility    = isSearchNew ? Visibility.Collapsed : Visibility.Visible;

            Visibility foundVis = param.FoundCount == 0 ? Visibility.Hidden : Visibility.Visible;
            FoundPanel.Visibility = foundVis;
            FoundBrowserPanel.Visibility = foundVis;
            FoundButtonsPanel.Visibility = foundVis;

            if (param.FoundCount == 0)
                FoundListBox.ItemsSource = null;

            if (isSearchNew)
                return;

            CandlesDuplicate.Text = format.Num(param.FoundCount);
            IterationsCount.Text = format.Num(param.Iterations);
            IterationsTime.Text = param.Duration;
        }
        /// <summary>
        /// Нажатие на кнопку `Отмена`
        /// </summary>
        void SearchCancel(object sender, RoutedEventArgs e)
        {
            SPARAM.IsProcess = false;
        }

        /// <summary>
        /// Подготовка найденных паттернов перед внесением в базу
        /// </summary>
        bool PattentFoundPrepare()
        {
            // Поиск был отменён
            if (SPARAM.Duration == null)
            {
                SPARAM.FoundList.Clear();
                SearchStatistic(SPARAM);
                return false;
            }

            if (SPARAM.FoundCount == 0)
                return true;

            var FoundListSorted = SPARAM.FoundList.OrderByDescending(x => x.Repeat).ToList();
            SPARAM.FoundList.Clear();
            foreach (var patt in FoundListSorted)
            {
                if (patt.Repeat < SPARAM.FoundRepeatMin)
                    break;
                patt.UnixList.Sort();
                SPARAM.FoundList.Add(patt);
            }

            return true;
        }
        /// <summary>
        /// Внесение найденных паттернов в базу
        /// </summary>
        bool PatternFoundBaseInsert()
        {
            if (!PattentFoundPrepare())
                return false;

            string sql = "INSERT INTO`_pattern_search`(" +
                            "`cdiId`," +

                            "`patternLength`," +
                            "`scatterPercent`," +
                            "`foundRepeatMin`," +

                            "`iterations`," +
                            "`foundCount`," +
                            "`duration`" +
                         ")VALUES(" +
                           $"{SPARAM.CdiId}," +

                           $"{SPARAM.PatternLength}," +
                           $"{SPARAM.PrecisionPercent}," +
                           $"{SPARAM.FoundRepeatMin}," +

                           $"{SPARAM.Iterations}," +
                           $"{SPARAM.FoundCount}," +
                           $"'{SPARAM.Duration}'" +
                         ")";
            int SearchId = mysql.Query(sql);

            if (SPARAM.FoundCount == 0)
                return true;

            var insert = new List<string>();
            for (int i = 0; i < SPARAM.FoundCount; i++)
            {
                insert.Add(SPARAM.FoundList[i].Insert(SearchId));

                if (insert.Count < 500 && i < SPARAM.FoundCount - 1)
                    continue;

                sql = "INSERT INTO`_pattern_found`(" +
                        "`searchId`," +
                        "`size`," +
                        "`structure`," +
                        "`repeatCount`," +
                        "`unixList`" +
                        ")VALUES" + string.Join(",", insert.ToArray());
                mysql.Query(sql);

                insert.Clear();
            }

            new Patterns();

            return true;
        }

        #endregion


        /// <summary>
        /// Визуальное отображение найденного паттерна
        /// </summary>
        void FoundListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundListBox.SelectedIndex == -1)
                return;

            var found = FoundListBox.SelectedItem as PatternUnit;
            var chart = new Chart("Pattern");
            chart.PageName = "PatternFound";
            chart.PatternSource(found);
            FoundBrowser.Address = chart.PageHtml;

            PatternFoundStep();
        }

        /// <summary>
        /// Нажатие на кнопку для отображения очередного найденного паттерна
        /// </summary>
        void PatternFoundBack(object sender, RoutedEventArgs e) => PatternFoundStep(-1);
        void PatternFoundNext(object sender, RoutedEventArgs e) => PatternFoundStep(1);
        void PatternFoundStep(int step = 0)
        {
            int index = step == 0 ? 1 : Convert.ToInt32(FoundStep.Text);
            var found = FoundListBox.SelectedItem as PatternUnit;

            if (step < 0 && index <= 1
             || step > 0 && index >= found.Repeat)
                return;

            index += step;
            FoundStep.Text = index.ToString();
            ButtonFoundBack.IsEnabled = index > 1;
            ButtonFoundNext.IsEnabled = index < found.Repeat;

            var visual = new Chart("Pattern", Candle.Unit(found.CdiId).Table);
            visual.PageName = "PatternVisual";
            visual.PatternVisual(found, index-1);
            SearchBrowser.Address = visual.PageHtml;
        }


        /// <summary>
        /// Нажатие на кнопку для показа истории поисков
        /// </summary>
        void ArchiveGo(object sender, RoutedEventArgs e) => ArchiveGo();
        public void ArchiveGo(bool fromMM = false)
        {
            if (position.MainMenu() != 3)
                return;

            bool isSearch = PatternSearchGrid.Visibility == Visibility.Visible;

            if (fromMM && isSearch)
                return;

            HeadArchive.Text = "Поиск паттернов" + (isSearch ? ": история" : "");
            ButtonArchive.Content = isSearch ? "<<< назад" : "История поисков";
            PatternSearchGrid.Visibility = isSearch ? Visibility.Collapsed : Visibility.Visible;
            PatternArchiveGrid.Visibility = !isSearch ? Visibility.Collapsed : Visibility.Visible;

            global.MW.Pattern.PatternArchive.PatternArchiveInit();
        }

        /// <summary>
        /// Формирование кода для найденного паттерна
        /// </summary>
        void FoundCodeGet(object sender, MouseButtonEventArgs e) => new PatternCode().Show();
        void FoundInfoShow(object sender, MouseButtonEventArgs e) => new PatternInfo().Show();
    }

    /// <summary>
    /// Настройки и результат поиска паттернов
    /// </summary>
    public class PatternSearchParam
    {
        public int CdiId { get; set; }          // ID свечных данных
        public int PatternLength { get; set; }  // Длина паттерна - количество свечей в паттерне
        public int PrecisionPercent { get; set; }// Точность в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений



        public bool IsProcess { get; set; }     // Происходит процесс поиска паттернов
        public IProgress<decimal> PBar { get; set; }// Прогресс-бар
        public string ProсessInfo { get; set; } // Информация о процессе поиска



        public int FoundCount { get { return FoundList.Count; } }// Количество найденных паттернов
        public List<PatternUnit> FoundList { get; set; } = new List<PatternUnit>();  // Список с данными найденных паттернов
        public long Iterations { get; set; }    // Количество итераций
        public string Duration { get; set; }    // Время выполнения



        public int FoundId { get; set; }        // ID паттерна при выборе из прибыльных
        public int FoundIndex { get; set; }     // Индекс паттерна при показе из прибыльных
    }
}
