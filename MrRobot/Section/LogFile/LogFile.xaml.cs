using System.IO;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
	public partial class LogFile : UserControl
	{
		public LogFile() => G.LogFile = this;

		public void Init()
		{
			InitializeComponent();
			MainMenu.Changed += FileRead;
		}

		public void FileRead(object s, RoutedEventArgs e) => FileRead();
		void FileRead()
		{
			if (position.MainMenu() != 7)
				return;

			var file = new StreamReader("log.txt");

			string content = "";
			string line;
			while ((line = file.ReadLine()) != null)
				content += $"{line}\n";

			file.Close();

			Log1.Text = content;
			Log1.ScrollToEnd();
		}
	}
}
