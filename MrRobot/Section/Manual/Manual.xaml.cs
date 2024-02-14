using System;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    public partial class Manual : UserControl
    {
        public Manual()
        {
            G.Manual = this;
            MainMenu.Init += Init;
        }

        void Init(int id)
        {
            if (id != (int)SECT.Manual)
                return;

            InitializeComponent();

            MainMenu.Init -= Init;
            G.SectionInited(id);
        }
    }
}
