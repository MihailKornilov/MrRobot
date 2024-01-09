
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MrRobot.Section
{
    public partial class History : UserControl
    {
        /// <summary>
        /// Выбор биржи
        /// </summary>
        private void MarketSel(int num)
        {
            for (int i = 1; i <= 3; i++)
            {
                Grid panel = FindName("HistoryMarketPanel_" + i) as Grid;
                panel.SetValue(Grid.ColumnProperty, i == num ? 1 : 0);

                Button button = FindName("HistoryMarketButton_" + i) as Button;
                button.Background = i == num ?
                    new SolidColorBrush(Color.FromArgb(255, 0xC1, 0xE2, 0xFA))  //C1E2FA
                    :
                    new SolidColorBrush(Color.FromArgb(255, 0xEC, 0xF3, 0xF8)); //ECF3F8
            }
        }


        private void Market_ByBit_Click(object sender, RoutedEventArgs e)
        {
            MarketSel(1);
        }

        private void Market_Binance_Click(object sender, RoutedEventArgs e)
        {
            MarketSel(2);
        }

        private void Market_Huobi_Click(object sender, RoutedEventArgs e)
        {
            MarketSel(3);
        }
    }
}
