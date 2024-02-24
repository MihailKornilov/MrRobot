﻿using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Console;

using Newtonsoft.Json;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.Interface;

namespace MrRobot.Section
{
	/// <summary>
	/// Логика взаимодействия для History.xaml
	/// </summary>
	public partial class History : UserControl
	{
		public History() => G.History = this;

		public void Init()
		{
			InitializeComponent();

			MenuCreate();
			HeadCountWrite();

			IS = new ISunit(HistoryIS);
			IS.WithHistory = true;
			IS.Changed += InstrumentChanged;
			InstrumentChanged();

			Candle.Updated += DownloadedListCreate;

			G.HistoryMoex.Init();
		}


		ISunit IS { get; set; }
		SpisokUnit IUnit => IS.IUnit;

		/// <summary>
		/// Вывод количества инструментов в заголовке
		/// </summary>
		void HeadCountWrite() =>
			IHeadCount.Text = $"{BYBIT.Instrument.Count} инструмент{format.End(BYBIT.Instrument.Count, "", "а", "ов")}";

		/// <summary>
		/// Выбран инструмент в списке
		/// </summary>
		void InstrumentChanged()
		{
			G.Vis(InfoPanel, IUnit != null);
			G.Vis(DownloadPanel, IUnit != null);

			if (IUnit == null)
				return;

			ByBitInstrumentPrecision.Text = format.E(IUnit.BasePrecision);
			ByBitInstrumentMinOrder.Text = format.E(IUnit.MinOrderQty);
			ByBitInstrumentTickSize.Text = format.E(IUnit.TickSize);
			ByBitInstrumentHistoryBegin.Text = IUnit.HistoryBegin;

			DownloadedListCreate();

			string[] data = IUnit.HistoryBegin.Split(' ');
			string[] sp = data[0].Split('.');
			if (sp.Length < 3)
				return;
			if (sp[2] == "0000")
				return;

			int year = int.Parse(sp[2]);
			int mon = int.Parse(sp[1]);
			int day = int.Parse(sp[0]);
			SetupDateBegin.SelectedDate = new DateTime(year, mon, day);
		}





		#region Download Process

		CDIparam PARAM;

		/// <summary>
		/// Установка UNIX-даты окончания загрузки
		/// </summary>
		int UnixFinish()
		{
			var item = SetupPeriod.SelectedItem as ComboBoxItem;
			if (item.TabIndex > 0)
				return format.UnixFromDay(SetupDateBegin.Text) + item.TabIndex * 24 * 60 * 60;

			// По сегодняшний день
			return format.UnixNow();
		}

		/// <summary>
		/// Старт загрузки истории
		/// </summary>
		async void DownloadGo(object sender, RoutedEventArgs e)
		{
			// Таймфрейм
			var TFitem = SetupTimeFrame.SelectedItem as ComboBoxItem;

			PARAM = new CDIparam()
			{
				Symbol = IUnit.Symbol,
				TimeFrame = format.TimeFrame((string)TFitem.Content),
				UnixStart = format.UnixFromDay(SetupDateBegin.Text),
				UnixFinish = UnixFinish(),
				NolCount = IUnit.Decimals,
				CC = 0,
				Progress = new Progress<decimal>(v =>
				{
					ProBar.Value = (double)v;
					ProcessText.Text = $"{format.DayFromUnix(PARAM.UnixStart)}: " +
									   $"загружено свечей: {PARAM.CC}" +
									   $"   ({v}%)" +
									   $"   {PARAM.Bar.TimeLeft}";
				})
			};

			DownloadElemDisable();
			await Task.Run(DownloadProcess);
			DownloadElemEnable();

			if (PARAM.Id == 0)
				return;

			new Candle();
			BYBIT.Instrument.CdiCountUpd(IUnit.Id);

			AutoProgon.Converter();
		}


