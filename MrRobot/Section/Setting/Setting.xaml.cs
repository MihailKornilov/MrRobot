using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Setting.xaml
    /// </summary>
    public partial class Setting : UserControl
    {
        public Setting()
        {
            G.Setting = this;
            MainMenu.Init += Init;
        }

        void Init(int id)
        {
            if (id != (int)SECT.Setting)
                return;

            InitializeComponent();
            MenuCreate();

            G.SettingEntity.Init();
            G.SettingMain.Init();

            MainMenu.Init -= Init;
            G.SectionInited(id);
        }

        /// <summary>
        /// Разделы меню настроек
        /// </summary>
        string[] MenuUnits = {
                "Общие",
                "Данные",
                "АвтоПрогон"
            };
        /// <summary>
        /// Создание разделов меню настроек
        /// </summary>
        void MenuCreate()
        {
            foreach (string v in MenuUnits)
                SettingMenuBox.Items.Add(new SettingMenuUnit(v));

            SettingMenuBox.SelectionChanged += (s, e) => MenuChange();
            SettingMenuBox.SelectedIndex = 1;
        }
        /// <summary>
        /// Выбран новый раздел меню
        /// </summary>
        void MenuChange()
        {
            var unit = SettingMenuBox.SelectedItem as SettingMenuUnit;
            MenuHead.Text = unit.Name;

            int sel = SettingMenuBox.SelectedIndex;
            for (int i = 0; i < MenuUnits.Length; i++)
            {
                var panel = (FindName($"Setting{i}") as Panel);
                if (panel != null)
                {
                    panel.Visibility = i == sel ? Visibility.Visible : Visibility.Collapsed;
                    continue;
                }

                var uc = (FindName($"Setting{i}") as UserControl);
                uc.Visibility = i == sel ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }



    class SettingMenuUnit
    {
        public SettingMenuUnit(string name) => Name = name;
        public string Name { get; set; }
    }
}

