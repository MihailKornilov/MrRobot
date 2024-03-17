using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using MrRobot.inc;

namespace MrRobot.Entity
{
	public class FastFind
	{
		public delegate void DLGT(string txt = "");
		public DLGT Changed { get; set; }

		TextBox TB;	// Текстовое поле для ввода поиска
		Label LBF,	// Отображение количества найденных записей
			  X;    // Красный крестик для отмены поиска
		
		// Количество найденных записей
		public int Count
		{
			set
			{
				if(TB.Text.Trim().Length == 0)
				{
					LBF.Content = "";
					return;
				}

				LBF.Content = value > 0 ? $"найдено: {value}" : "не найдено";
			}
		}

		public FastFind(Panel panel)
		{
			var SP = new StackPanel();
			SP.Width = 200;
			panel.Children.Add(SP);

			// Поле для ввода текста поиска
			TB = new TextBox();
			TB.Height = 25;
			TB.FontSize = 14;
			TB.Padding = new Thickness(5, 2, 5, 2);
			TB.Margin  = new Thickness(15, 7, 0, 0);
			TB.TextChanged += TBChanged;
			SP.Children.Add(TB);

			// Текст с количеством найденных записей
			LBF = new Label();
			LBF.Width = 100;
			LBF.Foreground = format.RGB("#999999");
			LBF.FontSize = 14;
			LBF.Padding = new Thickness(0);
			LBF.Margin = new Thickness(0, -22, 22, 0);
			LBF.HorizontalAlignment = HorizontalAlignment.Right;
			LBF.HorizontalContentAlignment = HorizontalAlignment.Right;
			SP.Children.Add(LBF);

			// Крестик для отмены поиска
			X = new Label();
			X.Style = Application.Current.Resources["FFCancel"] as Style;
			X.MouseLeftButtonDown += Cancel;
			SP.Children.Add(X);

			TB.Focus();
		}

		/// <summary>
		/// Ввод текста в поиске
		/// </summary>
		void TBChanged(object s, TextChangedEventArgs e)
		{
			string txt = TB.Text.Trim();
			G.Vis(X, txt.Length > 0);
			Changed?.Invoke(txt);
		}

		/// <summary>
		/// Сброс поиска
		/// </summary>
		void Cancel(object s, MouseButtonEventArgs e) =>
			TB.Text = "";
	}
}
