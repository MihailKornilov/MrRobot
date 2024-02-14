using MrRobot.inc;
using System.Windows.Controls;

namespace MrRobot.Section
{
    public partial class Manual : UserControl
    {
        public Manual()
        {
            InitializeComponent();
            G.Manual = this;
        }
    }
}
