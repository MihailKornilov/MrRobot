using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Tester.xaml
    /// </summary>
    public partial class Tester : UserControl
    {
        public Tester() => G.Tester = this;

        public void TesterInit()
        {
			DataContext = new Balance();

			InitializeComponent();

			CDIpanel.Page(4).TBLink = SelectLink.TBLink;
            CDIpanel.Page(4).OutMethod += SourceChanged;

            RobotsListBox.ItemsSource = Robots.ListBox();
            RobotsListBox.SelectedIndex = 0;
            RobotAddButton.Click += Robots.Load;

            Visualization = position.Val("4_VisualCheck.IsChecked", true);
            VisualCheck.IsChecked = Visualization;
            NoVisualButton.Visibility = G.Vis(!Visualization);
            AutoGoSlider.Value = position.Val("4_TesterSlider.Value", 0);

            LogMenu.SelectedIndex = position.Val("4_LogMenu_SelectedIndex", 0);

            SourceChanged();
        }



        /// <summary>
        /// Выбор нового графика
        /// </summary>
        void SourceChanged()
        {
            if (position.MainMenu() != (int)SECT.Tester)
                return;

            G.Vis(RobotPanel, CDIpanel.CdiId > 0);
            RobotsListBox.SelectedIndex = 0;

            if (CDIpanel.CdiId == 0)
                return;

            G.Vis(UseTF1Check, CDIpanel.CdiUnit().ConvertedFromId > 0);

            if (!G.IsAutoProgon)
                EChart.CDI("Tester", CDIpanel.CdiUnit());
        }



        /// <summary>
        /// Сохранение позиции Лог-меню
        /// </summary>
        void LogMenuPosition(object sender, SelectionChangedEventArgs e)
        {
            var tab = sender as TabControl;
            position.Set("4_LogMenu_SelectedIndex", tab.SelectedIndex.ToString());
        }

        /// <summary>
        /// Установка ширины Робот-лога при изменении ширины приложения
        /// </summary>
        public void RobotLogWidthSet(object sender, SizeChangedEventArgs e)
        {
            if (position.MainMenu() != (int)SECT.Tester)
                return;

            if (RobotLogList.ActualWidth == 0)
            {
                RobotLogList.Width = 1280;
                return;
            }

            if (LogMenu.ActualWidth == 0)
                return;

            double width = LogMenu.ActualWidth - 6;
            RobotLogList.Width = width;
        }


        /// <summary>
        /// Робот выбран в списке роботов
        /// </summary>
        void RobotListChanged(object sender, SelectionChangedEventArgs e) => GlobalInit();

        /// <summary>
        /// Нажатие на галочку: Использовать таймфрейм 1m
        /// </summary>
        void UseTF1Checked(object sender, RoutedEventArgs e) => GlobalInit();

        /// <summary>
        /// Нажатие на галочку: Визуализация
        /// </summary>
        void VisualChecked(object sender, RoutedEventArgs e)
        {
            Visualization = (bool)VisualCheck.IsChecked;
            position.Set("4_VisualCheck.IsChecked", Visualization);
            NoVisualButton.Visibility = G.Vis(!Visualization);
            GlobalInit();
        }
    }
}
