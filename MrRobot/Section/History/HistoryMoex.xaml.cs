using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.Entity;

namespace MrRobot.Section
{
    public partial class HistoryMoex : UserControl
    {
        public HistoryMoex()
        {
            InitializeComponent();

            HeadTB.Text = Market.Unit(2).Name;
        }

        void EngineIss(object sender, RoutedEventArgs e)
        {
            MOEX.Engine.iss();
        }
    }
}
