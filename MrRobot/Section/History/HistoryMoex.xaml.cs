using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.Entity;
using MrRobot.Connector;

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
                MoexDC.EngineId = (EngineBox.SelectedItem as EngineUnit).Id;
                DataContext = new MoexDC();
                MarketBox.SelectedIndex = 0;
            };
        }

        void EngineIss(object sender, RoutedEventArgs e)
        {
            MOEX.Engine.iss();
            MOEX.Market.iss();
        }
    }

    public class MoexDC
    {
        public static string HdName { get => Market.Unit(2).Name; }
        public static List<EngineUnit> EngineList { get => MOEX.Engine.ListAll(); }

        public static int EngineId { get; set; } 
        public static List<MoexMarketUnit> MarketList { get => MOEX.Market.ListEngine(EngineId); }
    }
}
