using System.Windows.Controls;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    public partial class History : UserControl
    {
        /// <summary>
        /// Меню: выбор биржи
        /// </summary>
        void MenuCreate(int marketId = 1)
        {
            MarketBox.ItemsSource = Market.ListAll();
            MarketBox.SelectionChanged += (s, e) =>
            {
                int sel = MarketBox.SelectedIndex;
                for (int i = 0; i < MarketBox.Items.Count; i++)
                {
                    var unit = MarketBox.Items[i] as MarketUnit;
                    var FN   = FindName($"MarketPanel{unit.Id}");
                    global.Vis(FN as Panel, i == sel);
                    global.Vis(FN as UserControl, i == sel);
                }
            };
            MarketBox.SelectedItem = Market.Unit(marketId);
        }
    }
}