		/// <summary>
		/// Процесс скачивания исторических данных в фоновом режиме
		/// </summary>
		void DownloadProcess()
		{
			var wc = new WebClient();
			DownloadCheck12(wc);

			PARAM.Table = Candle.DataTableCreate("bybit", PARAM.Symbol, PARAM.TimeFrame, PARAM.NolCount);
			PARAM.Bar = new ProBar((PARAM.UnixFinish - PARAM.UnixStart) / PARAM.TimeFrame / 60 / 1000, 1000);
			
			int barIndex = 0;
			var insert = new List<string>();
			bool isFinish = false;
			while (!isFinish)
			{
				if (!PARAM.IsProcess)
					return;

				//Формирование запроса
				string url = "https://api.bybit.com/v5/market/kline?category=spot" +
								$"&symbol={PARAM.Symbol}" +
								$"&interval={PARAM.TimeFrame}" +
								$"&start={PARAM.UnixStart}000" +
								 "&limit=1000";
				string str = wc.DownloadString(url);

				WriteLine($"{url}   {format.DTimeFromUnix(PARAM.UnixStart)}");

				PARAM.Bar.Val(barIndex++, PARAM.Progress);

				dynamic json = JsonConvert.DeserializeObject(str);
				var list = json.result.list;
				if (list.Count < 2)
					break;

				int Unix = 0;
				for (int k = list.Count - 2; k >= 0; k--)
				{
					var unit = new CandleUnit(list[k]);
					Unix = unit.Unix;
					if (Unix > PARAM.UnixFinish)
					{
						isFinish = true;
						break;
					}
					insert.Add(unit.Insert);
				}

				PARAM.CC += insert.Count;
				Candle.DataInsert(PARAM.Table, insert);
				PARAM.UnixStart = Unix;
			}

			PARAM.Id = Candle.InfoCreate(PARAM.Table);
		}

		/// <summary>
		/// Проверка на первую половину суток для минутного таймфрейма (если время загрузки начинается после 16:00)
		/// </summary>
		void DownloadCheck12(WebClient wc)
		{
			if (PARAM.TimeFrame != 1)
				return;

			string url = "https://api.bybit.com/v5/market/kline?category=spot" +
							$"&symbol={PARAM.Symbol}" +
							"&interval=1" +
							$"&start={PARAM.UnixStart}000" +
							"&limit=1000";

			string str = wc.DownloadString(url);
			dynamic json = JsonConvert.DeserializeObject(str);
			if (json.result.list.Count > 0)
				return;

			PARAM.UnixStart += 43_200; //прибавление 12 часов
		}

		/// <summary>
		/// Блокировка элементов настроек скачивания данных при начале загрузки
		/// </summary>
		void DownloadElemDisable()
		{
			ProBar.Value = 0;
			SetupPanel.IsEnabled = false;
			G.Vis(ProgressPanel);
			G.Hid(DownloadedPanel);
		}

		/// <summary>
		/// Разблокировка элементов настроек скачивания данных при начале загрузки
		/// </summary>
		void DownloadElemEnable()
		{
			SetupPanel.IsEnabled = true;
			G.Hid(ProgressPanel);
		}

		/// <summary>
		/// Отмена процесса загрузки
		/// </summary>
		void DownloadCancel(object s, RoutedEventArgs e) => PARAM.IsProcess = false;

		#endregion




		/// <summary>
		/// Список загруженных свечных данных по конкретному инструменту
		/// </summary>
		public void DownloadedListCreate()
		{
			if (IUnit == null)
				return;

			var list = Candle.ListOnIID(IUnit.Id);
			DownloadedList.ItemsSource = list;

			G.Vis(DownloadedPanel, list.Count > 0);

			if (list.Count > 0 && DownloadedList.SelectedIndex == -1)
				DownloadedList.SelectedIndex = 0;
		}

		/// <summary>
		/// Выбор из списка загруженных свечных данных
		/// </summary>
		void DowloadedListChanged(object s, SelectionChangedEventArgs e)
		{
			var box = s as ListBox;

			if (box.Items.Count > 0 && box.SelectedIndex == -1)
			{
				box.SelectedIndex = 0;
				return;
			}

			if (G.IsAutoProgon)
				return;

			EChart.CDI("History", box.SelectedItem as CDIunit);
		}

		/// <summary>
		/// Нажатие на крестик удаления загруженной истории
		/// </summary>
		void DownloadedX(object sender, MouseButtonEventArgs e) => Candle.UnitDel((sender as Label).TabIndex);
	}
}
