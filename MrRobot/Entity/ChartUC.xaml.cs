using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using CefSharp;
using MrRobot.inc;

namespace MrRobot.Entity
{
    /// <summary>
    /// Логика взаимодействия для Chart.xaml
    /// </summary>
    public partial class ChartUC : UserControl
    {
        public ChartUC() => InitializeComponent();

        // Раздел (каталог), в котором размещаются страницы графиков: шаблон (.tmp) и реальный (.html)
        string Section { get; set; }

        // Свечные данные
        CDIunit CdiUnit { get; set; }

        // Название страницы, в которой будет формироваться график
        string PageName { get; set; }

        // Заголовок страницы
        string Title { get => $"{Section}: {CdiUnit.Name}"; }

        // Путь к файлу-шаблону, с которого формируется страница с графиком
        string PathTmp   { get => Path.GetFullPath($"Browser/{Section}/{PageName}.tmp"); }
        string PathHtml  { get => Path.GetFullPath($"Browser/{Section}/{PageName}.html"); }
        string PathEmpty { get => Path.GetFullPath($"Browser/PageEmpty.html"); }


        public void CDI(string section, CDIunit unit, string pageName = "Chart")
        {
            Section = section;
            CdiUnit = unit;
            PageName = pageName;

            if (PageEmpty())
                return;

            Head(unit);
            PageDefault();
        }


        public void Empty() => PageEmpty(true);
        bool PageEmpty(bool isEmpty = false)
        {
            if (!isEmpty && CdiUnit != null)
                return false;

            Symbol();
            HeadTimeFrame.Text = "";
            Period();
            CandleCount();

            Browser.Address = PathEmpty;

            return true;
        }


        void Head(CDIunit unit)
        {
            Symbol(unit.Name);
            HeadTimeFrame.Text = unit.TF;
            Period(unit.DatePeriod);
            CandleCount(unit.RowsCount);
        }
        public void HeadOnly(CDIunit unit)
        {
            Head(unit);
            Browser.Address = PathEmpty;
        }
        public void Symbol(string txt = "") => HeadSymbol.Text = txt;
        public void Period(string txt = "") => HeadPeriod.Text = txt;
        public void CandleCount(int count = 0) => HeadCandleCount.Text = Candle.CountTxt(count);


        /// <summary>
        /// Переход на страницу биржи выбранного инструмента
        /// </summary>
        void SiteGo(object s, MouseButtonEventArgs e) => Process.Start($"https://www.bybit.com/ru-RU/trade/spot/{(s as TextBlock).Text}");


        public void Script(string txt) => Browser.ExecuteScriptAsync(txt);



        /// <summary>
        /// Создание страницы графика по умолчанию из шаблона
        /// </summary>
        void PageDefault()
        {
            if (PageName != "Chart")
                return;

            //Чтение файла шаблона и сразу запись в файл HTML
            var read = new StreamReader(PathTmp);
            var write = new StreamWriter(PathHtml);

            int Limit = 2000;
            string sql = "SELECT*" +
                        $"FROM`{CdiUnit.Table}`" +
                         "ORDER BY`unix`DESC " +
                        $"LIMIT {Limit}";
            var data = mysql.ChartCandles(sql);

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);

                line = line.Replace("CANDLES_DATA", $"[\n{data[0]}]");
                line = line.Replace("VOLUME_DATA",  $"[\n{data[1]}]");

                line = line.Replace("TICK_SIZE", CdiUnit.TickSize.ToString());
                line = line.Replace("NOL_COUNT", CdiUnit.NolCount.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            Browser.Address = PathHtml;
        }





