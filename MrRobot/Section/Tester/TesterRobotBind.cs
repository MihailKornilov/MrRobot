using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static System.Console;

using static RobotAPI.Robot;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using System.Windows.Markup;

namespace MrRobot.Section
{
	public partial class Tester : UserControl
	{
		/// <summary>
		/// Установка выбранного робота
		/// </summary>
		object ObjInstance { get; set; }
		MethodInfo Init;
		MethodInfo Step;
		MethodInfo Finish;
		int RobotId;        // Сохранение ID выбранного робота, чтобы потом не выбирать того же
		bool Visualization;             // Флаг включенной визуализации
		/// <summary>
		/// Установка выбранного инструмента
		/// </summary>
		const double BaseBalance = 0;
		const double QuoteBalance = 100;
		int CandleId;
		CDIparam InitParam;     // Для фоновой загрузки свечных данных

		bool RobotApply()
		{
			if (RobotsListBox.SelectedIndex <= 0)
				return false;

			var robot = RobotsListBox.SelectedItem as RobotUnit;

			if (RobotId == robot.Id)
				return true;

			if (!File.Exists(robot.Path))
			{
				RobotsListBox.SelectedIndex = 0;
				error.Msg("Отсутствует DLL-файл по указанному пути.");
				return false;
			}

			Assembly ASML = Assembly.LoadFrom(robot.Path);
			Type type = ASML.GetType(robot.Name);
			ObjInstance = Activator.CreateInstance(type);
			Init = type.GetMethod("Init");
			Step = type.GetMethod("Step");
			Finish = type.GetMethod("Finish");

			RobotId = robot.Id;

			return true;
		}

		void InstrumentSet()
		{
			var item = CDIpanel.CdiUnit();
			INSTRUMENT = BYBIT.Instrument.Unit(item.InstrumentId);
			INSTRUMENT.CdiId = item.Id;

			INSTRUMENT.BaseBalance = BaseBalance;
			INSTRUMENT.QuoteBalance = QuoteBalance;
			INSTRUMENT.BaseCommiss = 0;
			INSTRUMENT.QuoteCommiss = 0;

			// Если свечные данные не менялись, то загружаться из базы не будут
			if(CandleId != item.Id)
			{
				CANDLES_DATA = null;
				CANDLES_TF1_DATA = null;
			}

			CandleId = item.Id;

			BaseBalanceCoin.Content = INSTRUMENT.BaseCoin;
			QuoteBalanceCoin.Content = INSTRUMENT.QuoteCoin;
		}
		/// <summary>
		/// Загрузка свечных данных
		/// </summary>
		async void CandlesDataLoad()
		{
			if (CANDLES_DATA != null && CANDLES_TF1_DATA != null)
				return;

			InitParam.Id = CandleId;
			InitParam.IsProcess = true;
			SetupGrid.IsEnabled = false;
			G.Vis(CDIdownloadPanel);
			CDIdownload.Text = "";

			var CDI = Candle.Unit(CandleId);
			if(CANDLES_DATA == null)
				await Task.Run(() =>
				{
					var bar = new ProBar(CDI.RowsCount);
					string sql = "SELECT*" +
								$"FROM`{INSTRUMENT.Table}`" +
								 "ORDER BY`unix`";
					CANDLES_DATA = new List<object>();
					my.Main.Delegat(sql, row =>
					{
						CANDLES_DATA.Add(new CandleUnit(row));
						bar.Val(CANDLES_DATA.Count, InitParam.Progress);
					});
				});

			if (CANDLES_TF1_DATA == null)
				if ((bool)UseTF1Check.IsChecked)
					if (CDI.ConvertedFromId > 0)
					{
						var TF1 = Candle.Unit(CDI.ConvertedFromId);
						InitParam.Id = TF1.Id;
						await Task.Run(() =>
						{
							var bar = new ProBar(TF1.RowsCount);
							string sql = "SELECT*" +
										$"FROM`{TF1.Table}`" +
										 "ORDER BY`unix`";
							CANDLES_TF1_DATA = new List<object>();
							my.Main.Delegat(sql, row =>
							{
								CANDLES_TF1_DATA.Add(new CandleUnit(row));
								bar.Val(CANDLES_TF1_DATA.Count, InitParam.Progress);
							});
						});
					}

			SetupGrid.IsEnabled = true;
			G.Hid(CDIdownloadPanel);
			InitParam.IsProcess = false;
		}

