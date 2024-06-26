﻿using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using static System.Console;

using CefSharp;
using CefSharp.Wpf;

using RobotLib;
using MrRobot.Section;
using MrRobot.Entity;

namespace MrRobot.inc
{
	public class G
	{
		public static Dur dur { get; set; }
		public G(MainWindow mw)
		{
			MW = mw;
			dur = new Dur();
			Settings();
		}

		// Глобальные установки
		void Settings()
		{
			// Формат даны в виде 03.12.2023 (для календаря)
			var dtInfo = new DateTimeFormatInfo()
			{
				ShortDatePattern = "dd.MM.yyyy",
				ShortTimePattern = "hh:mm:ss tt",
				DateSeparator = ".",
				TimeSeparator = ":"
			};
			// Точка вместо запятой в дробных числах
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us")
			{
				DateTimeFormat = dtInfo
			};
		}

		// Установка каталога кеша для Веб-браузера
		public static void CefSettings()
		{
			var Settings = new CefSettings();
			Settings.CachePath = Directory.GetCurrentDirectory() + @"\CefSharpCache";
			Cef.Initialize(Settings);
		}



		// Флаг запущенного АвтоПрогона
		public static bool IsAutoProgon => AutoProgon.Active;

		/// <summary>
		/// Запись данных в log-файл
		/// </summary>
		public static void LogWrite(string txt = "", string fileName = "log.txt")
		{
			FileStream file;
			try
			{
				file = new FileStream(fileName, FileMode.OpenOrCreate);
			}
			catch
			{
				return;
			}
			txt = txt.Length > 0 ? $"{format.DTimeNow()}: {txt}\n" : file.Length == 0 ? "" : "\n";
			byte[] buffer = Encoding.UTF8.GetBytes(txt);
			file.Seek(0, SeekOrigin.End);
			file.Write(buffer, 0, buffer.Length);
			file.Close();
		}


		public static Visibility Vis(bool isVis = true) =>
			isVis ? Visibility.Visible : Visibility.Collapsed;
		public static void Vis(FrameworkElement elem) =>
			elem.Visibility = Visibility.Visible;
		public static Visibility Vis(UserControl uc, bool isVis = true) =>
			uc != null ? uc.Visibility = Vis(isVis) : Vis(false);
		public static Visibility Vis(FrameworkElement elem, bool isVis = true) =>
			elem != null ? elem.Visibility = Vis(isVis) : Vis(false);
		public static void Hid(FrameworkElement elem) =>
			elem.Visibility = Visibility.Collapsed;


