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
using MrRobot.Interface;

namespace MrRobot.Entity
{
	/// <summary>
	/// Логика взаимодействия для Chart.xaml
	/// </summary>
	public partial class ChartUC : UserControl
	{
		public ChartUC()
		{
			InitializeComponent();
		}

		// Раздел (каталог), в котором размещаются страницы графиков: шаблон (.tmp) и реальный (.html)
		string Section { get; set; }

		// Свечные данные
		CDIunit CdiUnit { get; set; }

		// Название страницы, в которой будет формироваться график
		string PageName { get; set; }

		// Заголовок страницы
		string Title => $"{Section}: {CdiUnit.Name}";

		// Путь к файлу-шаблону, с которого формируется страница с графиком
		string PathTmp   => Path.GetFullPath($"Browser/{Section}/{PageName}.tmp");
		string PathHtml  => Path.GetFullPath($"Browser/{Section}/{PageName}.html");
		string PathEmpty => Path.GetFullPath($"Browser/PageEmpty.html");


		public void CDI(string section, CDIunit unit, string pageName = "Chart")
		{
			if (unit == null)
				return;
			if (!my.Main.HasRows(unit.Table))
				return;

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
			Right();

			Browser.Address = PathEmpty;

			return true;
		}


		void Head(CDIunit unit)
		{
			Symbol(unit.Name);
			HeadTimeFrame.Text = unit.TF;
			Period(unit.DatePeriod);
			CandleCount(unit.RowsCount);
			Right();
		}
		public void HeadOnly(CDIunit unit)
		{
			Head(unit);
			Browser.Address = PathEmpty;
		}
		public void Symbol(string txt = "") => HeadSymbol.Text = txt;
		public void Period(string txt = "") => HeadPeriod.Text = txt;
		public void CandleCount(int count = 0) => HeadCandleCount.Text = Candle.CountTxt(count);
		public void Right(string txt = "") => HeadRight.Text = txt;


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

			var Candles = new List<string>();
			var Volumes = new List<string>();

			int Limit = 1000;
			string sql = "SELECT*" +
						$"FROM`{CdiUnit.Table}`" +
						 "ORDER BY`unix`" +
						$"LIMIT {Limit}";
			my.Main.Delegat(sql, res =>
			{
				var cndl = new CandleUnit(res);
				Candles.Add(cndl.CandleToChart());
				Volumes.Add(cndl.VolumeToChart());
			});

			string line;
			while ((line = read.ReadLine()) != null)
			{
				line = line.Replace("TITLE", Title);

				line = line.Replace("CANDLES_DATA", $"[\n{string.Join(",\n", Candles.ToArray())}]");
				line = line.Replace("VOLUME_DATA",  $"[\n{string.Join(",\n", Volumes.ToArray())}]");

				line = line.Replace("TICK_SIZE", CdiUnit.TickSize.ToString());
				line = line.Replace("NOL_COUNT", CdiUnit.Decimals.ToString());
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
			if (G.IsAutoProgon)
				return;

			CDI("Pattern", Candle.Unit(unit.CdiId), "PatternFound");
			HeadPanel.Height = new GridLength(0);

			var read = new StreamReader(PathTmp);
			var write = new StreamWriter(PathHtml);

			string sql = "SELECT*" +
						$"FROM`{CdiUnit.Table}`" +
						$"WHERE`unix`>={unit.UnixList[0]} " +
						 "ORDER BY`unix`" +
						$"LIMIT {unit.Length}";
			var candles = new List<CandleUnit>();
			my.Main.Delegat(sql, res => candles.Add(new CandleUnit(res)));

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
				line = line.Replace("NOL_COUNT", CdiUnit.Decimals.ToString());
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
			if (G.IsAutoProgon)
				return;

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
				line = line.Replace("NOL_COUNT", CdiUnit.Decimals.ToString());
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
		/// Список свечей для графика, на котором показывается найденных паттерн
		/// </summary>
		string PatternVisualData(PatternUnit item, int unix)
		{
			string sql = $"SELECT*" +
						 $"FROM`{CdiUnit.Table}`" +
						 $"WHERE`unix`>{unix - CdiUnit.TimeFrame * 60 * 1000} " +
						 $"ORDER BY`unix`" +
						 $"LIMIT 3000";

			int len = 0;
			var mass = new List<string>();
			my.Main.Delegat(sql, res =>
			{
				var cndl = new CandleUnit(res);
				if (cndl.Unix == unix)
					len = item.Length;

				mass.Add(cndl.CandleToChart(len-- > 0));
			});

			return $"[{string.Join(",\n", mass.ToArray())}]";
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

			string line;
			while ((line = read.ReadLine()) != null)
			{
				line = line.Replace("TITLE", Title);
				line = line.Replace("TICK_SIZE", CdiUnit.TickSize.ToString());
				line = line.Replace("NOL_COUNT", CdiUnit.Decimals.ToString());
				write.WriteLine(line);
			}
			read.Close();
			write.Close();

			Browser.Address = PathHtml;
		}





		/// <summary>
		/// Актуальные свечи для графика по конкретному инструменту
		/// </summary>
		public List<object> TradeCandlesActual(SpisokUnit unit)
		{
			Section = "Trade";
			PageName = "ChartActual";
			Symbol(unit.SymbolName);

			var CandleList = Candle.WCkline(unit.Symbol);
			var Candles = new List<string>();
			var Volumes = new List<string>();
			for (int k = 0; k < CandleList.Count; k++)
			{
				var cndl = CandleList[k] as CandleUnit;
				Candles.Insert(0, cndl.CandleToChart());
				Volumes.Insert(0, cndl.VolumeToChart());
			}

			var read = new StreamReader(PathTmp);
			var write = new StreamWriter(PathHtml);

			string title = $"{Section}: {unit.Name}";
			string CANDLES_DATA = "[\n" + string.Join(",\n", Candles.ToArray()) + "]";
			string VOLUMES_DATA = "[\n" + string.Join(",\n", Volumes.ToArray()) + "]";

			string line;
			while ((line = read.ReadLine()) != null)
			{
				line = line.Replace("TITLE", title);
				line = line.Replace("CANDLES_DATA", CANDLES_DATA);
				line = line.Replace("VOLUMES_DATA", VOLUMES_DATA);
				line = line.Replace("TICK_SIZE", unit.TickSize.ToString());
				line = line.Replace("NOL_COUNT", unit.Decimals.ToString());
				write.WriteLine(line);
			}
			read.Close();
			write.Close();

			Browser.Address = PathHtml;

			return CandleList;
		}
	}
}
