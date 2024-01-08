using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section.Pattern
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
            PatternCandlesCount.Text = position.Val("3_CandlesCountForSearch", "1");
            PatternScatterPrc.Text = position.Val("3_ScatterPercent", "0");
            FoundRepeatMin.Text = position.Val("3_FoundRepeatMin", "0");
            CandleNolAvoid.IsChecked = position.Val("3_CandleNolAvoid", false);

            global.Inited(3);
        }


        /// <summary>
        /// Выбор нового графика
        /// </summary>
        private void SourceListChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = sender as ListBox;
            if(box == null)
                return;

            var item = box.SelectedItem as CandleDataInfoUnit;
            if(item == null)
                return;

            // Прокручивание списка, пока не появится в представлении
            SourceListBox.ScrollIntoView(item);

            position.Set("3_ChartListBox_SelectedIndex", box.SelectedIndex);

            DBcandlesCount.Text = format.Num(item.RowsCount);
            DBtimeframe.Text = item.TF;
            DBdateBegin.Text = item.DateBegin;
            DBdateEnd.Text = item.DateEnd;

            PatternSearchClear();
            SearchBrowserShow(item);
        }


        /// <summary>
        /// Показ графика
        /// </summary>
        private void SearchBrowserShow(CandleDataInfoUnit item)
        {
            PatternChartHead.Update(item);

            if (global.IsAutoProgon)
                return;

            SearchBrowser.Address = new Chart("Pattern", item.Table).PageHtml;
        }

        /// <summary>
        /// Изменение параметра Количество свечей (сохранение позиции)
        /// </summary>
        private void CandlesCountChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_CandlesCountForSearch", PatternCandlesCount.Text);
        }

        /// <summary>
        /// Изменение параметра Разброс в процентах (сохранение позиции)
        /// </summary>
        private void ScatterPrcChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_ScatterPercent", PatternScatterPrc.Text);
        }

        /// <summary>
        /// Изменение параметра "Исключать менее N нахождений"
        /// </summary>
        private void FoundRepeatMinChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_FoundRepeatMin", FoundRepeatMin.Text);
        }

        /// <summary>
        /// Изменение параметра Исключать нулевые свечи (сохранение позиции)
        /// </summary>
        private void CandleNolAvoidClick(object sender, RoutedEventArgs e)
        {
            position.Set("3_CandleNolAvoid", (bool)CandleNolAvoid.IsChecked);
        }





        #region SEARCH PROCESS

        /// <summary>
        /// Очистка результатов поиска паттернов
        /// </summary>
        private void PatternSearchClear()
        {
            SetupPanel.Visibility = Visibility.Visible;
            ResultPanel.Visibility = Visibility.Collapsed;
            PatternFoundListPanel.Visibility = Visibility.Hidden;
            FoundBrowserPanel.Visibility = Visibility.Hidden;
            FoundButtonsPanel.Visibility = Visibility.Hidden;
        }
        /// <summary>
        /// Поиск паттернов в выбранном графике
        /// </summary>
        private async void PatternSearchGo(object sender, RoutedEventArgs e)
        {
            var CDI = SourceListBox.SelectedItem as CandleDataInfoUnit;
            var param = new PatternSearchParam()
            {
                PatternLength = Convert.ToInt16(PatternCandlesCount.Text),
                ScatterPercent = Convert.ToInt16(PatternScatterPrc.Text),
                FoundRepeatMin = Convert.ToInt16(FoundRepeatMin.Text),
                CandleNolAvoid = (bool)CandleNolAvoid.IsChecked,

                CdiId = CDI.Id,
                Table = CDI.Table,
                NolCount = CDI.NolCount,
                Exp = format.Exp(CDI.NolCount),
                TimeFrame = CDI.TimeFrame,

                FoundList = new List<PatternUnit>()
            };

            if (PatternSearchExist(param))
                return;

            PatternSearchStat();
            PatternProgressBar.Value = 0;
            var progress = new Progress<int>(v => {
                PatternProgressBar.Value = v;

                //if (param.FoundList.Count == 0)
                //    return;

                //int found = 0;
                //foreach (PatternFoundItem item in param.FoundList)
                //    if (item.Repeat >= param.FoundRepeatMin)
                //        found++;

                //if (found == 0)
                //    return;

                //ProcessFoundCount.Text = "Найдено совпадений: " + found;
            });
            var dur = new Dur();
            await Task.Run(() => PatternSearchProcess(param, progress));
            param.Duration = dur.Minutes();


            PattentSearchResult(param);
            PatternSearchStat(param);


            if(!PatternFoundBaseInsert(param))
                return;

            PatternListBox.ItemsSource = param.FoundList;
            PatternListBox.SelectedIndex = 0;
            PatternListBox.ScrollIntoView(PatternListBox.SelectedItem);
            PatternFoundListPanel.Visibility = Visibility.Visible;
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
                         $" AND`scatterPercent`={param.ScatterPercent}" +
                         $" AND`foundRepeatMin`<={param.FoundRepeatMin} " +
  (param.CandleNolAvoid ? " AND`candleNolAvoid`" : "") +
                         $"LIMIT 1";
            var row = mysql.QueryOne(sql);

            if (row.Count == 0)
                return false;

            param.FoundCount = Convert.ToInt32(row["foundCount"]);
            param.Iterations = Convert.ToUInt64(row["iterations"]);
            param.Duration = row["duration"];
            PatternSearchStat(param);

            if (param.FoundCount == 0)
            {
                AutoProgon.PatternSearch();
                return true;
            }

            int SearchId = Convert.ToInt32(row["id"]);
            sql = "SELECT*" +
                  "FROM`_pattern_found`" +
                 $"WHERE`searchId`={SearchId} " +
                  "ORDER BY`repeat`DESC";
            var Spisok = mysql.QueryList(sql);

            var FoundList = new List<PatternUnit>();
            var index = 0;
            for (int i = 0; i < Spisok.Count; i++)
            {
                var item = Spisok[i] as Dictionary<string, string>;

                if (param.FoundId == Convert.ToInt32(item["id"]))
                    index = i;

                string[] UnixListString = item["unixList"].Split(',');
                var UnixList = new List<int>();
                for (int n = 0; n < UnixListString.Length; n++)
                    UnixList.Add(Convert.ToInt32(UnixListString[n]));

                FoundList.Add(new PatternUnit
                {
                    Num = i + 1,
                    SearchId = Convert.ToInt32(item["searchId"]),
                    CdiId = param.CdiId,
                    Candle = item["candle"].Replace(';', '\n'),
                    Repeat = Convert.ToInt32(item["repeat"]),
                    Unix = UnixList[0],
                    UnixList = UnixList,
                    Price = Convert.ToDouble(item["price"]),
                    TimeFrame = param.TimeFrame,
                    PatternLength = param.PatternLength,
                    NolCount = param.NolCount
                });
            }

            PatternListBox.ItemsSource = FoundList;
            PatternListBox.SelectedIndex = index;
            PatternFoundListPanel.Visibility = Visibility.Visible;
            FoundBrowserPanel.Visibility = Visibility.Visible;
            FoundButtonsPanel.Visibility = Visibility.Visible;

            AutoProgon.PatternSearch();

            return true;
        }
        /// <summary>
        /// Показ краткой статистики найденного паттерна
        /// </summary>
        private void PatternSearchStat(PatternSearchParam param = null)
        {
            bool isStat = param != null;
            SourceListBox.IsEnabled = isStat;
            SetupPanel.IsEnabled = isStat;
            SearchGoButton.Visibility = isStat ? Visibility.Visible : Visibility.Collapsed;
            PatternProgressBar.Visibility = !isStat ? Visibility.Visible : Visibility.Collapsed;
            SetupPanel.Visibility = !isStat ? Visibility.Visible : Visibility.Collapsed;

            if (!isStat)
                return;

            CandlesDuplicate.Text = format.Num(param.FoundCount);
            IterationsCount.Text = format.Num(param.Iterations);
            IterationsTime.Text = param.Duration;
            ResultPanel.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Формирование паттерна свечей в цифровом формате для отображения результата в приложении
        /// </summary>
        private string CandlesString(PatternSearchParam PARAM, int i, ref int[] WickTop, ref int[] Body, ref int[] WickBtm)
        {
            string[] arr = new string[PARAM.PatternLength];

            for (int k = 0; k < PARAM.PatternLength; k++)
                arr[k] = WickTop[i + k] + " " +
                         Body[i + k] + " " +
                         WickBtm[i + k];

            return string.Join("\n", arr);
        }
        /// <summary>
        /// Проверка на наличие нулевой свечи
        /// </summary>
        private bool CandleNolAvoidCheck(PatternSearchParam PARAM, int i, ref int[] WickTop, ref int[] Body, ref int[] WickBtm)
        {
            if (!PARAM.CandleNolAvoid)
                return false;

            for (int k = 0; k < PARAM.PatternLength; k++)
                if (WickTop[i + k] == 0 && Body[i + k] == 0 && WickBtm[i + k] == 0)
                    return true;

            return false;
        }
        /// <summary>
        /// Поиск свечных паттернов без разброса в процентах
        /// </summary>
        /// PatternLength - длина паттерна (количество свечей в паттерне)
        /// NolAvoid - изберать нулевые свечи
        /// NolCount - количество знаков после запятой в инструменте
        private bool PatternSearchProcess_0percent(PatternSearchParam PARAM, IProgress<int> Progress)
        {
            if (PARAM.ScatterPercent > 0)
                return false;

            // Получение общего количества свечей
            string sql = $"SELECT COUNT(*)FROM`{PARAM.Table}`";
            int CCount = mysql.Count(sql);

            // Массивы дат, цен и размеров свечей всего списка (для оптимизации)
            int[] Unix = new int[CCount];           // Дата и время в формате UNIX
            double[] Price = new double[CCount];    // Цены
            int[] WickTop = new int[CCount];  // Верхние хвосты свечей
            int[] Body = new int[CCount];     // Тела свечей
            int[] WickBtm = new int[CCount];  // Нижние хвосты свечей

            sql = $"SELECT`unix`,`high`,`open`,`close`,`low`FROM`{PARAM.Table}`";
            mysql.PatternSearchMass(sql, PARAM.Exp, Unix, Price, WickTop, Body, WickBtm);



            int CountSearch = CCount - PARAM.PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var bar = new ProBar(CountSearch);
            var FNDAss = new Dictionary<string, int>();

            for (int i = 0; i < CountSearch; i++)
            {
                // Установка значения для Прогресс-бара
                if (bar.isUpd(i))
                    Progress.Report(bar.Value);

                if (CandleNolAvoidCheck(PARAM, i, ref WickTop, ref Body, ref WickBtm))
                    continue;

                for (int n = i + PARAM.PatternLength; n < CountSearch + PARAM.PatternLength; n++)
                {
                    PARAM.Iterations++;

                    bool found = true;
                    for (int k = 0; k < PARAM.PatternLength; k++)
                    {
                        if (WickTop[i+k] != WickTop[n+k]) { found = false; break; }
                        if (Body[i+k] != Body[n+k]) { found = false; break; }
                        if (WickBtm[i+k] != WickBtm[n+k]) { found = false; break; }
                    }

                    if (!found)
                        continue;

                    string cndl = CandlesString(PARAM, i, ref WickTop, ref Body, ref WickBtm);

                    if (!FNDAss.ContainsKey(cndl))
                    {
                        PARAM.FoundList.Add(new PatternUnit
                        {
                            Candle = cndl,
                            Repeat = 2,
                            Unix = Unix[i],
                            UnixList = new List<int> { Unix[i], Unix[n] },
                            Price = Price[i]
                        });

                        int c = PARAM.FoundList.Count - 1;
                        FNDAss.Add(cndl, c);
                        break;
                    }

                    int index = FNDAss[cndl];
                    PARAM.FoundList[index].Repeat++;
                    PARAM.FoundList[index].UnixList.Add(Unix[n]);
                    break;
                }
            }

            return true;
        }
        /// <summary>
        /// Поиск свечных паттернов с разбросом в процентах
        /// </summary>
        /// PatternLength - длина паттерна (количество свечей в паттерне)
        /// NolAvoid - изберать нулевые свечи
        /// ScatterPercent - допустимый разброс сравниваемых свечей в процентах
        /// NolCount - количество знаков после запятой в инструменте
        private void PatternSearchProcess(PatternSearchParam PARAM, IProgress<int> Progress)
        {
            if (PatternSearchProcess_0percent(PARAM, Progress))
                return;

            string sql = $"SELECT * FROM `{PARAM.Table}`";
            List<object> CandlesList = mysql.QueryList(sql);

            // Массивы размеров свечей всего списка в формате double (для оптимизации)
            int CCount = CandlesList.Count;
            int[] Unix = new int[CCount];
            double[] Price = new double[CCount];
            double[] WickTop = new double[CCount];
            double[] WickTopMin = new double[CCount];
            double[] WickTopMax = new double[CCount];
            double[] Body = new double[CCount];
            double[] BodyMin = new double[CCount];
            double[] BodyMax = new double[CCount];
            double[] WickBtm = new double[CCount];
            double[] WickBtmMin = new double[CCount];
            double[] WickBtmMax = new double[CCount];
            for (int i = 0; i < CCount; i++)
            {
                var src = CandlesList[i] as Dictionary<string, string>;

                double open = Convert.ToDouble(src["open"]),
                       close = Convert.ToDouble(src["close"]),
                       high = Convert.ToDouble(src["high"]),
                       low = Convert.ToDouble(src["low"]),
                       body = Math.Round(close - open, PARAM.NolCount);

                Unix[i] = Convert.ToInt32(src["unix"]);
                Price[i] = Convert.ToDouble(src["close"]);

                double koef = (double)PARAM.ScatterPercent / 100 / 2;
                WickTop[i] = Math.Round(open > close ? high - open : high - close, PARAM.NolCount);
                double prc = WickTop[i] * koef;
                WickTopMin[i] = Math.Round(WickTop[i] - prc, PARAM.NolCount + 2);
                WickTopMax[i] = Math.Round(WickTop[i] + prc, PARAM.NolCount + 2);

                Body[i] = body;
                prc = Math.Abs(body) * koef;
                BodyMin[i] = Math.Round(Body[i] - prc, PARAM.NolCount + 2);
                BodyMax[i] = Math.Round(Body[i] + prc, PARAM.NolCount + 2);

                WickBtm[i] = Math.Round(open < close ? open - low : close - low, PARAM.NolCount);
                prc = WickBtm[i] * koef;
                WickBtmMin[i] = Math.Round(WickBtm[i] - prc, PARAM.NolCount + 2);
                WickBtmMax[i] = Math.Round(WickBtm[i] + prc, PARAM.NolCount + 2);
            }

            int CountSearch = CCount - PARAM.PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var bar = new ProBar(CountSearch);
            var FNDAss = new Dictionary<string, int>();

            for (int i = 0; i < CountSearch; i++)
            {
                if (bar.isUpd(i))
                    Progress.Report(bar.Value);

//                if (CandleNolAvoidCheck(PARAM, i, ref WickTop, ref Body, ref WickBtm))
                    continue;

                for (int n = i + PARAM.PatternLength; n < CountSearch + PARAM.PatternLength; n++)
                {
                    PARAM.Iterations++;

                    bool found = true;
                    for (int k = 0; k < PARAM.PatternLength; k++)
                    {
                        int src = i + k;    // Порядковый номер исходной свечи
                        int dst = n + k;    // Порядковый номер сравниваемой свечи
                        if (WickTop[src] < WickTopMin[dst]
                         || WickTop[src] > WickTopMax[dst]
                         || Body[src] < BodyMin[dst]
                         || Body[src] > BodyMax[dst]
                         || WickBtm[src] < WickBtmMin[dst]
                         || WickBtm[src] > WickBtmMax[dst])
                        {
                            found = false;
                            break;
                        }
                    }

                    if (!found)
                        continue;

                    string[] arr = new string[PARAM.PatternLength];
                    for (int k = 0; k < PARAM.PatternLength; k++)
                    {
                        string WTmin = (WickTopMin[i + k] * PARAM.Exp).ToString();
                        string WTmax = (WickTopMax[i + k] * PARAM.Exp).ToString();
                        string Bmin = (BodyMin[i + k] * PARAM.Exp).ToString();
                        string Bmax = (BodyMax[i + k] * PARAM.Exp).ToString();
                        string WBmin = (WickBtmMin[i + k] * PARAM.Exp).ToString();
                        string WBmax = (WickBtmMax[i + k] * PARAM.Exp).ToString();
                        arr[k] = WTmin + "_" + WTmax + " " +
                                 Bmin + "_" + Bmax + " " +
                                 WBmin + "_" + WBmax;
                    }
                    string key = string.Join("\n", arr);

                    if (!FNDAss.ContainsKey(key))
                    {
                        PARAM.FoundList.Add(new PatternUnit
                        {
                            Candle = "",//CandlesString(PARAM, i, ref WickTop, ref Body, ref WickBtm),
                            Key = key,
                            Repeat = 2,
                            Unix = Unix[i],
                            UnixList = new List<int> { Unix[i], Unix[n] },
                            Price = Price[i]
                        });

                        int c = PARAM.FoundList.Count - 1;
                        FNDAss.Add(key, c);

                        continue;
                    }

                    int index = FNDAss[key];
                    PARAM.FoundList[index].Repeat++;
                    PARAM.FoundList[index].UnixList.Add(Unix[n]);
                }
            }
        }
        /// <summary>
        /// Возврат результата после выполнения поиска паттернов
        /// </summary>
        private void PattentSearchResult(PatternSearchParam PARAM)
        {
            // Сортировка найденных паттернов по количеству по убыванию
            var FoundListTmp = from pf in PARAM.FoundList orderby pf.Repeat descending select pf;

            PARAM.FoundList = new List<PatternUnit>();
            foreach (var item in FoundListTmp)
            {
                if (item.Repeat < PARAM.FoundRepeatMin)
                    continue;

                item.Num = ++PARAM.FoundCount;
                item.PatternLength = PARAM.PatternLength;
                item.NolCount = PARAM.NolCount;
                item.TimeFrame = PARAM.TimeFrame;
                PARAM.FoundList.Add(item);
            }
        }
        /// <summary>
        /// Внесение найденных паттернов в базу
        /// </summary>
        private bool PatternFoundBaseInsert(PatternSearchParam param)
        {
            string sql = "INSERT INTO`_pattern_search`(" +
                            "`cdiId`," +

                            "`patternLength`," +
                            "`scatterPercent`," +
                            "`foundRepeatMin`," +
                            "`candleNolAvoid`," +

                            "`iterations`," +
                            "`foundCount`," +
                            "`duration`" +
                         ")VALUES(" +
                           $"{param.CdiId}," +

                           $"{param.PatternLength}," +
                           $"{param.ScatterPercent}," +
                           $"{param.FoundRepeatMin}," +
                           $"{(param.CandleNolAvoid ? 1 : 0)}," +

                           $"{param.Iterations}," +
                           $"{param.FoundCount}," +
                           $"'{param.Duration}'" +
                         ")";
            long SearchId = mysql.Query(sql);

            if (param.FoundCount == 0)
            {
                AutoProgon.PatternSearch();
                return false;
            }

            string[] insert = new string[param.FoundCount];
            for (int i = 0; i < param.FoundCount; i++)
            {
                var item = param.FoundList[i];
                insert[i] = "(" +
                                $"{SearchId}," +
                                $"'{item.Candle.Replace('\n', ';')}'," +
                                $"{item.Repeat}," +
                                $"{item.Price}," +
                                $"'{string.Join(",", item.UnixList.ToArray())}'" +
                            ")";
            }

            sql = "INSERT INTO `_pattern_found`(" +
                    "`searchId`," +
                    "`candle`," +
                    "`repeat`," + 
                    "`price`," + 
                    "`unixList`" + 
                  ")VALUES" + string.Join(",", insert.ToArray());
            mysql.Query(sql);

            global.MW.Pattern.PatternArchive.SearchList();

            return !AutoProgon.PatternSearch();
        }

        #endregion



        /// <summary>
        /// Визуальное отображение найденного паттерна
        /// </summary>
        private void PatternListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
            var PatternItem = box.SelectedItem as PatternUnit;
            if (PatternItem == null)
                return;

            PatternListBox.ScrollIntoView(PatternItem);

            FoundStep.Text = "1";
            ButtonFoundBack.IsEnabled = false;
            ButtonFoundNext.IsEnabled = true;

            Chart patternChart = new Chart("Pattern");
            patternChart.PageName = "PatternFound";
            patternChart.PatternFound(PatternItem);
            FoundBrowser.Address = patternChart.PageHtml;
            FoundBrowserPanel.Visibility = Visibility.Visible;

            var ChartItem = (CandleDataInfoUnit)SourceListBox.SelectedItem;

            Chart visual = new Chart("Pattern", ChartItem.Table);
            visual.PageName = "PatternChartVisualShow";
            visual.Title = "Pattern Found Visual";
            visual.PatternChartVisualShow(PatternItem);
            SearchBrowser.Address = visual.PageHtml;

            FoundButtonsPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Нажатие на кнопку для отображения предыдущего найденного паттерна
        /// </summary>
        private void PatternFoundBack(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(FoundStep.Text);

            if (index <= 1)
                return;

            index--;
            FoundStep.Text = index.ToString();
            ButtonFoundNext.IsEnabled = true;

            var PatternItem = PatternListBox.SelectedItem as PatternUnit;
            var ChartItem = SourceListBox.SelectedItem as CandleDataInfoUnit;

            var visual = new Chart("Pattern", ChartItem.Table);
            visual.PageName = "PatternChartVisualShow";
            visual.PatternChartVisualShow(PatternItem, index-1);
            SearchBrowser.Address = visual.PageHtml;

            if (index <= 1)
                ButtonFoundBack.IsEnabled = false;
        }

        /// <summary>
        /// Нажатие на кнопку для отображения очередного найденного паттерна
        /// </summary>
        private void PatternFoundNext(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(FoundStep.Text);
 
            var PatternItem = PatternListBox.SelectedItem as PatternUnit;

            if (index >= PatternItem.Repeat)
                return;

            index++;
            FoundStep.Text = index.ToString();
            ButtonFoundBack.IsEnabled = true;

            var ChartItem = SourceListBox.SelectedItem as CandleDataInfoUnit;

            var visual = new Chart("Pattern", ChartItem.Table);
            visual.PageName = "PatternChartVisualShow";
            visual.PatternChartVisualShow(PatternItem, index-1);
            SearchBrowser.Address = visual.PageHtml;

            if (index >= PatternItem.Repeat)
                ButtonFoundNext.IsEnabled = false;
        }

        /// <summary>
        /// Нажатие на кнопку "Новый поиск"
        /// </summary>
        private void SearchNew(object sender, RoutedEventArgs e) => PatternSearchClear();


        /// <summary>
        /// Нажатие на кнопку для показа истории поисков
        /// </summary>
        private void ArchiveGo(object sender, RoutedEventArgs e) => ArchiveGo();
        public void ArchiveGo()
        {
            bool isSearch = PatternSearchGrid.Visibility == Visibility.Visible;
            HeadArchive.Text = "Поиск паттернов" + (isSearch ? ": история" : "");
            ButtonArchive.Content = isSearch ? "<<< назад" : "История поисков";
            PatternSearchGrid.Visibility = isSearch ? Visibility.Collapsed : Visibility.Visible;
            PatternArchiveGrid.Visibility = !isSearch ? Visibility.Collapsed : Visibility.Visible;

            global.MW.Pattern.PatternArchive.PatternArchiveInit();
        }

        /// <summary>
        /// Формирование кода для найденного паттерна
        /// </summary>
        private void FoundCodeGet(object sender, MouseButtonEventArgs e)
        {
            new PatternCode().Show();
        }
    }

    /// <summary>
    /// Шаблон единицы найденных паттернов
    /// </summary>
    public class PatternUnit
    {
        public int Id { get; set; }
        public string IdStr { get { return "#" + Id; } }
        public int Num { get; set; }            // Порядковый номер
        public string Candle { get; set; }      // Список свечей в пунктах целыми числами
        public string Key { get; set; }         // Ключ-свечи
        public int Repeat { get; set; }         // Количество повторений конкретного паттерна
        public int Unix { get; set; }           // Время в формате UNIX первого найденного совпадения
        public List<int> UnixList { get; set; } // Времена найденных паттернов в формате UNIX
        public double Price { get; set; }       // Цена первого найденного совпадения
        public int NolCount { get; set; }       // Количество нулей после запятой


        // Информация об свечных данных
        public int CdiId { get; set; }          // ID свечных данных из `_candle_data_info`
        public string Symbol { get; set; }      // Название инструмента
        public int TimeFrame { get; set; }      // Таймфрейм
        public string TF { get { return format.TF(TimeFrame); } }      // Таймфрейм в виде 10m
        public string CandlesCount { get; set; }// Количество свечей в графике


        // Данные о настройках
        public int PatternLength { get; set; }  // Длина паттерна
        public int ScatterPercent { get; set; } // Разброс в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений
        public bool CandleNolAvoid { get; set; }// Исключать нулевые свечи


        // Результат поиска
        public int SearchId { get; set; }       // ID поиска из `_pattern_search`
        public string Dtime { get; set; }       // Дата и время поиска
        public int FoundCount { get; set; }     // Количество найденных паттернов
        public string Duration { get; set; }    // Время выполнения


        // Результат теста
        public int ProfitCount { get; set; }    // Количество прибыльных результатов
        public int LossCount { get; set; }      // Количество убыточных результатов
        public int Procent { get; set; }        // Процент прибыльности
    }

    /// <summary>
    /// Шаблон настроек и результатов поиска паттернов
    /// </summary>
    public class PatternSearchParam
    {
        public int PatternLength { get; set; }  // Длина паттерна - количество свечей в паттерне
        public int ScatterPercent {  get; set; }// Разброс в процентах
        public int FoundRepeatMin {  get; set; }// Исключать менее N нахождений
        public bool CandleNolAvoid { get; set; }// Исключать нулевые свечи


        public int CdiId { get; set; }           // ID исторических данных
        public string Table { get; set; }       // Таблица со свечами
        public int NolCount { get; set; }       // Количество нулей после запятой
        public ulong Exp { get; set; }          //
        public int TimeFrame {  get; set; }     // Таймфрейм таблицы со свечами


        public int FoundCount { get; set; }     // Количество найденных паттернов
        public List<PatternUnit> FoundList { get; set; }   // Список с данными найденных паттернов
        public ulong Iterations { get; set; }   // Количество итераций
        public string Duration { get; set; }    // Время выполнения

        public int FoundId { get; set; }        // ID паттерна при выборе из прибыльных
    }
}
