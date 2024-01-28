using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public partial class CDIselectLink : UserControl
    {
        public CDIselectLink()
        {
            InitializeComponent();
        }

        void OpenPanel(object sender, MouseButtonEventArgs e)
        {
            var win = global.MW.PointToScreen(new Point(0, 0));
            var el = TBLink.PointToScreen(new Point(0, 0));
            int left = (int)(el.X - win.X) - 64;
            int top = (int)(el.Y - win.Y) + 20;

            CDIpanel.Open(left, top);
        }
    }
}