		void CandlesTF1use()
		{
			CANDLES_TF1_USE = false;

			if (CDIpanel.CdiUnit().ConvertedFromId == 0)
				return;
			if (!(bool)UseTF1Check.IsChecked)
				return;

			CANDLES_TF1_USE = CANDLES_TF1_DATA != null;
		}


		public async void GlobalInit()
		{
			PanelVisible();
			AutoGoStop();
			RobotSetupList.ItemsSource = null;
			RobotLogList.Items.Clear();
			OrderExecuted.ItemsSource = null;

			if (!RobotApply())
				return;

			InstrumentSet();

			InitParam = new CDIparam {
				IsProcess = false,
				Progress = new Progress<decimal>(v => { CDIdownload.Text = $"{v}%"; })
			};
			CandlesDataLoad();
			await Task.Run(() => { while (InitParam.IsProcess) Thread.Sleep(300); });

			CandlesTF1use();
			BalanceUpdate();
			new PATTERN(Patterns.ListAll());

			TESTER_GLOBAL_INIT();

			LOGG.Method = RobotLog;
			Init.Invoke(ObjInstance, new object[] { new string[]{} });
			LOGG.Output();

			G.Vis(RobotSetupButton, SETUP.Items.Count > 0);
			OrderExecutedView();
			TesterChartInit();
			TesterBar.Value = 0;

			AutoProgon.RobotStart();
		}
		/// <summary>
		/// Скрытие/отображение панелей
		/// </summary>
		void PanelVisible()
		{
			bool hide = RobotsListBox.SelectedIndex <= 0;
			RobotSetupButton.Visibility = Visibility.Collapsed;
			BalancePanel.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
			VisualPanel.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
			ProcessPanel.Visibility = hide || !Visualization ? Visibility.Collapsed : Visibility.Visible;
		}
		void TesterChartInit()
		{
			var item = CDIpanel.CdiUnit();

			if (!Visualization)
			{
				// Отображение пустой страницы вместо графика
				EChart.HeadOnly(item);
				return;
			}
			EChart.TesterGraficInit(item);
		}
		void BalanceUpdate()
		{
			BaseBalanceSum.Content  = format.Coin(INSTRUMENT.BaseBalance);
			QuoteBalanceSum.Content = format.Coin(INSTRUMENT.QuoteBalance);
			BaseCommissSum.Content  = format.Coin(INSTRUMENT.BaseCommiss);
			QuoteCommissSum.Content = format.Coin(INSTRUMENT.QuoteCommiss);
		}

		/// <summary>
		/// Открытие окна с настройками робота
		/// </summary>
		void RobotSetupOpen(object sender, RoutedEventArgs e)
		{
			if (SETUP.Items.Count == 0)
				return;

			new GridBack(RobotSetupPanel);

			if (RobotSetupList.Items.Count > 0)
				return;

			RobotSetupList.ItemsSource = SETUP.Items;
		}
		/// <summary>
		/// Изменение слайдера в Настройке робота
		/// </summary>
		void RobotSetupSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var slider = sender as Slider;
			var parent = slider.Parent as WrapPanel;
			var tb = parent.FindName("RSsliderTB") as TextBox;
			tb.Text = slider.Value.ToString();
		}


		#region AutoGo

