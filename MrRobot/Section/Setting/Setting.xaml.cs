using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Setting.xaml
    /// </summary>
    public partial class Setting : UserControl
    {
        public Setting()
        {
            InitializeComponent();
        }

        private void AutoProgonGo(object sender, RoutedEventArgs e)
        {
            if (global.IsAutoProgon)
                return;

            var param = new AutoProgonParam
            {
                ConvertTF = "5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20",
                PatternLength = "3",
                PrecisionPercent = "99",
                FoundRepeatMin = "10",
                SymbolMass = new string[]
                {
                    "SOL/USDT",
                    "ETH/USDT",
                    "BTC/USDT",
                    "ALGO/USDT",
                    "XRP/USDT",
                    "AVAX/USDT",
                    "LINK/USDT",
                    "DOGE/USDT",
                    "ICP/USDT",
                    "MATIC/USDT",
                    "DYDX/USDT",
                    "DOT/USDT",
                    "ADA/USDT"
                }
            };

            AutoProgon.Go(param);
        }
    }
}
