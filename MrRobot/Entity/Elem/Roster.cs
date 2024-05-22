using System.Windows;
using System.Windows.Controls;

using RobotLib;

namespace MrRobot.Entity
{
	/// <summary>
	/// Список-таблица с данными и заголовками
	/// </summary>
	public class Roster
	{
		WrapPanel WP { get; set; }
		public Roster(StackPanel SP)
		{
			WP = new WrapPanel();
			WP.Background = format.RGB("#EEEEEE");
			
			SP.Margin = new Thickness(3);
			SP.Children.Add(WP);
			SP.Children.Add(Sep());
		}


		public void Col(string cnt, int w)
		{
			WP.Children.Add(LBL(cnt, w));
		}

		Label LBL(string cnt, int w)
		{
			var lb = new Label();
			lb.Content = cnt;
			lb.Width = w;
			lb.FontSize = 11;
			lb.Foreground = format.RGB("#777777");
			return lb;
		}

		Separator Sep() {
			var sep = new Separator();
			sep.Height = 1;
			sep.Margin = new Thickness(0);
			sep.Background = format.RGB("#CCCCCC");
			sep.VerticalAlignment = VerticalAlignment.Top;
			return sep;
		}
	}
}
