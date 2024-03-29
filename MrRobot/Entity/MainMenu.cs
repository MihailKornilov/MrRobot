﻿using System;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public class MainMenu
    {
        public delegate void DLGT();
        public static DLGT Changed { get; set; }

        public MainMenu()
        {
            // Создание главного меню
            var GridMain = G.MW.Content as Panel;
            var grid = GridMain.Children[0] as Panel;
            grid.Background = format.RGB("#C8DCEE");
            
            var SP = new StackPanel();
            SP.Background = format.RGB("#DCEBF8");
            SP.Margin = new Thickness(0,1,1,0);
            grid.Children.Add(SP);

            var lb = new ListBox();
            lb.Style = Application.Current.Resources["MMStyle"] as Style;
            lb.ItemTemplate = Application.Current.Resources["MMItemTmp"] as DataTemplate;
            lb.ItemContainerStyle = Application.Current.Resources["MMItemStyle"] as Style;
            for (int i = 1; i <= Enum.GetNames(typeof(SECT)).Length; i++)
                lb.Items.Add(new MMUnit(i));
            lb.SelectionChanged += Change;
            SP.Children.Add(lb);
            LB = lb;
            Go();
        }

        static ListBox LB { get; set; }

        /// <summary>
        /// Клик по кнопке меню
        /// </summary>
        public static void Go(object s, RoutedEventArgs e) => Go();
        public static void Go(int i = 0) => LB.SelectedIndex = position.MainMenu(i) - 1;

        /// <summary>
        /// Смена раздела Главного меню
        /// </summary>
        void Change(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
            var unit = box.SelectedItem as MMUnit;

            position.MainMenu(unit.Index);

            int index = 1;
            var UC = (G.MW.Content as Panel).Children;
            for (int i = 0; i < UC.Count; i++)
            {
                var uc = UC[i] as UserControl;
                if (uc == null)
                    continue;
                if(!uc.GetType().FullName.Contains("Section"))
                    continue;

                G.Vis(uc, index == unit.Index);
                index++;
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
        public string Image => $"pack://application:,,,/Resources/images/button-{Section}.png";
        public string ImageOver => $"pack://application:,,,/Resources/images/button-{Section}-over.png";
    }
}
