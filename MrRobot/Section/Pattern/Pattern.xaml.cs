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
using System.Windows.Navigation;
using CefSharp.DevTools.Browser;

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
            PatternCandlesCount.Text = position.Val("3_CandlesCountForSearch", "1");
            PatternScatterPrc.Text = position.Val("3_ScatterPercent", "0");
            FoundRepeatMin.Text = position.Val("3_FoundRepeatMin", "0");

            global.Inited(3);
        }


        /// <summary>
        /// Выбор нового графика
        /// </summary>
        void SourceListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
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

            SearchClear();
            SearchBrowserShow(item);
        }


        /// <summary>
        /// Показ графика
        /// </summary>
        void SearchBrowserShow(CandleDataInfoUnit item)
        {
            PatternChartHead.Update(item);

            if (global.IsAutoProgon)
                return;

            SearchBrowser.Address = new Chart("Pattern", item.Table).PageHtml;
        }

        /// <summary>
        /// Изменение параметра Количество свечей (сохранение позиции)
        /// </summary>
        void CandlesCountChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_CandlesCountForSearch", PatternCandlesCount.Text);
        }

        /// <summary>
        /// Изменение параметра Разброс в процентах (сохранение позиции)
        /// </summary>
        void ScatterPrcChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_ScatterPercent", PatternScatterPrc.Text);
        }

        /// <summary>
        /// Изменение параметра "Исключать менее N нахождений"
        /// </summary>
        void FoundRepeatMinChanged(object sender, TextChangedEventArgs e)
        {
            position.Set("3_FoundRepeatMin", FoundRepeatMin.Text);
        }

        /// <summary>
        /// Изменение параметра Исключать нулевые свечи (сохранение позиции)
        /// </summary>





        #region SEARCH PROCESS

        /// <summary>
        /// Очистка результатов поиска паттернов
        /// </summary>
        void SearchClear()
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
        async void PatternSearchGo(object sender, RoutedEventArgs e)
        {
            var CDI = SourceListBox.SelectedItem as CandleDataInfoUnit;
            var param = new PatternSearchParam()
            {
                PatternLength = Convert.ToInt16(PatternCandlesCount.Text),
                ScatterPercent = Convert.ToInt16(PatternScatterPrc.Text),
                FoundRepeatMin = Convert.ToInt16(FoundRepeatMin.Text),

                CdiId = CDI.Id,
                Table = CDI.Table,
                NolCount = CDI.NolCount,
                Exp = format.Exp(CDI.NolCount),
                TimeFrame = CDI.TimeFrame,

                FoundList = new List<PatternFoundUnit>()
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

            var FoundList = new List<PatternFoundUnit>();
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

                FoundList.Add(new PatternFoundUnit
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
        void PatternSearchStat(PatternSearchParam param = null)
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
        /// Проверка на наличие нулевой свечи
        /// </summary>
        bool IsCandleNol(PatternSearchParam PARAM, int i, int[] Full)
        {
            if (PARAM.CandleNolCount == 0)
                return false;

            if(PARAM.CandleNolMiss > 0)
            {
                PARAM.CandleNolMiss--;
                return true;
            }

            int miss = 0;
            for (int k = PARAM.PatternLength - 1; k >= 0; k--)
                if (Full[i + k] == 0)
                {
                    miss = k + 1;
                    break;
                }

            if (miss == 0)
                return false;

            PARAM.CandleNolMiss = miss - 1;

            return true;
        }
        /// <summary>
        /// Сравнение очередного паттерна
        /// </summary>
        bool PatternCompare(PatternSearchParam PARAM, int i, int n, int[] WickTop, int[] Body, int[] WickBtm)
        {
            PARAM.Iterations++;

            for (int k = 0; k < PARAM.PatternLength; k++)
            {
                if (Math.Abs(WickTop[i + k] - WickTop[n + k]) > PARAM.ScatterPercent)
                    return false;
                if (Body[i + k] > 0 && Body[n + k] < 0)
                    return false;
                if (Body[i + k] < 0 && Body[n + k] > 0)
                    return false;
                if (Math.Abs(Body[i + k] - Body[n + k]) > PARAM.ScatterPercent)
                    return false;
                if (Math.Abs(WickBtm[i + k] - WickBtm[n + k]) > PARAM.ScatterPercent)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Формирование паттерна свечей в цифровом формате для отображения результата в приложении
        /// </summary>
        string CandlesString(PatternSearchParam PARAM, int i, int[] WickTop, int[] Body, int[] WickBtm)
        {
            string[] arr = new string[PARAM.PatternLength];

            for (int k = 0; k < PARAM.PatternLength; k++)
                arr[k] = WickTop[i + k] + " " +
                         Body[i + k] + " " +
                         WickBtm[i + k];

            return string.Join("\n", arr);
        }
        /// <summary>
        /// Поиск свечных паттернов без разброса в процентах
        /// </summary>
        /// PatternLength - длина паттерна (количество свечей в паттерне)
        /// NolAvoid - изберать нулевые свечи
        /// NolCount - количество знаков после запятой в инструменте
        bool PatternSearchProcess(PatternSearchParam PARAM, IProgress<int> Progress)
        {
            // Получение общего количества свечей
            string sql = $"SELECT COUNT(*)FROM`{PARAM.Table}`";
            int CCount = mysql.Count(sql);

            // Массивы дат, цен и размеров свечей всего списка (для оптимизации)
            int[] Unix = new int[CCount];           // Дата и время в формате UNIX
            double[] Price = new double[CCount];    // Цены (Close - цена закрытия)
            int[] WickTop = new int[CCount];        // Верхние хвосты свечей
            int[] Body = new int[CCount];           // Тела свечей
            int[] WickBtm = new int[CCount];        // Нижние хвосты свечей
            int[] Full = new int[CCount];           // Размер свечи от Low до High

            sql = $"SELECT*FROM`{PARAM.Table}`";
            mysql.PatternSearchMass(sql, PARAM, Unix, Price, WickTop, Body, WickBtm, Full);

            int CountSearch = CCount - PARAM.PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var bar = new ProBar(CountSearch);
            var FNDAss = new Dictionary<string, int>();

            for (int i = 0; i < CountSearch; i++)
            {
                // Установка значения для Прогресс-бара
                if (bar.isUpd(i))
                    Progress.Report(bar.Value);

                if(IsCandleNol(PARAM, i, Full))
                    continue;

                for (int n = i + PARAM.PatternLength; n < CountSearch + PARAM.PatternLength; n++)
                {
                    if (IsCandleNol(PARAM, i, Full))
                        continue;
                    if (!PatternCompare(PARAM, i, n, WickTop, Body, WickBtm))
                        continue;

                    string cndl = CandlesString(PARAM, i, WickTop, Body, WickBtm);

                    if (!FNDAss.ContainsKey(cndl))
                    {
                        PARAM.FoundList.Add(new PatternFoundUnit
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
        /// Возврат результата после выполнения поиска паттернов
        /// </summary>
        void PattentSearchResult(PatternSearchParam PARAM)
        {
            // Сортировка найденных паттернов по количеству по убыванию
            var FoundListTmp = from pf in PARAM.FoundList orderby pf.Repeat descending select pf;

            PARAM.FoundList = new List<PatternFoundUnit>();
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
        bool PatternFoundBaseInsert(PatternSearchParam param)
        {
            string sql = "INSERT INTO`_pattern_search`(" +
                            "`cdiId`," +

                            "`patternLength`," +
                            "`scatterPercent`," +
                            "`foundRepeatMin`," +

                            "`iterations`," +
                            "`foundCount`," +
                            "`duration`" +
                         ")VALUES(" +
                           $"{param.CdiId}," +

                           $"{param.PatternLength}," +
                           $"{param.ScatterPercent}," +
                           $"{param.FoundRepeatMin}," +

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
        void PatternListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
            var PatternItem = box.SelectedItem as PatternFoundUnit;
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
        void PatternFoundBack(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(FoundStep.Text);

            if (index <= 1)
                return;

            index--;
            FoundStep.Text = index.ToString();
            ButtonFoundNext.IsEnabled = true;

            var PatternItem = PatternListBox.SelectedItem as PatternFoundUnit;
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
        void PatternFoundNext(object sender, RoutedEventArgs e)
        {
            int index = Convert.ToInt32(FoundStep.Text);
 
            var PatternItem = PatternListBox.SelectedItem as PatternFoundUnit;

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
        void SearchNew(object sender, RoutedEventArgs e) => SearchClear();


        /// <summary>
        /// Нажатие на кнопку для показа истории поисков
        /// </summary>
        void ArchiveGo(object sender, RoutedEventArgs e) => ArchiveGo();
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
        void FoundCodeGet(object sender, MouseButtonEventArgs e)
        {
            new PatternCode().Show();
        }
    }
    
    /// <summary>
    /// Настройки и результат поиска паттернов
    /// </summary>
    public class PatternSearchParam
    {
        public int PatternLength { get; set; }  // Длина паттерна - количество свечей в паттерне
        public int ScatterPercent { get; set; } // Разброс в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений
        public int CandleNolCount { get; set; } // Количество нулевых свечей в свечных данных
        public int CandleNolMiss { get; set; }  // Количество свечей, которое нужно пропустить, если в паттерне оказалась нулевая свеча


        public int CdiId { get; set; }          // ID исторических данных
        public string Table { get; set; }       // Таблица со свечами
        public int NolCount { get; set; }       // Количество нулей после запятой
        public ulong Exp { get; set; }          //
        public int TimeFrame { get; set; }     // Таймфрейм таблицы со свечами


        public int FoundCount { get; set; }     // Количество найденных паттернов
        public List<PatternFoundUnit> FoundList { get; set; }   // Список с данными найденных паттернов
        public ulong Iterations { get; set; }   // Количество итераций
        public string Duration { get; set; }    // Время выполнения

        public int FoundId { get; set; }        // ID паттерна при выборе из прибыльных
    }

    /// <summary>
    /// Единица найденного паттерна
    /// </summary>
    public class PatternFoundUnit
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
}
