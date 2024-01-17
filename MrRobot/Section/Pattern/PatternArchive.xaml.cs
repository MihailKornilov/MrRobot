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
            ArchivePatternList.ItemsSource = Patterns.ProfitList();

            PAinited = true;
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
        /// Список успешных поисков
        /// </summary>
        public void SearchList()
        {
            SearchStat();
            ArchiveData.ItemsSource = Patterns.SearchListAll();
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

            ArchivePatternList.ItemsSource = Patterns.ProfitList(ProfitPrc, ProfitOrder);
        }




        /// <summary>
        /// Нажатие на поиск или на паттерн
        /// </summary>
        void ArchiveUnitClick(object sender, MouseButtonEventArgs e)
        {
            var LBI = sender as ListBoxItem;
            var Item = LBI.Content as PatternUnit;
            var CDI = Candle.Unit(Item.CdiId);
            var param = new PatternSearchParam()
            {
                CdiId = CDI.Id,
                PatternLength = Item.Length,
                PrecisionPercent = Item.PrecisionPercent,
                FoundRepeatMin = Item.FoundRepeatMin,
                FoundId = Item.Repeat > 0 ? Item.Id : 0
            };

            global.MW.Pattern.ArchiveGo();
            global.MW.Pattern.SourceListBox.SelectedItem = CDI;
            global.MW.Pattern.LengthSlider.Value = Item.Length;
            global.MW.Pattern.PrecisionPercentSlider.Value = Item.PrecisionPercent;
            global.MW.Pattern.FoundRepeatMin.Text = Item.FoundRepeatMin.ToString();
            global.MW.Pattern.PatternSearchExist(param);
        }
    }
}
