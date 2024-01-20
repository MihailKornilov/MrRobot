using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.Entity;
using MrRobot.inc;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Tester.xaml
    /// </summary>
    public partial class Tester : UserControl
    {
        public Tester()
        {
            InitializeComponent();
            TesterInit();
        }

        public void TesterInit()
        {
            if (global.IsInited(4))
                return;

            InstrumentListBox.ItemsSource = Candle.ListAll();
            InstrumentListBox.SelectedIndex = position.Val("4_InstrumentListBox_SelectedIndex", 0);

            RobotsListBox.ItemsSource = Robots.ListBox();
            RobotsListBox.SelectedIndex = 0;
            RobotAddButton.Click += Robots.Load;

            Visualization = position.Val("4_VisualCheck.IsChecked", true);
            VisualCheck.IsChecked = Visualization;
            NoVisualButton.Visibility = Visualization ? Visibility.Hidden : Visibility.Visible;
            AutoGoSlider.Value = position.Val("4_TesterSlider.Value", 0);

            LogMenu.SelectedIndex = position.Val("4_LogMenu_SelectedIndex", 0);

            global.Inited(4);
        }



        /// <summary>
        /// Выбор нового графика
        /// </summary>
        private void InstrumentListChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = sender as ListBox;
            if (box == null)
                return;

            position.Set("4_InstrumentListBox_SelectedIndex", box.SelectedIndex);
            RobotPanel.Visibility = box.SelectedIndex == -1 ? Visibility.Hidden : Visibility.Visible;
            RobotsListBox.SelectedIndex = 0;

            if (box.SelectedIndex == -1)
                return;

            var item = box.SelectedItem as CandleDataInfoUnit;

            // Прокручивание списка, пока не появится в представлении
            InstrumentListBox.ScrollIntoView(item);

            TesterBrowserShow(item);

            UseTF1Check.Visibility = item.ConvertedFromId == 0 ? Visibility.Collapsed : Visibility.Visible;
        }


        /// <summary>
        /// Показ графика
        /// </summary>
        private void TesterBrowserShow(CandleDataInfoUnit item)
        {
            if (global.IsAutoProgon)
                return;
            if (Candle.Unit(item.Id) == null)
                return;

            TesterBrowser.Address = new Chart("Tester", item.Table).PageHtml;
            TesterChartHead.Update(item);
        }



        /// <summary>
        /// Сохранение позиции Лог-меню
        /// </summary>
        private void LogMenuPosition(object sender, SelectionChangedEventArgs e)
        {
            var tab = sender as TabControl;
            position.Set("4_LogMenu_SelectedIndex", tab.SelectedIndex.ToString());
        }

        /// <summary>
        /// Установка ширины Робот-лога при изменении ширины приложения
        /// </summary>
        public void RobotLogWidthSet(object sender, SizeChangedEventArgs e)
        {
            if (position.MainMenu() != 4)
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
        private void RobotListChanged(object sender, SelectionChangedEventArgs e) => GlobalInit();

        /// <summary>
        /// Нажатие на галочку: Использовать таймфрейм 1m
        /// </summary>
        private void UseTF1Checked(object sender, RoutedEventArgs e) => GlobalInit();

        /// <summary>
        /// Нажатие на галочку: Визуализация
        /// </summary>
        private void VisualChecked(object sender, RoutedEventArgs e)
        {
            if (!global.IsInited())
                return;

            Visualization = (bool)VisualCheck.IsChecked;
            position.Set("4_VisualCheck.IsChecked", Visualization);
            NoVisualButton.Visibility = Visualization ? Visibility.Hidden : Visibility.Visible;
            GlobalInit();
        }
    }
}
