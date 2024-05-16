using System.Windows;
using System.Windows.Controls;

using RobotLib;
using MrRobot.inc;

namespace MrRobot.Entity
{
	/// <summary>
	/// Задний фон для элементов поверх всего
	/// </summary>
	public class Back
	{
		public delegate void VOID();
		public VOID Method { get; set; }

		Grid grid;
		public Back()
		{
			grid = new Grid();
			grid.Background = format.RGB("#000000");
			grid.Opacity = 0.5;
			grid.MouseLeftButtonDown += (s, e) =>
			{
				Method?.Invoke();
				Hide();
			};
			Grid.SetColumn(grid, 0);
			Grid.SetColumnSpan(grid, 2);
			Panel.SetZIndex(grid, 2);
			G.MainGrid.Add(grid);
		}
		public void Hide() =>
			G.MainGrid.Remove(grid);
	}


	/// <summary>
	/// Задний фон для выпадающего списка
	/// </summary>
	class GridBack
	{
		delegate void Dcall();
		static Dcall GBremove;

		public GridBack(FrameworkElement elem)
		{
			elem.Visibility = Visibility.Visible;

			var border = elem as Border;

			var grid = new Grid();
			grid.Background = format.RGB("#888888");
			grid.Opacity = 0.05;
			grid.MouseLeftButtonDown += (s, ee) =>
			{
				(grid.Parent as Panel).Children.Remove(grid);
				G.Hid(border);
			};
			Grid.SetRow(grid, 0);
			Grid.SetRowSpan(grid, 5);
			(border.Parent as Panel).Children.Add(grid);
		}

		public GridBack(InstrumentSelect panel) => Create(panel.Parent as Panel, panel.OpenPanel);
		public GridBack(CDIselectPanel panel) => Create(panel.Parent as Panel, panel.OpenPanel);
		void Create(Panel panel, Border border)
		{
			var grid = new Grid();
			grid.Background = format.RGB("#888888");
			grid.Opacity = 0.05;
			GBremove += () => {
				panel.Children.Remove(grid);
				G.Hid(border);
				GBremove = null;
			};
			grid.MouseLeftButtonDown += (s, e) => GBremove();
			Grid.SetColumn(grid, 0);
			Grid.SetColumnSpan(grid, 2);
			panel.Children.Add(grid);
		}
		public static void Remove()
		{
			if (GBremove != null)
				GBremove();
		}
	}

}
