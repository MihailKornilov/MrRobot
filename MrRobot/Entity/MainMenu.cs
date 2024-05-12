using System;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using RobotLib;
using MrRobot.inc;

namespace MrRobot.Entity
{
    public class MainMenu
    {
        public delegate void DLGT();
        public static DLGT Changed { get; set; }

		static ListBox LB { get; set; }
		public MainMenu()
        {
            // Создание главного меню
            var SP = new StackPanel();
            SP.Background = format.RGB("#DCEBF8");
            SP.Margin = new Thickness(0,1,1,0);

            LB = new ListBox();
			LB.Style = Application.Current.Resources["MMStyle"] as Style;
			LB.ItemTemplate = Application.Current.Resources["MMItemTmp"] as DataTemplate;
			LB.ItemContainerStyle = Application.Current.Resources["MMItemStyle"] as Style;
            for (int i = 1; i <= Enum.GetNames(typeof(SECT)).Length; i++)
				LB.Items.Add(new MMUnit(i));
			LB.SelectionChanged += Change;
            SP.Children.Add(LB);

			G.Grid0.Add(SP);

            Go();
        }

        /// <summary>
        /// Клик по кнопке меню
        /// </summary>
        public static void Go(object s, RoutedEventArgs e) => Go();
        public static void Go(int i = 0) =>
            LB.SelectedIndex = position.MainMenu(i) - 1;

        /// <summary>
        /// Смена раздела Главного меню
        /// </summary>
        void Change(object sender, SelectionChangedEventArgs e)
        {
			var box = sender as ListBox;
            var unit = box.SelectedItem as MMUnit;

			position.MainMenu(unit.Index);

            for (int index = 1; index <= G.Grid1.Count; index++)
            {
				var uc = G.Grid1[index-1] as UserControl;
				G.Vis(uc, index == unit.Index);
            }

            Changed?.Invoke();
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
        public string Section => Enum.GetName(typeof(SECT), Index);
        public string Image     => $"pack://application:,,,/Resources/images/button-{Section}.png";
        public string ImageOver => $"pack://application:,,,/Resources/images/button-{Section}-over.png";
    }
}
