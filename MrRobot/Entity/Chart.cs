using System;
using System.IO;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Section;
using System.Linq;

namespace MrRobot.Entity
{
    public class Chart
    {
        private string Section { get; set; } = "";    // Раздел (каталог), в котором размещаются страницы графиков: шаблон (.tmp) и реальный (.html)

        // Формирование путей и названий для страниц .html и .tmp
        private string PName;
        public string PageName
        {
            get { return PName; }
            set
            {
                PName = value;

                string sect = Section;
                if (Section.Length > 0)
                    sect += "/";

                PageTmp  = Path.GetFullPath("Browser/" + sect + value + ".tmp");
                PageHtml = Path.GetFullPath("Browser/" + sect + value + ".html");
            }
        }


        // Путь к файлу-шаблону, с которого формируется страница с графиком
        private string PageTmp;


        // Путь к скомпилированному html-файлу с графиком
        string _PageHtml;
        public string PageHtml
        {
            get
            {
                PageDefault();
                return _PageHtml;
            }
            private set { _PageHtml = value; }
        }



        // Название таблицы с данными свечного графика. Если отсутствует, выводится пустая страница
        private string TableName;
        public string Table
        {
            get {  return TableName; }
            private set
            {
                string name = mysql.QueryString($"SHOW TABLES LIKE '{value}'");
                TableName = name ?? throw new Exception($"Таблицы {value} не существует в базе данных.");
            }
        }



        public string Title;            // Заголовок страницы
        private CandleDataInfoUnit CdiUnit;  // Информация об инструменте и таблице выводимого графика

        /// <summary>
        /// Конструктор для пустой страницы
        /// </summary>
        public Chart()
        {
            PageName = "PageEmpty";
            Title = PageName;
        }
        /// <summary>
        /// Конструктор без таблицы. Данные для графика берутся не из базы.
        /// </summary>
        public Chart(string section)
        {
            Section = section;
        }
       /// <summary>
        /// Конструктор с таблицей
        /// </summary>
        public Chart(string section, string tableName)
        {
            Section = section;
            Table = tableName;
            PageName = "Chart"; // Название страницы для .html и .tmp по умолчанию
            CdiUnit = Candle.UnitOnTable(TableName);
            Title = Section + ": " + CdiUnit.Name;
        }



        /// <summary>
        /// Создание страницы графика по умолчанию из шаблона
        /// </summary>
        void PageDefault()
        {
            if (PageName != "Chart")
                return;

            //Чтение файла шаблона и сразу запись в файл HTML
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(_PageHtml);

            int Limit = 10000;
            string sql = "SELECT*" +
                        $"FROM`{TableName}`" +
                         "ORDER BY`unix`DESC " +
                        $"LIMIT {Limit}";
            var QueryList = mysql.ChartCandles(sql);

            string candlesData = "[\n" + QueryList[0] + "]";
            string volumeData = "[\n" + QueryList[1] + "]";

            string tickSize = CdiUnit.TickSize.ToString();
            string nolCount = CdiUnit.NolCount.ToString();

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);

                line = line.Replace("CANDLES_DATA", candlesData);
                line = line.Replace("VOLUME_DATA", volumeData);

                line = line.Replace("TICK_SIZE", tickSize);
                line = line.Replace("NOL_COUNT", nolCount);
                write.WriteLine(line);
            }
            read.Close();
            write.Close();
        }





        // Вставка в начало и в конец двух скрытых свечей для центрирования паттерна
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
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(_PageHtml);

            var CDI = Candle.Unit(unit.CdiId);
            string sql = "SELECT*" +
                        $"FROM`{CDI.Table}`" +
                        $"WHERE`unix`>={unit.UnixList[0]} " +
                         "ORDER BY`unix`" +
                        $"LIMIT {unit.Length}";
            var candles = mysql.CandlesDataCache(sql);

            var mass = new List<string>();
            int unix = format.TimeZone(unit.UnixList[0]);
            int secondTF = unit.TimeFrame * 60;
            int rangeBegin = unix - secondTF * 2;
            int rangeEnd = unix + secondTF * unit.Length;

            PatternSourceEmpty(mass, rangeBegin, secondTF, candles[0].Open);

            for (int i = 0;  i < candles.Count; i++)
                mass.Add(candles[i].CandleToChart());

            PatternSourceEmpty(mass, rangeEnd, secondTF, candles.Last().Close);

            rangeEnd += secondTF;
            string candlesData = "[" + string.Join(",", mass.ToArray()) + "]";
            string nolCount = unit.NolCount.ToString();

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("TICK_SIZE", CDI.TickSize.ToString());
                line = line.Replace("NOL_COUNT", nolCount);
                line = line.Replace("CANDLES_DATA", candlesData);
                line = line.Replace("RANGE_BEGIN", rangeBegin.ToString());
                line = line.Replace("RANGE_END", rangeEnd.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();
        }

        /// <summary>
        /// Показ найденного паттерна на графике
        /// </summary>
        public void PatternVisual(PatternUnit item, int UnixIndex = 0)
        {
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(_PageHtml);

            string line;
            int unix = item.UnixList[UnixIndex];
            string candlesData = PatternVisualData(item, unix);
            int rangeBegin = format.TimeZone(unix) - item.TimeFrame * 60 * 30;
            int rangeEnd = rangeBegin + item.TimeFrame * 60 * 70;

            string nolCount = item.NolCount.ToString();
            double exp = format.Exp(item.NolCount);
            string tickSize = format.E((double)1 / exp);

            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("TICK_SIZE", tickSize);
                line = line.Replace("NOL_COUNT", nolCount);
                line = line.Replace("CANDLES_DATA", candlesData);
                line = line.Replace("RANGE_BEGIN", rangeBegin.ToString());
                line = line.Replace("RANGE_END", rangeEnd.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();
        }

        /// <summary>
        /// Список свечей для графика
        /// </summary>
        string PatternVisualData(PatternUnit item, int unix)
        {
            string sql = $"SELECT*" +
                         $"FROM`{TableName}`" +
                         $"WHERE`unix`>{unix - item.TimeFrame * 60 * 1000} " +
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
        public void TesterGraficInit()
        {
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(_PageHtml);
                
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
        }




        /// <summary>
        /// Актуальныы свечи для графика по конкретному инструменту
        /// </summary>
        public List<object> TradeCandlesActual(InstrumentUnit unit)
        {
            var CandleList = Candle.WCkline(unit.Symbol);
            var Candles = new List<string>();
            var Volumes = new List<string>();
            for (int k = 0; k < CandleList.Count; k++)
            {
                var cndl = CandleList[k] as CandleUnit;
                Candles.Insert(0, cndl.CandleToChart());
                Volumes.Insert(0, cndl.VolumeToChart());
            }

            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(_PageHtml);

            string CANDLES_DATA = "[\n" + string.Join(",\n", Candles.ToArray()) + "]";
            string VOLUMES_DATA = "[\n" + string.Join(",\n", Volumes.ToArray()) + "]";

            string line;
            while ((line = read.ReadLine()) != null)
            {
                line = line.Replace("TITLE", Title);
                line = line.Replace("CANDLES_DATA", CANDLES_DATA);
                line = line.Replace("VOLUMES_DATA", VOLUMES_DATA);
                line = line.Replace("TICK_SIZE", unit.TickSize.ToString());
                line = line.Replace("NOL_COUNT", unit.NolCount.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            return CandleList;
        }
    }
}