		DispatcherTimer AutoGoTimer;
		/// <summary>
		/// Запущено или нет автоматическое тестирование
		/// </summary>
		bool AutoGoStatus()
		{
			return AutoGoTimer != null;
		}
		/// <summary>
		/// Остановка автоматического тестирования
		/// </summary>
		void AutoGoStop()
		{
			if (!AutoGoStatus())
				return;

			AutoGoTimer.Stop();
			AutoGoTimer = null;
			AutoGoButtonStatus(false);
		}
		/// <summary>
		/// Запуск автоматического тестирования - нажатие на кнопку
		/// </summary>
		void AutoGo(object sender, RoutedEventArgs e)
		{
			if (!Visualization)
				return;

			if (TESTER_FINISHED)
				return;

			if (AutoGoStatus())
			{
				AutoGoStop();
				BalanceUpdate();
				LOGG.Output();
				OrderExecutedView();
				return;
			}

			AutoGoTimer = new DispatcherTimer();
			AutoGoTimer.Interval = TimeSpan.FromMilliseconds(SliderV());
			AutoGoTimer.Tick += AutoGoTick;
			AutoGoTimer.Start();
			AutoGoButtonStatus();
			AutoGoSlider.Focus();
		}
		void AutoGoTick(object sender, EventArgs e) => TesterRobotStep();
		void AutoGoSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			position.Set("4_TesterSlider.Value", AutoGoSlider.Value.ToString());

			if (!AutoGoStatus())
				return;

