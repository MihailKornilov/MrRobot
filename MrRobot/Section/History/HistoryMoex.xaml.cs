using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.inc;

namespace MrRobot.Section
{
    public partial class HistoryMoex : UserControl
    {
        public HistoryMoex()
        {
            InitializeComponent();

            DataContext = new MoexDC();
            Market.Updated += () => DataContext = new MoexDC();

            EngineBox.SelectedIndex = 0;
            EngineBox.SelectionChanged += (s, e) =>
            {
                MoexDC.EngineId = (EngineBox.SelectedItem as MoexUnit).Id;
                DataContext = new MoexDC();
                MarketBox.SelectedIndex = 0;
            };

            FindBox.TextChanged += (s, e) =>
            {
                MoexDC.FindTxt = FindBox.Text;
                DataContext = new MoexDC();
                FoundCount.Content = $"найдено: {MOEX.Security.FoundCount(FindBox.Text)}";
            };
            FoundCancel.MouseLeftButtonDown += (s, e) => FindBox.Text = "";

            History.MenuMethod += (id) => {
                if (id == 2)
                    FindBox.Focus();
            };
        }

        void EngineIss(object sender, RoutedEventArgs e)
        {
            //MOEX.Engine.iss();
            //MOEX.Market.iss();
            //MOEX.Board.iss();
            //MOEX.BoardGroup.iss();
            //MOEX.Security.iss();
        }
    }

    public class MoexDC
    {
        public static string HdName { get => Market.Unit(2).Name; }
        public static List<MoexUnit> EngineList { get => MOEX.Engine.ListAll(); }

        public static int EngineId { get; set; } 
        public static List<MoexUnit> MarketList { get => MOEX.Market.ListEngine(EngineId); }


        public static string FindTxt
        {
            get => position.Val($"1.Moex.FindTxt", "");
            set => position.Set($"1.Moex.FindTxt", value);
        }
        public static Visibility FindVis { get => global.Vis(FindTxt.Length > 0); }
        public static List<SecurityUnit> SecurityList { get => MOEX.Security.List1000(FindTxt); }
    }
}
