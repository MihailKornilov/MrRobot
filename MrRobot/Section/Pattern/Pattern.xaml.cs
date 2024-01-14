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
using System.Diagnostics;
using System.Security.Cryptography;

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
            SearchResultCheck();
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

            var CDI = SourceListBox.SelectedItem as CandleDataInfoUnit;
            var param = new PatternSearchParam
            {
                CdiId = CDI.Id,
                PatternLength = (int)LengthSlider.Value,
                PrecisionPercent = (int)PrecisionPercentSlider.Value,
                FoundRepeatMin = Convert.ToInt32(FoundRepeatMin.Text),

                TimeFrame = CDI.TimeFrame,
                NolCount = CDI.NolCount,
                Exp = format.Exp(CDI.NolCount)
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
            var CDI = SourceListBox.SelectedItem as CandleDataInfoUnit;
            SPARAM = new PatternSearchParam()
            {
                IsProcess = true,

                PatternLength = (int)LengthSlider.Value,
                PrecisionPercent = (int)PrecisionPercentSlider.Value,
                FoundRepeatMin = Convert.ToInt16(FoundRepeatMin.Text),

                CdiId = CDI.Id,
                Table = CDI.Table,
                NolCount = CDI.NolCount,
                Exp = format.Exp(CDI.NolCount),
                TimeFrame = CDI.TimeFrame,

                PBar = new Progress<int>(v => {
                    SearchProgress.Value = v;
                    ProgressPrc.Content = v + "%";
                    ProсessInfo.Text = SPARAM.ProсessInfo;
                }),

                FoundList = new List<PatternFoundUnit>()
            };

            if (PatternSearchExist(SPARAM))
                return;

            await Task.Run(() => SearchProcess());

            PattentSearchResult();
            PatternFoundBaseInsert();
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

            param.FoundCount = Convert.ToInt32(row["foundCount"]);
            param.Iterations = Convert.ToUInt64(row["iterations"]);
            param.Duration = row["duration"];

            if (param.FoundCount == 0)
            {
                SearchStatistic(param);
                AutoProgon.PatternSearch();
                return true;
            }

            int SearchId = Convert.ToInt32(row["id"]);
            sql = "SELECT*" +
                  "FROM`_pattern_found`" +
                 $"WHERE`searchId`={SearchId} " +
                 $"  AND`repeatCount`>={param.FoundRepeatMin} " +
                  "ORDER BY`repeatCount`DESC";
            var Spisok = mysql.QueryList(sql);

            param.FoundCount = Spisok.Count;
            param.FoundList = new List<PatternFoundUnit>();
            for (int i = 0; i < Spisok.Count; i++)
            {
                var item = Spisok[i] as Dictionary<string, string>;

                if (param.FoundId == Convert.ToInt32(item["id"]))
                    param.FoundIndex = i;

                string[] UnixListString = item["unixList"].Split(',');
                var UnixList = new List<int>();
                for (int n = 0; n < UnixListString.Length; n++)
                    UnixList.Add(Convert.ToInt32(UnixListString[n]));

                param.FoundList.Add(new PatternFoundUnit
                {
                    Num = i + 1,
                    SearchId = Convert.ToInt32(item["searchId"]),
                    CdiId = param.CdiId,
                    Size = Convert.ToInt32(item["size"]),
                    Structure = item["structure"],
                    Repeat = Convert.ToInt32(item["repeatCount"]),
                    UnixList = UnixList,
                    TimeFrame = param.TimeFrame,
                    PatternLength = param.PatternLength,
                    NolCount = param.NolCount
                });
            }

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
            SourceListBox.IsEnabled = !param.IsProcess;
            SetupPanel.IsEnabled = !param.IsProcess;
            SearchPanel.Visibility        = isSearchNew ? Visibility.Visible : Visibility.Collapsed;
            SearchGoButton.Visibility     = param.IsProcess ? Visibility.Collapsed : Visibility.Visible;
            ProgressPanel.Visibility      = param.IsProcess ? Visibility.Visible : Visibility.Collapsed;
            ResultPanel.Visibility        = isSearchNew ? Visibility.Collapsed : Visibility.Visible;

            Visibility foundVis = param.FoundCount == 0 ? Visibility.Hidden : Visibility.Visible;
            FoundPanel.Visibility = foundVis;
            FoundBrowserPanel.Visibility = foundVis;
            FoundButtonsPanel.Visibility = foundVis;

            FoundListBox.ItemsSource = param.FoundCount > 0 ? param.FoundList : null;
            if(param.FoundCount > 0)
            {
                FoundListBox.SelectedIndex = param.FoundIndex;
                var item = FoundListBox.SelectedItem;
                FoundListBox.ScrollIntoView(item);
            }

            if (isSearchNew)
                return;

            CandlesDuplicate.Text = format.Num(param.FoundCount);
            IterationsCount.Text = format.Num(param.Iterations);
            IterationsTime.Text = param.Duration;
        }
        /// <summary>
        /// Поиск свечных паттернов без разброса в процентах
        /// </summary>
        void SearchProcess()
        {
            var dur = new Dur();

            string sql = $"SELECT COUNT(*)FROM`{SPARAM.Table}`";
            int count = mysql.Count(sql);

            sql = $"SELECT*FROM`{SPARAM.Table}`";
            var MASS = mysql.PatternSearchMass(sql, SPARAM, count);

            int CountSearch = MASS.Count - SPARAM.PatternLength * 2 + 1;   // Общее количество свечей на графике с учётом длины паттерна
            var bar = new ProBar(CountSearch);
            var FNDAss = new Dictionary<string, int>();
            SPARAM.PBar.Report(0);

            for (int i = 0; i < CountSearch; i++)
            {
                if (!SPARAM.IsProcess)
                    return;

                // Установка значения для Прогресс-бара
                if (bar.isUpd(i))
                {
                    SPARAM.ProсessInfo = $"Прошло времени: {bar.TimePass}";
                    int c = SPARAM.FoundList.Count;
                    if (c > 0)
                        SPARAM.ProсessInfo += $"\nНайдено {c} совпадени{format.End(c, "е", "я", "й")}";
                    SPARAM.PBar.Report(bar.Value);
                }

                for (int n = i + SPARAM.PatternLength; n < CountSearch + SPARAM.PatternLength; n++)
                {
                    SPARAM.Iterations++;

                    if (!MASS[i].Compare(MASS[n]))
                        continue;

                    string key = MASS[i].Structure();
                    int UnixN = MASS[n].CandleList[0].Unix;

                    if (!FNDAss.ContainsKey(key))
                    {
                        SPARAM.FoundList.Add(new PatternFoundUnit
                        {
                            Size = MASS[i].Size,
                            Structure = key,
                            UnixList = new List<int> { MASS[i].CandleList[0].Unix, UnixN }
                        });

                        int c = SPARAM.FoundList.Count - 1;
                        FNDAss.Add(key, c);
                        continue;
                    }

                    int index = FNDAss[key];
                    if (!SPARAM.FoundList[index].UnixList.Contains(UnixN))
                        SPARAM.FoundList[index].UnixList.Add(UnixN);
                }
            }

            SPARAM.IsProcess = false;
            SPARAM.PBar.Report(100);
            SPARAM.Duration = dur.Minutes();
        }
        /// <summary>
        /// Нажатие на кнопку `Отмена`
        /// </summary>
        void SearchCancel(object sender, RoutedEventArgs e)
        {
            SPARAM.IsProcess = false;
        }

        /// <summary>
        /// Возврат результата после выполнения поиска паттернов
        /// </summary>
        void PattentSearchResult()
        {
            if (SPARAM.Duration == null)
                return;

            // Сортировка найденных паттернов по количеству по убыванию
            var FoundListTmp = from pf in SPARAM.FoundList orderby pf.Repeat descending select pf;

            SPARAM.FoundList = new List<PatternFoundUnit>();
            foreach (var item in FoundListTmp)
            {
                item.Repeat = item.UnixList.Count;
                if (item.Repeat < SPARAM.FoundRepeatMin)
                    continue;

                item.Num = ++SPARAM.FoundCount;
                item.CdiId = SPARAM.CdiId;
                item.PatternLength = SPARAM.PatternLength;
                item.NolCount = SPARAM.NolCount;
                item.TimeFrame = SPARAM.TimeFrame;
                item.UnixList.Sort();
                SPARAM.FoundList.Add(item);
            }
        }
        /// <summary>
        /// Внесение найденных паттернов в базу
        /// </summary>
        void PatternFoundBaseInsert()
        {
            // Поиск был отменён
            if (SPARAM.Duration == null)
            {
                SearchStatistic(SPARAM);
                return;
            }

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

            if (SPARAM.FoundCount > 0)
            {
                var insert = new List<string>();
                for (int i = 0; i < SPARAM.FoundCount; i++)
                {
                    var item = SPARAM.FoundList[i];
                    insert.Add("(" +
                                    $"{SearchId}," +
                                    $"{item.Size}," +
                                    $"'{item.Structure}'," +
                                    $"{item.Repeat}," +
                                    $"'{string.Join(",", item.UnixList.ToArray())}'" +
                                ")");

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

                    insert = new List<string>();
                }
            }

            global.MW.Pattern.PatternArchive.SearchList();

            if(!AutoProgon.PatternSearch())
                SearchStatistic(SPARAM);
        }

        #endregion


        /// <summary>
        /// Визуальное отображение найденного паттерна
        /// </summary>
        void FoundListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundListBox.SelectedIndex == -1)
                return;

            var found = FoundListBox.SelectedItem as PatternFoundUnit;
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
            var found = FoundListBox.SelectedItem as PatternFoundUnit;

            if (step < 0 && index <= 1
             || step > 0 && index >= found.Repeat)
                return;

            index += step;
            FoundStep.Text = index.ToString();
            ButtonFoundBack.IsEnabled = index > 1;
            ButtonFoundNext.IsEnabled = index < found.Repeat;

            var visual = new Chart("Pattern", Candle.InfoUnit(found.CdiId).Table);
            visual.PageName = "PatternVisual";
            visual.PatternVisual(found, index-1);
            SearchBrowser.Address = visual.PageHtml;
        }


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
        void FoundCodeGet(object sender, MouseButtonEventArgs e) => new PatternCode().Show();
        void FoundInfoShow(object sender, MouseButtonEventArgs e) => new PatternInfo().Show();
    }

    /// <summary>
    /// Настройки и результат поиска паттернов
    /// </summary>
    public class PatternSearchParam
    {
        public bool IsProcess { get; set; }     // Происходит процесс поиска паттернов
        public IProgress<int> PBar { get; set; }// Прогресс-бар
        public string ProсessInfo { get; set; } // Информация о процессе поиска
        public bool Cancelled { get; set; }     // Процесс поиска был отменён

        public int PatternLength { get; set; }  // Длина паттерна - количество свечей в паттерне
        public int PrecisionPercent { get; set; }// Точность в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений


        public int CdiId { get; set; }          // ID исторических данных
        public string Table { get; set; }       // Таблица со свечами
        public int NolCount { get; set; }       // Количество нулей после запятой
        public ulong Exp { get; set; }          //
        public int TimeFrame { get; set; }      // Таймфрейм таблицы со свечами


        public int FoundCount { get; set; }     // Количество найденных паттернов
        public List<PatternFoundUnit> FoundList { get; set; }   // Список с данными найденных паттернов
        public ulong Iterations { get; set; }   // Количество итераций
        public string Duration { get; set; }    // Время выполнения

        public int FoundId { get; set; }        // ID паттерна при выборе из прибыльных
        public int FoundIndex { get; set; }     // Индекс паттерна при показе из прибыльных
    }

    /// <summary>
    /// Единица паттерна
    /// </summary>
    public class PatternUnit
    {
        double _PriceMax;
        double PriceMax
        {
            get { return _PriceMax; }
            set
            {
                if(_PriceMax < value)
                    _PriceMax = value;
            }
        }
        double _PriceMin;
        double PriceMin
        {
            get { return _PriceMin; }
            set
            {
                if (_PriceMin == 0 || _PriceMin > value)
                    _PriceMin = value;
            }
        }
        public int Size { get; set; }   // Размер паттерна в пунктах
        public List<CandleUnit> CandleList { get; set; } // Состав паттерна из свечей
        /// <summary>
        /// Создание паттерна
        /// </summary>
        public bool Create(List<CandleUnit> list, PatternSearchParam param)
        {
            if(list.Count < param.PatternLength)
                return false;

            foreach (var cndl in list)
            {
                if (cndl.High == cndl.Low)
                    return false;

                PriceMax = cndl.High;
                PriceMin = cndl.Low;
            }

            Size = (int)Math.Round((PriceMax - PriceMin) * param.Exp);

            CandleList = new List<CandleUnit>();
            foreach(var cndl in list)
                CandleList.Add(new CandleUnit(cndl, PriceMax, PriceMin));

            return true;
        }
        /// <summary>
        /// Сравнение паттернов
        /// </summary>
        public bool Compare(PatternUnit PU)
        {
            //            int Prc = 100 - SPARAM.PrecisionPercent;

            for (int k = 0; k < CandleList.Count; k++)
            {
                var src = CandleList[k];
                var dst = PU.CandleList[k];

                if (src.IsGreen != dst.IsGreen)
                    return false;
                if (src.SpaceTopInt != dst.SpaceTopInt)
                    return false;
                if (src.WickTopInt  != dst.WickTopInt)
                    return false;
                if (src.BodyInt     != dst.BodyInt)
                    return false;
                if (src.WickBtmInt  != dst.WickBtmInt)
                    return false;
                if (src.SpaceBtmInt != dst.SpaceBtmInt)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Содержание паттерна: свечи с учётом пустот сверху и снизу
        /// </summary>
        public string Structure()
        {
            string[] arr = new string[CandleList.Count];

            for (int k = 0; k < CandleList.Count; k++)
                arr[k] = CandleList[k].Struct();

            return string.Join(";", arr);
        }
    }

    /// <summary>
    /// Единица найденного паттерна
    /// </summary>
    public class PatternFoundUnit
    {
        public int Id { get; set; }
        public string IdStr { get { return "#" + Id; } }
        public int Num { get; set; }            // Порядковый номер
        public int Size { get; set; }           // Высота паттерна в пунктах
        public string Structure { get; set; }   // Список свечей в процентах целыми числами
        public int Repeat { get; set; }         // Количество повторений паттерна на графике
        public List<int> UnixList { get; set; } // Времена найденных паттернов в формате UNIX
        public int NolCount { get; set; }       // Количество нулей после запятой


        string _Cndl;
        public string Cndl
        {
            get
            {
                if (_Cndl != null)
                    return _Cndl;

                string[] pattern = Structure.Split(';');
                string[] cndl = new string[pattern.Length];
                for(int i = 0; i < pattern.Length; i++)
                {
                    string[] spl = pattern[i].Split(' ');
                    cndl[i] = $"{spl[1]} {spl[2]} {spl[3]}";
                }

                _Cndl = string.Join("\n", cndl.ToArray());
                return _Cndl;
            }
        }


        // Информация об свечных данных
        public int CdiId { get; set; }          // ID свечных данных из `_candle_data_info`
        public string Symbol { get; set; }      // Название инструмента
        public int TimeFrame { get; set; }      // Таймфрейм
        public string TF { get { return format.TF(TimeFrame); } }      // Таймфрейм в виде 10m
        public string CandlesCount { get; set; }// Количество свечей в графике


        // Данные о настройках
        public int PatternLength { get; set; }  // Длина паттерна
        public int PrecisionPercent { get; set; } // Точность в процентах
        public int FoundRepeatMin { get; set; } // Исключать менее N нахождений


        // Результат поиска
        public int SearchId { get; set; }       // ID поиска из `_pattern_search`
        public string Dtime { get; set; }       // Дата и время поиска
        public int FoundCount { get; set; }     // Количество найденных паттернов
        public string Duration { get; set; }    // Время выполнения


        // Результат теста
        public int ProfitCount { get; set; }    // Количество прибыльных результатов
        public int LossCount { get; set; }      // Количество убыточных результатов
        public int ProfitPercent { get; set; }        // Процент прибыльности
    }
}
