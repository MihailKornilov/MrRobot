using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Section;

namespace MrRobot.Entity
{
    public partial class MainMenu : UserControl
    {
        public MainMenu()
        {
            InitializeComponent();

            /// Создание главного меню
            for (int i = 1; i < SectionName().Length; i++)
                MMlist.Items.Add(new MMUnit(i));

            MMlist.SelectionChanged += Change;
            global.MW.Loaded += (s, e) => Go();
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

            global.MW.Pattern.ArchiveGo(true);
            global.MW.Pattern.FoundLine();
            CDIpanel.PageChanged();

            switch (index)
            {
                case 1:
                    if (SectionUpd.Update[1])
                        SectionUpd.History();

                    global.MW.History.HistoryInit();
                    break;

                case 2:
                    if (SectionUpd.Update[2])
                        SectionUpd.Converter();

                    global.MW.Converter.ConverterInit();
                    break;

                case 3:
                    if (SectionUpd.Update[3])
                        SectionUpd.Pattern();

                    global.MW.Pattern.PatternInit();
                    break;

                case 4:
                    if (SectionUpd.Update[4])
                        SectionUpd.Tester();

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
