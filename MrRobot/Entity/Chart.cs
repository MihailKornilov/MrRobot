using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;

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
                //PageDefault();
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
        CDIunit CdiUnit { get; set; }  // Информация об инструменте и таблице выводимого графика

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
