using System;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.Entity;
using MrRobot.inc;

namespace MrRobot.Section
{
	/// <summary>
	/// Настройка разделов приложения
	/// </summary>
	public partial class SettingSection : UserControl
	{
		public SettingSection()
		{
			InitializeComponent();

			var tab = new Roster(RodsterSP);
			tab.Col("Имя раздела", 100);
			tab.Col("Текст в меню", 100);
			tab.Col("Заголовок", 200);
		}

		void SectionNew(object sender, RoutedEventArgs e)
		{
			var DLG = new Dialog();
			var name = DLG.Input("Имя раздела:");
			name.Focus();
			var menu = DLG.Input("Текст в меню:");
			var head = DLG.Input("Заголовок:");
			var sort = DLG.Input("Сортировка:");

			DLG.Submit += () =>
			{
				var sql = "INSERT INTO`_section`(" +
							"`name`," +
							"`menu`," +
							"`head`," +
							"`sort`" +
						  ")VALUES(" +
						   $"'{name.Text}'," +
						   $"'{menu.Text}'," +
						   $"'{head.Text}'," +
						   $"{Convert.ToInt32(sort.Text)}" +
						  ")";
				WriteLine(sql);
				my.Main.Query(sql);
			};
		}
	}
}
