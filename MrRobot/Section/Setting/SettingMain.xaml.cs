using System.Windows.Controls;

using MrRobot.inc;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для SettingMain.xaml
    /// </summary>
    public partial class SettingMain : UserControl
    {
        public SettingMain()
        {
            G.SettingMain = this;
        }

        public void Init()
        {
            InitializeComponent();
        }

    }
}