        /// <summary>
        /// Вставка в начало и в конец двух скрытых свечей для центрирования паттерна
        /// </summary>
        void PatternSourceEmpty(List<string> mass, int unix, int secondTF, double price)
        {
            for (int i = 0; i < 2; i++)
                mass.Add("\n{" +
                        $"time:{unix + secondTF * i}," +
                         "color:'#222'," +
                        $"high:{price}," +
                        $"open:{price}," +
                        $"close:{price}," +
                        $"low:{price}" +
                   "}");
        }
        /// <summary>
        /// Визуальное отображение найденного паттерна (маленький график)
        /// </summary>
        public void PatternSource(PatternUnit unit)
        {
            CDI("Pattern", Candle.Unit(unit.CdiId), "PatternFound");
            HeadPanel.Height = new GridLength(0);

            var read = new StreamReader(PathTmp);
            var write = new StreamWriter(PathHtml);

            string sql = "SELECT*" +
                        $"FROM`{CdiUnit.Table}`" +
                        $"WHERE`unix`>={unit.UnixList[0]} " +
                         "ORDER BY`unix`" +
                        $"LIMIT {unit.Length}";
            var candles = mysql.CandlesDataCache(sql);

            var mass = new List<string>();
            int unix = format.TimeZone(unit.UnixList[0]);
            int secondTF = CdiUnit.TimeFrame * 60;
            int rangeBegin = unix - secondTF * 2;
            int rangeEnd = unix + secondTF * unit.Length;

            PatternSourceEmpty(mass, rangeBegin, secondTF, candles[0].Open);

            for (int i = 0; i < candles.Count; i++)
                mass.Add(candles[i].CandleToChart());

            PatternSourceEmpty(mass, rangeEnd, secondTF, candles.Last().Close);

            rangeEnd += secondTF;
            string candlesData = "[" + string.Join(",", mass.ToArray()) + "]";

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("TICK_SIZE", CdiUnit.TickSize.ToString());
                line = line.Replace("NOL_COUNT", CdiUnit.NolCount.ToString());
                line = line.Replace("CANDLES_DATA", candlesData);
                line = line.Replace("RANGE_BEGIN", rangeBegin.ToString());
                line = line.Replace("RANGE_END", rangeEnd.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            Browser.Address = PathHtml;
        }


        /// <summary>
        /// Показ найденного паттерна на графике
        /// </summary>
        public void PatternVisual(PatternUnit item, int UnixIndex = 0)
        {
            CDI("Pattern", Candle.Unit(item.CdiId), "PatternVisual");

            var read = new StreamReader(PathTmp);
            var write = new StreamWriter(PathHtml);

            int unix = item.UnixList[UnixIndex];
            string candlesData = PatternVisualData(item, unix);
            int rangeBegin = format.TimeZone(unix) - CdiUnit.TimeFrame * 60 * 30;
            int rangeEnd = rangeBegin + CdiUnit.TimeFrame * 60 * 70;
            string line;

            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("TICK_SIZE", CdiUnit.TickSize.ToString());
                line = line.Replace("NOL_COUNT", CdiUnit.NolCount.ToString());
                line = line.Replace("CANDLES_DATA", candlesData);
                line = line.Replace("RANGE_BEGIN", rangeBegin.ToString());
                line = line.Replace("RANGE_END", rangeEnd.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            Browser.Address = PathHtml;
        }
        /// <summary>
        /// Список свечей для графика
        /// </summary>
        string PatternVisualData(PatternUnit item, int unix)
        {
            string sql = $"SELECT*" +
                         $"FROM`{CdiUnit.Table}`" +
                         $"WHERE`unix`>{unix - CdiUnit.TimeFrame * 60 * 1000} " +
                         $"ORDER BY`unix`" +
                         $"LIMIT 3000";

            var mass = new List<string>();
            int len = 0;
            foreach (var cndl in mysql.CandlesDataCache(sql))
            {
                if (cndl.Unix == unix)
                    len = item.Length;

                mass.Add(cndl.CandleToChart(len-- > 0));
            }

            return "[" + string.Join(",\n", mass.ToArray()) + "]";
        }






        /// <summary>
        /// Отображение графика в начале тестирования
        /// </summary>
        public void TesterGraficInit(CDIunit item)
        {
            CDI("Tester", item, "TesterProcess");
            Period();
            CandleCount();

            var read = new StreamReader(PathTmp);
            var write = new StreamWriter(PathHtml);

            string tickSize = CdiUnit.TickSize.ToString();
            string nolCount = CdiUnit.NolCount.ToString();

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("TICK_SIZE", tickSize);
                line = line.Replace("NOL_COUNT", nolCount);
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            Browser.Address = PathHtml;
        }
    }
}