			AutoGoTimer.Interval = TimeSpan.FromMilliseconds(SliderV());
		}
		/// <summary>
		/// Значения слайдера для изменения скорости движения графика во время тестирования
		/// </summary>
		double SliderV()
		{
			Dictionary<double, double> ass = new Dictionary<double, double>();

			ass.Add(0, 1000);
			ass.Add(1, 850);
			ass.Add(2, 600);
			ass.Add(3, 350);
			ass.Add(4, 200);
			ass.Add(5, 150);
			ass.Add(6, 100);
			ass.Add(7, 55);
			ass.Add(8, 33);
			ass.Add(9, 17);
			ass.Add(10, 0);

			return ass[AutoGoSlider.Value];
		}
		/// <summary>
		/// Изменение состояния кнопки запуска теста
		/// </summary>
		void AutoGoButtonStatus(bool go = true)
		{
			CandleAddButton.Visibility = go ? Visibility.Hidden : Visibility.Visible;
			AutoGoButton.Content = go ? "Стоп" : "Старт";
			TESTER_AUTO = go;

			if(go)
			{
				AutoGoButton.Background = new SolidColorBrush(Color.FromArgb(255, 0xCC, 0x6F, 0x6F)); // CC6F6F
				AutoGoButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x7B, 0x1C, 0x1C)); // 7B1C1C
				return;
			}

			AutoGoButton.Background = new SolidColorBrush(Color.FromArgb(255, 0x1A, 0xA5, 0x73)); // 1AA573
			AutoGoButton.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x0A, 0x42, 0x2E)); // 0A422E
		}

		#endregion



		/// <summary>
		/// Добавление очередной свечи в график - нажатие на кнопку
		/// </summary>
		void CandleAdd(object sender, RoutedEventArgs e) => TesterRobotStep();
		/// <summary>
		/// Очередной шаг: добавление новой свечи
		/// </summary>
		void TesterRobotStep()
		{
			if (!TESTER_GLOBAL_STEP())
				return;

			// Остановка авто-теста со стороны робота
			if (!TESTER_AUTO && AutoGoStatus())
			{
				AutoGoStop();
				BalanceUpdate();
				LOGG.Output();
				OrderExecutedView();
				return;
			}

			// Выполнение очередного шага в Роботе
			Step.Invoke(ObjInstance, new object[] { });

			// Обновление прогресс-бара над графиком
			TesterBar.Value = (double)CANDLES_COUNT / (double)INSTRUMENT.RowsCount * 100;

			// Отображение количества свечей в заголовке графика
			EChart.CandleCount(CANDLES_COUNT);

			// Отображение даты последней свечи в заголовке графика
			EChart.Right(DATE_TIME);

			// Вставка очередной свечи в график
			EChart.Script($"candles.update({CANDLES().CandleToChart()})");

			if (!AutoGoStatus())
			{
				BalanceUpdate();
				LOGG.Output();
				OrderExecutedView();
			}
 
			RobotLine();

			if (!TESTER_FINISHED)
				return;

			AutoGoStop();
			Finish?.Invoke(ObjInstance, new object[] { });
			BalanceUpdate();
			LOGG.Output();
			OrderExecutedView();
		}




		#region NO VISUAL

		bool IsNoVisualProcess { get; set; } // Флаг процесса тестирования без визуализации
		/// <summary>
		/// Блокировка полей настроек перед запуском теста
		/// </summary>
		void NoVisualLock(string ButtonContent = "", int ButtonWidth = 0)
		{
			bool unlock = ButtonWidth > 0;
			NoVisualButton.Content = unlock ? ButtonContent : "Остановить";
			NoVisualButton.Width = unlock ? ButtonWidth : 80;
			CDIpanel.Lock = unlock;
			SetupGrid.IsEnabled = unlock;
			UseTF1Check.IsEnabled = unlock;
			VisualCheck.IsEnabled = unlock;
			IsNoVisualProcess = !unlock;
		}
		/// <summary>
		/// Тестирование без визуализации - нажатие на кнопку
		/// </summary>
		async void NoVisualStart(object sender, RoutedEventArgs e)
		{
			string ButtonContent = NoVisualButton.Content.ToString();
			int ButtonWidth = (int)NoVisualButton.Width;

			// Остановка тестирования
			if (!TESTER_FINISHED && IsNoVisualProcess)
			{
				NoVisualLock(ButtonContent, ButtonWidth);
				return;
			}

			NoVisualLock();

			if(!G.IsAutoProgon)
				GlobalInit();

			var progress = new Progress<decimal>(v => { TesterBar.Value = (double)v; });
			await Task.Run(() => NoVisualProcess(progress));
			object res = Finish.Invoke(ObjInstance, new object[] { });

			BalanceUpdate();
			LOGG.Output();
			OrderExecutedView();

			NoVisualLock(ButtonContent, ButtonWidth);

			AutoProgon.RobotTest();
		}
		void NoVisualProcess(IProgress<decimal> Progress)
		{
			var bar = new ProBar(INSTRUMENT.RowsCount);

			while (TESTER_GLOBAL_STEP() && IsNoVisualProcess)
			{
				// Выполнение очередного шага в Роботе
				Step.Invoke(ObjInstance, new object[] { });
				bar.Val(CANDLES_COUNT, Progress);
			}
			Progress.Report(100);
		}

		#endregion



		/// <summary>
		/// Вывод информации в лог Тестера
		/// </summary>
		void RobotLog(List<RobotAPI.LogUnit> list)
		{
			foreach(var unit in list)
				RobotLogList.Items.Add(unit);

			int count = list.Count - 1;
			RobotLogList.ScrollIntoView(list[count]);
		}

		/// <summary>
		/// Рисование линии покупки/продажи на графике
		/// </summary>
		void RobotLine()
		{
			string[] line = LineGet();

			if (line == null)
				return;
			if (line.Length == 0)
				return;

			string script = "";
			for(int i = 0; i < line.Length; i++)
			{
				string[] spl = line[i].Split(';');
				string type = spl[0].ToUpper();

				script += $"Line{type}.price={spl[1]};candles.createPriceLine(Line{type});";
			}

			EChart.Script(script);
		}

		/// <summary>
		/// Вывод исполненных SPOT ордеров
		/// </summary>
		void OrderExecutedView()
		{
			OrderExecuted.ItemsSource = null;

			if (ORDERS.Count == 0)
				return;

			OrderExecuted.ItemsSource = ORDERS;

			int c = ORDERS.Count - 1;
			OrderExecuted.ScrollIntoView(ORDERS[c]);
		}
	}
}
