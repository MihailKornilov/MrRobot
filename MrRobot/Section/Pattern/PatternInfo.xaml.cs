using System;
using System.Windows;

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
            var found = global.MW.Pattern.FoundListBox.SelectedItem as PatternFoundUnit;
            int step = Convert.ToInt32(global.MW.Pattern.FoundStep.Text);

            PatternInfoBox.Text =
                $"Unix:  {found.UnixList[0]}\n" +
                $"Dtime: {format.DTimeFromUnix(found.UnixList[0])}\n" +
                $"Size:  {found.Size}\n" +
                 "\n" +
                $"Struct:\n{found.Structure.Replace(';', '\n')}\n" +
                 "\n" +
                $"{CandleList(found)}\n" + 
              $"Step: {step}";
        }

        string CandleList(PatternFoundUnit found)
        {
            var send = "";
            var CDI = Candle.InfoUnit(found.CdiId);
            string sql = "SELECT*" +
                        $"FROM`{CDI.Table}`" +
                        $"WHERE`unix`>={found.UnixList[0]} " +
                         "ORDER BY`unix`" +
                        $"LIMIT {found.PatternLength}";
            foreach(var cndl in mysql.ConvertCandles(sql))
                send += $"High: {cndl.High}   Open: {cndl.Open}   Close: {cndl.Close}   Low: {cndl.Low}\n";

            return send;
        }
    }
}
