using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

using CefSharp.Wpf;
using MrRobot.inc;
using Newtonsoft.Json.Converters;

namespace MrRobot.Entity
{
	public class ChartLight
	{

		ChromiumWebBrowser Browser;
		string PathTmp  => Path.GetFullPath($"Browser/History/LightLine.tmp.html");
		string PathHtml => Path.GetFullPath($"Browser/History/LightLine.html");


		public ChartLight(Panel panel)
		{
			if (panel.Children.Count == 0)
			{
				Browser = new ChromiumWebBrowser();
				panel.Children.Add(Browser);
			}
			else
				Browser = panel.Children[0] as ChromiumWebBrowser;

			PageCreate();
		}



		bool PageCreate()
		{
			var table = "binance_1";

			var read  = new StreamReader(PathTmp);
			var write = new StreamWriter(PathHtml);

			var LineData = new List<string>();

			int Limit = 5000;
			string sql = "SELECT*" +
						$"FROM`{table}`" +
						 "ORDER BY`unix`DESC " +
						$"LIMIT {Limit}";
			int last = 0;
			my.Tick.Delegat(sql, res =>
			{
				var unit = new TickUnit(res);
				int ux = (int)(unit.Unix/1000);
				if (last == ux)
					return;
				last = ux;
				LineData.Add(unit.ToChart);
			});

			LineData.Reverse();

			string line;
			while ((line = read.ReadLine()) != null)
			{
				line = line.Replace("TITLE", "Chart Line");
				line = line.Replace("LINE_DATA", $"[\n{string.Join(",\n", LineData.ToArray())}]");
				write.WriteLine(line);
			}
			read.Close();
			write.Close();

			Browser.Address = PathHtml;

			return true;
		}

	}
}
