using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using RobotLib;
using MrRobot.inc;

namespace MrRobot.Entity.Elem
{
	public class Dialog
	{
		Grid GRID { get; set; }
		Back BACK { get; set; }
		public Dialog()
		{
			// Главный Grid
			GRID = new Grid();
			GRID.Width  = 400;
			GRID.Height = 300;
			GRID.Background = format.RGB("#FFFFFF");

			// Три строки
			var row = new RowDefinition();
			row.Height = new GridLength(35);
			GRID.RowDefinitions.Add(row);
			
			row = new RowDefinition();
			GRID.RowDefinitions.Add(row);

			row = new RowDefinition();
			row.Height = new GridLength(45);
			GRID.RowDefinitions.Add(row);




			// Заголовок
			var SP = new StackPanel();
			SP.Background = format.RGB("#FFFFE0");
			Grid.SetRow(SP, 0);
			GRID.Children.Add(SP);

			var sep = new Separator();
			sep.Height = 1;
			sep.Margin = new Thickness(0);
			sep.Background = format.RGB("#EEEECC");
			sep.VerticalAlignment = VerticalAlignment.Bottom;
			Grid.SetRow(sep, 0);
			GRID.Children.Add(sep);




			// Нижние кнопки
			SP = new StackPanel();
			SP.Background = format.RGB("#F0F0F0");
			var WP = new WrapPanel();
			WP.HorizontalAlignment = HorizontalAlignment.Center;
			WP.Children.Add(Submit());
			WP.Children.Add(Cancel());
			SP.Children.Add(WP);
			Grid.SetRow(SP, 2);
			GRID.Children.Add(SP);

			sep = new Separator();
			sep.Height = 1;
			sep.Margin = new Thickness(0);
			sep.Background = format.RGB("#DDDDDD");
			sep.VerticalAlignment = VerticalAlignment.Top;
			Grid.SetRow(sep, 2);
			GRID.Children.Add(sep);







			Grid.SetColumn(GRID, 0);
			Grid.SetColumnSpan(GRID, 2);
			Panel.SetZIndex(GRID, 5);
			G.MainGrid.Add(GRID);

			BACK = new Back();
			BACK.Method += DialogClose;
		}

		Button Submit()
		{
			var but = new Button();
			but.Content = "Внести раздел";
			but.MinWidth = 50;
			but.Margin  = new Thickness( 0,10, 20, 0);
			but.Padding = new Thickness(10, 4, 10, 4);
			return but;
		}
		TextBlock Cancel() { 
			var tb = new TextBlock();
			tb.Text = "Отмена";
			tb.Margin = new Thickness(0, 15, 0, 0);
			tb.Cursor = Cursors.Hand;
			tb.MouseLeftButtonDown += (s, e) =>
			{
				DialogClose();
				BACK.Hide();
			};
			return tb;
		}


		void DialogClose() =>
			G.MainGrid.Remove(GRID);
	}
}
