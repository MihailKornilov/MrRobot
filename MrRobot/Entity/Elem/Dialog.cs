using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using RobotLib;
using MrRobot.inc;

namespace MrRobot.Entity
{
	public class Dialog
	{
		public delegate void SUBMIT();
		public SUBMIT Submit { get; set; }


		Grid GRID { get; set; }
		Back BACK { get; set; }
		StackPanel CNT { get; set; }	// Центральное содержание


		public string HeadTxt { get; set; } = "Внесение";
		public string ButSubmitTxt { get; set; } = "Внести";

		public Dialog()
		{
			// Главный Grid
			GRID = new Grid();
			GRID.Width = 400;
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
			var txt = new TextBlock();
			txt.FontSize = 15;
			txt.Foreground = format.RGB("#555555");
			txt.Margin = new Thickness(10, 7, 0, 0);
			txt.Text = HeadTxt;
			SP.Children.Add(txt);
			Grid.SetRow(SP, 0);
			GRID.Children.Add(SP);

			var sep = new Separator();
			sep.Height = 1;
			sep.Margin = new Thickness(0);
			sep.Background = format.RGB("#EEEECC");
			sep.VerticalAlignment = VerticalAlignment.Bottom;
			Grid.SetRow(sep, 0);
			GRID.Children.Add(sep);






			// Содержание
			CNT = new StackPanel();
			CNT.Margin = new Thickness(0, 20, 0, 20);
			Grid.SetRow(CNT, 1);
			GRID.Children.Add(CNT);








			// Нижние кнопки
			SP = new StackPanel();
			SP.Background = format.RGB("#F0F0F0");
			var WP = new WrapPanel();
			WP.HorizontalAlignment = HorizontalAlignment.Center;
			WP.Children.Add(ButSubmit());
			WP.Children.Add(ButCancel());
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

		Button ButSubmit()
		{
			var but = new Button();
			but.Content = ButSubmitTxt;
			but.MinWidth = 50;
			but.Margin  = new Thickness(0, 10, 20, 0);
			but.Padding = new Thickness(10, 4, 10, 4);
			but.Click += (s, e) => 
			{
				Submit?.Invoke();
				DialogClose();
				BACK.Hide();
			};
			return but;
		}
		TextBlock ButCancel()
		{
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


		public TextBox Input(string about = "")
		{
			var WP = new WrapPanel();

			var lb = new Label();
			lb.Width = 120;
			lb.HorizontalContentAlignment = HorizontalAlignment.Right;
			lb.Foreground = format.RGB("#777777");
			lb.Margin = new Thickness(0, 5, 4, 0);
			lb.Content = about;
			WP.Children.Add(lb);

			var tb = new TextBox();
			tb.Width = 220;
			tb.Margin  = new Thickness(0, 5, 0, 5);
			tb.Padding = new Thickness(4);
			WP.Children.Add(tb);

			CNT.Children.Add(WP);

			return tb;
		}
	}
}
