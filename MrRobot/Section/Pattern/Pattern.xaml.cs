using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
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
            CDIpanel.Page(3).TBLink = SelectLink.TBLink;
            CDIpanel.Page(3).OutMethod += SourceChanged;
            MainMenu.Changed += () => ArchiveGo(true);
            MainMenu.Changed += FoundLine;
        }

        public void PatternInit()
        {
            if (global.IsInited(3))
                return;

            LengthSlider.Value = position.Val("3_CandlesCountForSearch", 1);
            PrecisionPercentSlider.Value = position.Val("3_ScatterPercent", 100);
            FoundRepeatMin.Text = position.Val("3_FoundRepeatMin", "0");
            FoundButtonBack.Content = "<<<";
            FoundSlider.ValueChanged += (s, e) => PatternFoundStep();

            SourceChanged();

            global.Inited(3);
        }


        bool IsSrcChosen => SrcId > 0;  // Свечные данные выбраны
        int SrcId => CDIpanel.CdiId;    // ID свечных данных
        CDIunit SrcUnit => Candle.Unit(SrcId);  // Единица свечных данных


        /// <summary>
        /// Выбор нового графика
        /// </summary>
        public void SourceChanged()
        {
            if (position.MainMenu() != 3)
                return;
            if (!IsSrcChosen)
                return;

            DBcandlesCount.Text = format.Num(SrcUnit.RowsCount);
            DBtimeframe.Text = SrcUnit.TF;
            DBdateBegin.Text = SrcUnit.DateBegin;
            DBdateEnd.Text = SrcUnit.DateEnd;

            // Вывод графика
            if (global.IsAutoProgon)
            {
                EChart.Empty();
                return;
            }

            if(!SearchResultCheck())
                EChart.CDI("Pattern", SrcUnit);
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

        bool SearchResultCheck(bool isCheck = true)
        {
            if (!global.IsInited(3))
                return false;
            if (!isCheck)
                return false;

            var param = new PatternSearchParam
            {
                CdiId = SrcId,
                PatternLength = (int)LengthSlider.Value,
                PrecisionPercent = (int)PrecisionPercentSlider.Value,
                FoundRepeatMin = Convert.ToInt32(FoundRepeatMin.Text)
            };

            return PatternSearchExist(param);
        }




        #region SEARCH PROCESS

        PatternSearchParam SPARAM;
        /// <summary>
        /// Поиск паттернов в выбранном графике
        /// </summary>
        async void PatternSearchGo(object sender, RoutedEventArgs e)
        {
            SPARAM = new PatternSearchParam()
            {
                IsProcess = true,

                CdiId = SrcId,
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

            int PatternLength = SPARAM.PatternLength;
            int CountSearch = MASS.Count - PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var FNDass = new Dictionary<string, int>();

            // Предварительный подсчёт общего количества итераций
            long Iterations = 0;
            for (int i = 1; i < CountSearch + 1; i++)
                Iterations += i;

            var bar = new ProBar(Iterations, 1000);
            SPARAM.PBar.Report(0);

            Iterations = 0;
            int CountSearchN = CountSearch + PatternLength;
            var FoundList = SPARAM.FoundList;
            var UnixNlist = new List<int>();

            for (int i = 0; i < CountSearch; i++)
            {
                // Установка значения для Прогресс-бара
                if (bar.isUpd(Iterations))
                {
                    if (!SPARAM.IsProcess)
                        return;

                    SPARAM.ProсessInfo = $"Прошло времени: {bar.TimePass}    Осталось: {bar.TimeLeft}";
                    int c = FoundList.Count;
                    if (c > 0)
                        SPARAM.ProсessInfo += $"\nНайден{format.End(c, "", "о")} {c} паттерн{format.End(c, "", "а", "ов")}";
                    SPARAM.PBar.Report(bar.Value);
                }

                var MASSi = MASS[i];
                int nBegin = i + PatternLength;
                for (int n = nBegin; n < CountSearchN; n++)
                {
                    var MASSnCL = MASS[n].CandleList;
                    if (!MASSi.Compare(MASSnCL))
                        continue;

                    int UnixI = MASSi.CandleList[0].Unix;
                    int UnixN = MASSnCL[0].Unix;

                    // Если паттерн был найден ранее, пропуск
                    if (UnixNlist.Contains(UnixI))
                        continue;
                    if (UnixNlist.Contains(UnixN))
                        continue;

                    UnixNlist.Add(UnixN);

                    string key = MASSi.StructDB;
                    if (FNDass.ContainsKey(key))
                    {
                        int index = FNDass[key];
                        var list = FoundList[index].UnixList;
                        if (!list.Contains(UnixN))
                             list.Add(UnixN);
                        continue;
                    }

                    MASSi.UnixList = new List<int> { UnixI, UnixN };
                    FoundList.Add(MASSi);

                    int c = FoundList.Count - 1;
                    FNDass.Add(key, c);
                }
                Iterations += CountSearchN - nBegin;
            }

            SPARAM.Iterations = Iterations;
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

            FoundLine();

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
            SPARAM.SearchId = mysql.Query(sql);

            if (SPARAM.FoundCount == 0)
            {
                new Patterns();
                return true;
            }

            var insert = new List<string>();
            for (int i = 0; i < SPARAM.FoundCount; i++)
            {
                insert.Add(SPARAM.FoundList[i].Insert(SPARAM.SearchId));

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
        /// Расстановка временных линий для визуального отображенмя найденных паттернов
        /// </summary>
        public void FoundLine()
        {
            if (position.MainMenu() != 3)
                return;

            FoundLinePanel.Children.Clear();

            if (FoundListBox.Items.Count == 0)
                return;


            int index = Convert.ToInt32(FoundStep.Text) - 1;
            var found = FoundListBox.SelectedItem as PatternUnit;
            var CDI = Candle.Unit(found.CdiId);
            int Width = (int)FoundLinePanel.ActualWidth;
            int Height = (int)FoundLinePanel.ActualHeight;

            for(int i = 0; i < found.UnixList.Count; i++)
            {
                int unix = found.UnixList[i];
                double dist = (unix - CDI.UnixBegin) / 60 / CDI.TimeFrame;
                double X = dist / CDI.RowsCount * Width;

                var line = new Line()
                {
                    Tag = i+1,
                    X1 = X,
                    Y1 = 0,
                    X2 = X,
                    Y2 = Height
                };

                if(i == index)
                {
                    line.Stroke = format.RGB("#FFFFC0");
                    line.StrokeThickness = 2;
                }

                // Нажатие на временную линию
                line.MouseLeftButtonDown += (s, e) => FoundSlider.Value = (int)(s as Line).Tag;

                FoundLinePanel.Children.Add(line);
            }
        }

        /// <summary>
        /// Визуальное отображение найденного паттерна
        /// </summary>
        void FoundListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundListBox.SelectedIndex == -1)
                return;

            var found = FoundListBox.SelectedItem as PatternUnit;
            FoundChart.PatternSource(found);

            FoundSlider.Maximum = found.UnixList.Count;

            if (FoundSlider.Value == 1)
                PatternFoundStep();

            FoundSlider.Value = 1;
        }

        /// <summary>
        /// Нажатие на кнопку для отображения очередного найденного паттерна
        /// </summary>
        void PatternFoundBack(object sender, RoutedEventArgs e) => FoundSlider.Value -= 1;
        void PatternFoundNext(object sender, RoutedEventArgs e) => FoundSlider.Value += 1;
        void PatternFoundStep()
        {
            int index = (int)FoundSlider.Value;
            if (index == 0)
                return;

            var found = FoundListBox.SelectedItem as PatternUnit;

            FoundStep.Text = index.ToString();
            FoundButtonBack.IsEnabled = index > 1;
            FoundButtonNext.IsEnabled = index < found.Repeat;

            EChart.PatternVisual(found, index-1);

            FoundLine();
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
            PatternSearchGrid.Visibility  = global.Vis(!isSearch);
            PatternArchiveGrid.Visibility = global.Vis(isSearch);
        }

        /// <summary>
        /// Формирование кода для найденного паттерна
        /// </summary>
        void FoundCodeGet(object sender, MouseButtonEventArgs e) => new PatternCode().Show();
        void FoundInfoShow(object sender, MouseButtonEventArgs e) => new PatternInfo().Show();

        /// <summary>
        /// Удаление поиска
        /// </summary>
        void SearchX(object sender, MouseButtonEventArgs e)
        {
            int PatternLength = (int)LengthSlider.Value;
            int PrecisionPercent = (int)PrecisionPercentSlider.Value;
            int SearchId = Patterns.SUnitIdOnParam(SrcId, PatternLength, PrecisionPercent);

            Patterns.SUnitDel(SearchId);
            SourceChanged();
            global.MW.Pattern.PatternArchive.SearchList();
        }
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



        public int SearchId { get; set; }       // ID произведённого поиска
        public int FoundCount { get { return FoundList.Count; } }// Количество найденных паттернов
        public List<PatternUnit> FoundList { get; set; } = new List<PatternUnit>();  // Список с данными найденных паттернов
        public long Iterations { get; set; }    // Количество итераций
        public string Duration { get; set; }    // Время выполнения



        public int FoundId { get; set; }        // ID паттерна при выборе из прибыльных
        public int FoundIndex { get; set; }     // Индекс паттерна при показе из прибыльных
    }
}