		// Программное нажатие на кнопку
		public static void ButtonClick(Button but) =>
			but.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));


		// ---=== Entities приложения ===---
		public static Exchange Exchange { get; set; }


		// Первый Grid главного окна
		public static UIElementCollection MainGrid =>
			(MW.Content as Grid).Children;
		public static UIElementCollection Grid0 =>
			(MainGrid[0] as Grid).Children;
		public static UIElementCollection Grid1 =>
			(MainGrid[1] as Grid).Children;

		// ---=== UserControl's всего приложения ===---
		public static MainWindow MW     { get; set; }

		public static History History   { get; set; }
		public static HistoryMoex HistoryMoex { get; set; }
		public static Converter Converter { get; set; }
		public static Pattern Pattern   { get; set; }
		public static PatternArchive PatternArchive { get; set; }
		public static Tester Tester     { get; set; }
		public static Trade Trade       { get; set; }
		public static Setting Setting   { get; set; }
		public static SettingEntity SettingEntity { get; set; }
		public static SettingMain SettingMain { get; set; }
		public static LogFile LogFile   { get; set; }
		public static Manual Manual     { get; set; }


		public static CDIselectPanel CDIselectPanel { get; set; }
		public static InstrumentSelect ISPanel { get; set; }

		public static WssVisual WssVisual { get; set; }
		public static ObtPlay ObtPlay { get; set; }
	}




	/// <summary>
	/// Измерение скорости выполнения участка программы
	/// </summary>
	public class Dur
	{
		Stopwatch SW;
		public Dur()
		{
			SW = new Stopwatch();
			SW.Start();
		}
		public string Minutes()
		{
			SW.Stop();
			TimeSpan ts = SW.Elapsed;

			if(ts.Hours > 0)
				return string.Format("{0}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

			return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
		}
		public string Second()
		{
			SW.Stop();
			TimeSpan ts = SW.Elapsed;
			return string.Format("{0}:{1:000}", ts.Seconds, ts.Milliseconds);
		}
		/// <summary>
		/// Вывод результата в секундах и миллисекундах в Консоль
		/// </summary>
		public void SecondCWL(string msg="") =>
			WriteLine(msg + Second());

		public int TotalSeconds() =>
			(int)(SW.ElapsedMilliseconds / 1000);
		public long ElapsedMS => SW.ElapsedMilliseconds;



		/// <summary>
		/// Статическая схема
		/// </summary>
		static Dur dur;
		public static Dur Start() => dur = new Dur();
		public static string Interval(bool isWL = false)
		{
			TimeSpan ts = dur.SW.Elapsed;
			string elaps = string.Format("{0}:{1:000}", ts.Seconds, ts.Milliseconds);
			if(isWL)
				WriteLine(elaps);
			return elaps;
		}
		public static string Stop(bool isWL = false)
		{
			string elaps = Interval(isWL);
			dur.SW.Stop();
			return elaps;
		}
	}




	/// <summary>
	/// Статический класс для вывода сообщений об ошибке в нижней строке приложения
	/// </summary>
	public static class error
	{
		/// <summary>
		/// Показ сообщения об ошибке в нижней строке приложения
		/// </summary>
		public static void Msg(string txt)
		{
		}
	}




	/// <summary>
	/// Обновление значения прогресс-бара
	/// </summary>
	public class ProBar
	{
		double All { get; set; }        // Общее количество
		double StepCount { get; set; }  // Количество шагов, в каждый из которых будут обновляться данные прогресс-бара
		double Sotka { get; set; }      // Сотая часть от общей суммы
		double Area;            // Участок, при завершении которого обновляется процент ПрогрессБара
		decimal Percent;         // Значение в процентах, которое будет передаваться в Value
		decimal PercentStep;     // Значение, на которое увеличивается процент при следующем шаге
		public decimal Value { get; private set; }  // Значение в процентах, которое будет выводиться в Прогресс-бар

		Dur dur = new Dur();
		double MilliSecondPass { get; set; }
		double MilliSecondLeft { get; set; }
		public string TimePass {
			get
			{
				var pass = TimeSpan.FromMilliseconds(MilliSecondPass);
				if (pass.Hours > 0)
					return string.Format("{0}:{1:00}:{2:00}", pass.Hours, pass.Minutes, pass.Seconds);

				return string.Format("{0}:{1:00}", pass.Minutes, pass.Seconds);
			}
		}
		public string TimeLeft {
			get
			{
				var left = TimeSpan.FromMilliseconds(MilliSecondLeft);
				if (left.Hours > 0)
					return string.Format("{0}:{1:00}:{2:00}", left.Hours, left.Minutes, left.Seconds);

				return string.Format("{0}:{1:00}", left.Minutes, left.Seconds);
			}
		}

		public ProBar(double all, double stepCount = 100)
		{
			if (all == 0)
				all = 1;
			if (stepCount > all)
				stepCount = all;
			All = all;
			StepCount = stepCount;
			Sotka = All / StepCount;
			Area = Sotka;
			PercentStep = 100 / (decimal)StepCount;
		}

		/// <summary>
		/// Проверка, обновлять ли Прогресс-бар (при All >= StepCount)
		/// </summary>
		public bool isUpd(long count)
		{
			if (count == 0)
				return false;
			if (count < Area)
				return false;

			Area += Sotka;
			Value = Math.Round(Percent, format.Round(StepCount));
			Percent += PercentStep;

			MilliSecondPass = dur.ElapsedMS;
			MilliSecondLeft = MilliSecondPass / count * All - MilliSecondPass;

			return true;
		}

		public bool Val(long count, IProgress<decimal> Progress)
		{
			if (!isUpd(count))
				return false;

			Progress.Report(Value);
			return true;
		}
	}


	/// <summary>
	/// Список главных разделов приложения
	/// </summary>
	enum SECT
	{
		History = 1,
		Converter,
		Pattern,
		Tester,
		Trade,
		Setting,
		LogFile,
		Manual
	}
}
