using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public partial class MainMenu : UserControl
    {
        public delegate void Dlgt();
        public static Dlgt Changed = () => { };

        public MainMenu()
        {
            InitializeComponent();

            /// Создание главного меню
            for (int i = 1; i < SectionName().Length; i++)
                MMlist.Items.Add(new MMUnit(i));

            MMlist.SelectionChanged += Change;
        }


        /// <summary>
        /// Список разделов меню
        /// </summary>
        public static string[] SectionName()
        {
            string[] mass = {
                "Global",
                "History",
                "Converter",
                "Pattern",
                "Tester",
                "Trade",
                "Setting"
            };

            return mass;
        }

        /// <summary>
        /// Клик по кнопке меню
        /// </summary>
        void ButtonClick(object sender, RoutedEventArgs e) => Go((sender as Button).TabIndex);
        public static void Go(int i = 0) => global.MW.MMenu.MMlist.SelectedIndex = position.MainMenu(i) - 1;

        /// <summary>
        /// Смена раздела Главного меню
        /// </summary>
        void Change(object sender, SelectionChangedEventArgs e)
        {
            int index = position.MainMenu();
            string[] section = SectionName();
            for (int i = 1; i < section.Length; i++)
            {
                var uc = global.MW.FindName(section[i]) as UserControl;
                global.Vis(uc, i == index);
            }

            Changed();

            switch (index)
            {
                case 3:
                    global.MW.Pattern.PatternInit();
                    break;

                case 4:
                    global.MW.Tester.TesterInit();
                    break;

                case 5:
                    global.MW.Trade.TradeInit();
                    break;
            }
        }
    }

    /// <summary>
    /// Данные кнопок главного меню
    /// </summary>
    public class MMUnit
    {
        public MMUnit(int i) => Index = i;
        // Подярковый индекс
        public int Index { get; private set; }
        // Имя раздела
        public string Section   { get { return MainMenu.SectionName()[Index]; } }
        public string Image     { get { return $"pack://application:,,,/Resources/images/button-{Section}.png"; } }
        public string ImageOver { get { return $"pack://application:,,,/Resources/images/button-{Section}-over.png"; } }
    }
}
