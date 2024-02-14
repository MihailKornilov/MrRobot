using System.Windows.Controls;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    public partial class History : UserControl
    {
        public delegate void Dlgt(int id);
        public static Dlgt MenuMethod = (int id) => { };

        /// <summary>
        /// Меню: выбор биржи
        /// </summary>
        void MenuCreate()
        {
            MarketBox.ItemsSource = Market.ListAll();
            MarketBox.SelectionChanged += (s, e) =>
            {
                int sel = MarketBox.SelectedIndex;
                MarketUnit unit;
                for (int i = 0; i < MarketBox.Items.Count; i++)
                {
                    unit = MarketBox.Items[i] as MarketUnit;
                    var FN = FindName($"MarketPanel{unit.Id}");
                    G.Vis(FN as Panel, i == sel);
                    G.Vis(FN as UserControl, i == sel);
                }
                unit = MarketBox.SelectedItem as MarketUnit;
                position.Set("1.MarketMenu.Index", unit.Id);
                MenuMethod(unit.Id);
            };
            MarketBox.SelectedItem = Market.Unit(position.Val("1.MarketMenu.Index", 1));
        }
    }
}
