using System.Windows;
using System.Windows.Controls;

using MrRobot.inc;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для SettingMain.xaml
    /// </summary>
    public partial class SettingMain : UserControl
    {
        public SettingMain() => G.SettingMain = this;

        public void Init()
        {
            InitializeComponent();
			StartWinNoClose.IsChecked = position.Val("6.1.StartWinCheck", false);
			StartWinNoClose.Checked   += StartWinCheck;
			StartWinNoClose.Unchecked += StartWinCheck;
			SqlLog.IsChecked = my.IS_LOG;
			SqlLog.Checked   += SqlLogCheck;
			SqlLog.Unchecked += SqlLogCheck;
		}

		void StartWinCheck(object s, RoutedEventArgs e) =>
            position.Set("6.1.StartWinCheck", (bool)StartWinNoClose.IsChecked);
		void SqlLogCheck(object s, RoutedEventArgs e) =>
			position.Set("6.1.SqlLog", my.IS_LOG = (bool)SqlLog.IsChecked);
	}
}
