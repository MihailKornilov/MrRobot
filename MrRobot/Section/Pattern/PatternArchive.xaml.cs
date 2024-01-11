using System;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для PatternArchive.xaml
    /// </summary>
    public partial class PatternArchive : UserControl
    {
        public PatternArchive()
        {
            InitializeComponent();
        }

        bool PAinited;
        public void PatternArchiveInit()
        {
            if (PAinited)
                return;

            SearchList();
            SearchStat();
            ProfitList();

            PAinited = true;
        }

        /// <summary>
        /// Список успешных поисков
        /// </summary>
        public void SearchList()
        {
            string sql = "SELECT*" +
                         "FROM`_pattern_search`" +
                         "WHERE`foundCount`" +
                         "ORDER BY`id`DESC";
            var Spisok = mysql.QueryList(sql);

            if (Spisok.Count == 0)
                return;

            var ArchiveList = new List<PatternFoundUnit>();
            foreach (Dictionary<string, string> row in Spisok)
            {
                var CDI = Candle.InfoUnit(row["cdiId"]);
                ArchiveList.Add(new PatternFoundUnit
                {
                    Id = Convert.ToInt32(row["id"]),
                    CdiId = Convert.ToInt32(row["cdiId"]),
                    Dtime = format.DateOne(row["dtimeAdd"]),
                    Symbol = CDI.Name,
                    TimeFrame = CDI.TimeFrame,
                    CandlesCount = Candle.CountTxt(CDI.RowsCount),
                    PatternLength = Convert.ToInt32(row["patternLength"]),
                    PrecisionPercent = Convert.ToInt32(row["scatterPercent"]),
                    FoundRepeatMin = Convert.ToInt32(row["foundRepeatMin"]),
                    FoundCount = Convert.ToInt32(row["foundCount"]),
                    Duration = row["duration"]
                });
            }

            ArchiveData.ItemsSource = ArchiveList;
        }

        /// <summary>
        /// Статистика поисков и найденных паттернов
        /// </summary>
        void SearchStat()
        {
            // Всего поисков
            string sql = "SELECT COUNT(*)FROM`_pattern_search`";
            SearchAll.Content = mysql.Count(sql).ToString();

            // Поиски с результатами
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_search`" +
                  "WHERE`foundCount`";
            SearchWithResult.Content = mysql.Count(sql).ToString();

            // Всего паттернов
            sql = "SELECT COUNT(*)FROM`_pattern_found`";
            PatternAll.Content = mysql.Count(sql).ToString();

            // Прибыльные паттерны
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`>`lossCount`";
            PatternProfit.Content = mysql.Count(sql).ToString();

            // Прибыльные паттерны 50%
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`>`lossCount`" +
                    "AND 100-`lossCount`/`profitCount`*100>=50";
            PatternProfit50.Content = mysql.Count(sql).ToString();

            // Прибыльные паттерны 60%
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`>`lossCount`" +
                    "AND 100-`lossCount`/`profitCount`*100>=60";
            PatternProfit60.Content = mysql.Count(sql).ToString();

            // Прибыльные паттерны 70%
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`>`lossCount`" +
                    "AND 100-`lossCount`/`profitCount`*100>=70";
            PatternProfit70.Content = mysql.Count(sql).ToString();

            // Убыточные паттерны
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`<`lossCount`";
            PatternLoss.Content = mysql.Count(sql).ToString();

            // Не проверенные паттерны
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                  "WHERE!`profitCount`" +
                    "AND!`lossCount`";
            PatternNotTested.Content = mysql.Count(sql).ToString();
        }

        /// <summary>
        /// Нажатие на поиск или на паттерн
        /// </summary>
        void ArchiveUnitClick(object sender, MouseButtonEventArgs e)
        {
            var LBI = sender as ListBoxItem;
            var Item = LBI.Content as PatternFoundUnit;
            var CDI = Candle.InfoUnit(Item.CdiId);
            var param = new PatternSearchParam()
            {
                PatternLength = Item.PatternLength,
                PrecisionPercent = Item.PrecisionPercent,
                FoundRepeatMin = Item.FoundRepeatMin,
                CdiId = CDI.Id,
                TimeFrame = CDI.TimeFrame,
                NolCount = CDI.NolCount,
                FoundId = Item.Repeat > 0 ? Item.Id : 0
            };

            global.MW.Pattern.ArchiveGo();
            global.MW.Pattern.SourceListBox.SelectedItem = CDI;
            global.MW.Pattern.LengthSlider.Value = Item.PatternLength;
            global.MW.Pattern.PrecisionPercentSlider.Value = Item.PrecisionPercent;
            global.MW.Pattern.FoundRepeatMin.Text = Item.FoundRepeatMin.ToString();
            global.MW.Pattern.PatternSearchExist(param);
        }

        int ProfitPrc = 50;         // Минимальный процент прибыльности в запросе
        string ProfitOrder = "id";  // Порядок запроса
        void PatternProfitShow(object sender, MouseButtonEventArgs e)
        {
            ArchiveMenu.SelectedIndex = 1;

            var label = sender as Label;

            if(label.Name.Length > 0)
            {
                ProfitPrc = 0;
                if (label.Name != "PatternProfit")
                    ProfitPrc = Convert.ToInt32(label.Name.Substring(13, 2));
            }
            else
            {
                ProfitOrder = "id";
                if(label.Content.ToString() == "Процент")
                    ProfitOrder = "procent";
            }

            ProfitList();
        }

        /// <summary>
        /// Список прибыльных паттернов
        /// </summary>
        void ProfitList()
        {
            string PrcStr = "100-`lossCount`/`profitCount`*100";

            string sql = "SELECT DISTINCT(`searchId`)" +
                         "FROM`_pattern_found`" +
                         "WHERE`profitCount`>`lossCount`"+
                         $"AND {PrcStr}>={ProfitPrc}";
            string SearchIds = mysql.Ids(sql);

            sql = "SELECT" +
                    "`id`," +
                    "`cdiId`," +
                    "`patternLength`," +
                    "`foundCount`," +
                    "`scatterPercent`," +
                    "`foundRepeatMin`," +
                    "`dtimeAdd`" +
                  "FROM`_pattern_search`" +
                 $"WHERE`id`IN({SearchIds})";
            var SS = mysql.IdRowAss(sql);

            sql = "SELECT" +
                    "`id`," +
                    "`searchId`," +
                    "`repeat`," +
                    "`profitCount`," +
                    "`lossCount`," +
                   $"ROUND({PrcStr})`procent`" +
                  "FROM`_pattern_found`" +
                  "WHERE`profitCount`>`lossCount`" +
                   $"AND {PrcStr}>={ProfitPrc} "+
                 $"ORDER BY`{ProfitOrder}`DESC";
            var PFS = mysql.QueryList(sql);

            var list = new List<PatternFoundUnit>();
            foreach (Dictionary<string, string> row in PFS)
            {
                int Sid = Convert.ToInt32(row["searchId"]);
                var SSass = SS[Sid] as Dictionary<string, string>;
                var CDI = Candle.InfoUnit(SSass["cdiId"]);

                list.Add(new PatternFoundUnit
                {
                    Id = Convert.ToInt32(row["id"]),
                    CdiId = CDI.Id,
                    Dtime = format.DateOne(SSass["dtimeAdd"]),
                    Symbol = CDI.Name,
                    TimeFrame = CDI.TimeFrame,
                    CandlesCount = Candle.CountTxt(CDI.RowsCount),
                    PatternLength = Convert.ToInt32(SSass["patternLength"]),
                    PrecisionPercent = Convert.ToInt32(SSass["scatterPercent"]),
                    FoundRepeatMin = Convert.ToInt32(SSass["foundRepeatMin"]),
                    FoundCount = Convert.ToInt32(SSass["foundCount"]),
                    Repeat = Convert.ToInt32(row["repeat"]),
                    ProfitCount = Convert.ToInt32(row["profitCount"]),
                    LossCount = Convert.ToInt32(row["lossCount"]),
                    ProfitPercent = Convert.ToInt32(row["procent"])
                });
            }

            ArchivePatternList.ItemsSource = list;
        }
    }
}
