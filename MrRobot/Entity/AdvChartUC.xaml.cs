using System.IO;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using CefSharp;

namespace MrRobot.Entity
{
    /// <summary>
    /// Логика взаимодействия для AdvChartUC.xaml
    /// </summary>
    public partial class AdvChartUC : UserControl
    {
        public AdvChartUC() => InitializeComponent();

        string PathTmp  { get => Path.GetFullPath($"Browser/AdvChart/index.tmp.html"); }
        string PathHtml { get => Path.GetFullPath($"Browser/AdvChart/index.html"); }
        string PathHttp { get => "c:/www/nyandoma/advanced_chart/index.html"; }

        public void CDI(CDIunit unit)
        {
            var read = new StreamReader(PathTmp);
            var write = new StreamWriter(PathHtml);

            int Limit = 1000;
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
                line = line.Replace("NOL_COUNT", unit.NolCount.ToString());
                write.WriteLine(line);
            }
            read.Close();
            write.Close();

            File.Copy(PathHtml, PathHttp, true);

            if (ACBrowser.Address == null)
                ACBrowser.Address = "http://nyandoma/advanced_chart/index.html";
            else
                ACBrowser.Reload();
        }
    }
}

