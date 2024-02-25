using System.Windows;
using System.Collections.Generic;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для PatternInfo.xaml
    /// </summary>
    public partial class PatternInfo : Window
    {
        public PatternInfo()
        {
            InitializeComponent();
            InfoShow();
        }

        void InfoShow()
        {
            var found = G.Pattern.FoundListBox.SelectedItem as PatternUnit;
            var CDI = Candle.Unit(found.CdiId);

            PatternInfoBox.Text =
                $"{CDI.Name} {CDI.TF} " +
                $"Struct:\n{found.Struct}\n" +
                 "\n" +
                $"{FoundList(found)}\n";
        }



        List<PatternUnit> PatternList(PatternUnit found)
        {
			var CDI = Candle.Unit(found.CdiId);
			var CandleList = new List<CandleUnit>();
            var PatternList = new List<PatternUnit>();

			string sql = $"SELECT*FROM`{CDI.Table}`";
			my.Main.Delegat(sql, res =>
            {
                int unix = res.GetInt32("unix");
				if (CandleList.Count == 0 && !found.UnixList.Contains(unix))
					return;

				CandleList.Add(new CandleUnit(res));

				if (CandleList.Count < found.Length)
					return;

				PatternList.Add(new PatternUnit(CandleList, found.CdiId, found.PrecisionPercent));
				CandleList.Clear();
			});

            return PatternList;
        }
        string FoundList(PatternUnit found)
        {
            var CDI = Candle.Unit(found.CdiId);
            string send = "";
            int step = 1;
            foreach (var patt in PatternList(found))
            {
                var cList = patt.CandleList;
                send += $"{step++}. " +
                        //$"{cList[0].Unix} " +
                        $"{format.DTimeFromUnix(cList[0].Unix)}  " +
                        $"Size: {patt.Size}\n" +
                        $"{CandleList(cList, CDI.Decimals)}" +
                        $"\n";
            }

            return send;
        }

        string CandleList(List<CandleUnit> list, int precission)
        {
            var send = "";
            foreach(var cndl in list)
                send += $"   High: {format.Price(cndl.High, precission)}" +
                        $"   Open: {format.Price(cndl.Open, precission)}" +
                        $"   Close: {format.Price(cndl.Close, precission)}" +
                        $"   Low: {format.Price(cndl.Low, precission)}\n";
            return send;
        }
    }
}
