using System;
using System.IO;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Section;

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
        private string PHtml;
        public string PageHtml
        {
            get
            {
                PageDefault();
                return PHtml;
            }
            private set { PHtml = value; }
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
            CdiUnit = Candle.InfoUnitOnTable(TableName);
            Title = Section + ": " + CdiUnit.Name;
        }



        /// <summary>
        /// Создание страницы графика по умолчанию из шаблона
        /// </summary>
        private void PageDefault()
        {
            if (PageName != "Chart")
                return;

            //Чтение файла шаблона и сразу запись в файл HTML
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(PHtml);

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




        /// <summary>
        /// Визуальное отображение найденного паттерна (маленький график)
        /// </summary>
        public void PatternFound(PatternFoundUnit item)
        {
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(PHtml);

            var mass = new List<string>();
            double open = item.Price;
            int unix = item.Unix;
            int rangeBegin = format.TimeZone(unix - item.TimeFrame * 60 * 2);
            int rangeEnd = format.TimeZone(rangeBegin + item.TimeFrame * 60 * (item.PatternLength + 3));

            // Вставка в начало двух скрытых свечей для центрирования паттерна
            for (int i = 2; i > 0; i--)
                mass.Add("\n{" +
                        $"time:{format.TimeZone(unix - item.TimeFrame * 60 * i)}," +
                         "color:'#222'," + 
                        $"high:{open}," +
                        $"open:{open}," +
                        $"close:{open}," +
                        $"low:{open}" +
                   "}");

            ulong exp = format.Exp(item.NolCount);

            string[] split = item.Candle.Split('\n');
            for (int i = 0;  i < split.Length; i++)
            {
                string[] cndl = split[i].Split(' ');
                double close = open + Convert.ToDouble(cndl[1]) / exp;
                double high = Convert.ToDouble(cndl[0]) / exp;
                high = open > close ? open + high : close + high;
                double low = Convert.ToDouble(cndl[2]) / exp;
                low = open > close ? close - low : open - low;

                mass.Add("\n{" +
                            $"time:{format.TimeZone(unix)}," +
                            $"high:{high}," +
                            $"open:{open}," +
                            $"close:{close}," +
                            $"low:{low}" +
                       "}");
                unix += item.TimeFrame * 60;
                open = close;
            }

            // Вставка в конец двух скрытых свечей для центрирования паттерна
            for (int i = 0; i < 2; i++)
                mass.Add("\n{" +
                        $"time:{format.TimeZone(unix + item.TimeFrame * 60 * i)}," +
                         "color:'#222'," +
                        $"high:{open}," +
                        $"open:{open}," +
                        $"close:{open}," +
                        $"low:{open}" +
                   "}");


            string candlesData = "[" + string.Join(",", mass.ToArray()) + "]";

            string nolCount = item.NolCount.ToString();
            string tickSize = format.E((double)1/(double)exp);

            string line;
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
        /// Показ найденного паттерна на графике
        /// </summary>
        public void PatternChartVisualShow(PatternFoundUnit item, int UnixIndex = 0)
        {
            var read = new StreamReader(PageTmp);
            var write = new StreamWriter(PHtml);

            string line;
            int unix = item.UnixList[UnixIndex];
            string candlesData = PatternVisualShowData(item, unix);
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
        /// Выделение цветом найденного паттерна
        /// </summary>
        private string PatternVisualShowColor(CandleUnit unit, PatternFoundUnit item, ref int PatternLength)
        {
            if(item.PatternLength == PatternLength)
            {
                bool found = false;
                foreach(int ux in item.UnixList)
                    if(ux == unit.Unix)
                    {
                        found = true;
                        break;
                    }
               
                if(!found)
                    return "";
            }

            if (PatternLength-- == 0)
            {
                PatternLength = item.PatternLength;
                return "";
            }

            return "color:'#" + (unit.Close > unit.Open ? "60CE5E" : "FF324D") + "',";//56B854    F6465D
        }

        /// <summary>
        /// Список свечей для графика
        /// </summary>
        private string PatternVisualShowData(PatternFoundUnit item, int unix)
        {
            string sql = $"SELECT*" +
                         $"FROM`{TableName}`" +
                         $"WHERE`unix`>{unix - item.TimeFrame * 60 * 1000} " +
                         $"ORDER BY`unix`" +
                         $"LIMIT 3000";

            var mass = new List<string>();
            int PatternLength = item.PatternLength;
            foreach (CandleUnit unit in mysql.ConvertCandles(sql))
            {
                mass.Add("\n{" +
                            $"time:{format.TimeZone(unit.Unix)}," +
                            PatternVisualShowColor(unit, item, ref PatternLength) + 
                            $"high:{unit.High}," +
                            $"open:{unit.Open}," +
                            $"close:{unit.Close}," +
                            $"low:{unit.Low}" +
                       "}");
            }

            return "[" + string.Join(",", mass.ToArray()) + "]";
        }




        /// <summary>
        /// Отображение графика в начале тестирования
        /// </summary>
        public void TesterGraficInit()
        {
            StreamReader read = new StreamReader(PageTmp);
            StreamWriter write = new StreamWriter(PHtml);
                
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
            var write = new StreamWriter(PHtml);

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
