using System.IO;
using System.Net;
using System.Windows.Controls;
using static System.Console;

using CefSharp;
using CefSharp.Wpf;
using MrRobot.inc;

namespace MrRobot.Entity
{
	public class AdvChart
	{
		static readonly IPAddress IP = IPAddress.Loopback;
		static readonly int Port = 8888;

		public AdvChart(Panel panel, CDIunit cdi)
		{
			if (cdi == null)
				return;
			if (!PageCreate(cdi))
				return;

			if (panel.Children.Count == 0)
			{
				var browser = new ChromiumWebBrowser();
				browser.Address = $"http://{IP}:{Port}/advchart/index.html";
				panel.Children.Add(browser);
				return;
			}

			(panel.Children[0] as ChromiumWebBrowser).Reload();
		}


		string PathTmp  => Path.GetFullPath($"Browser/AdvChart/index.tmp.html");
		string PathHtml => Path.GetFullPath($"Browser/AdvChart/index.html");

		bool PageCreate(CDIunit unit)
		{
			if(!mysql.IsTableExist(unit.Table))
				return false;

			var read = new StreamReader(PathTmp);
			var write = new StreamWriter(PathHtml);

			int Limit = 500;
			string sql = "SELECT*" +
						$"FROM`{unit.Table}`" +
						 "ORDER BY`unix`DESC " +
						$"LIMIT {Limit}";
			var data = mysql.ChartCandles(sql, true);

			string line;
			while ((line = read.ReadLine()) != null)
			{
				line = line.Replace("CANDLES_DATA", $"[\n{data[0]}]");
				line = line.Replace("SYMBOL", unit.Symbol);
				line = line.Replace("NAME", unit.Name);
				line = line.Replace("TIME_FRAME", unit.TimeFrame.ToString());
				line = line.Replace("CANDLES_COUNT", Candle.CountTxt(unit.RowsCount, false));
				line = line.Replace("EXP", unit.Exp.ToString());
				line = line.Replace("NOL_COUNT", unit.Decimals.ToString());
				write.WriteLine(line);
			}
			read.Close();
			write.Close();

			return true;
		}

	}
}
