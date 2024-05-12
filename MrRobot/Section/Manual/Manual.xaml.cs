using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Section
{
    public partial class Manual : UserControl
    {
        public Manual()
        {
			G.Manual = this; 
            InitializeComponent();
        }
    }
}
