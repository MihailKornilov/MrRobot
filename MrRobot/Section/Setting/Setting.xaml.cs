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
		public Setting() => G.Setting = this;

		public void Init()
		{
			InitializeComponent();
			MenuCreate();

			G.SettingEntity.Init();
			G.SettingMain.Init();
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
			SettingMenuBox.SelectedIndex = position.Val("6.SettingMenu", 1);
		}
		/// <summary>
		/// Выбран новый раздел меню
		/// </summary>
		void MenuChange()
		{
			var unit = SettingMenuBox.SelectedItem as SettingMenuUnit;
			MenuHead.Text = unit.Name;

			int sel = SettingMenuBox.SelectedIndex;
			position.Set("6.SettingMenu", sel);
			for (int i = 0; i < MenuUnits.Length; i++)
			{
				var panel = (FindName($"Setting{i}") as Panel);
				if (panel != null)
				{
					G.Vis(panel, i == sel);
					continue;
				}

				var uc = (FindName($"Setting{i}") as UserControl);
				G.Vis(uc, i == sel);
			}
		}
	}



	class SettingMenuUnit
	{
		public SettingMenuUnit(string name) => Name = name;
		public string Name { get; set; }
	}
}

