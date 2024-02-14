using MrRobot.Entity;
using System.Windows;
using System.Windows.Controls;

namespace MrRobot
{
    public partial class App : Application
    {
        void MMGo(object sender, RoutedEventArgs e) => MainMenu.Go((sender as Button).TabIndex);
    }
}
