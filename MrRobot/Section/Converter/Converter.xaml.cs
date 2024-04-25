using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;

using RobotLib;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;

namespace MrRobot.Section
{
	public partial class Converter : UserControl
	{
		public Converter() => G.Converter = this;

		public void Init()
		{
			InitializeComponent();
			CDIpanel.Page(2).TBLink = SelectLink.TBLink;
			CDIpanel.Page(2).OutMethod += SourceChanged;
			SourceChanged();
			Candle.Updated += ResultListCreate;
		}


		bool IsSourceChosen => SourceId > 0;        // Свечные данные выбраны
		int SourceId => CDIpanel.Page(2).CdiId;     // ID свечных данных
		CDIunit SourceUnit => Candle.Unit(SourceId);// Единица свечных данных


		public void SourceChanged()
		{
			if (!IsSourceChosen)
				return;
			if (G.IsAutoProgon)
				return;

			ResultListCreate();
			new AdvChart(ChartPanel, SourceUnit);
		}



		#region Процесс конвертации

		CDIparam ConvertParam;
		/// <summary>
		/// Составление массива с выбранными таймфреймами для конвертации
		/// </summary>
		int[] ConvertCheckedTF()
		{
			Dictionary<int, string> CheckTF = format.TFass();
			int count = 0;
			int[] CheckedTF = new int[100];
			foreach (int tf in CheckTF.Keys)
			{
				var check = FindName("CheckTF" + tf) as CheckBox;
				if (check == null)
					continue;
				if (!(bool)check.IsChecked)
					continue;

				CheckedTF[count++] = tf;
			}
			Array.Resize(ref CheckedTF, count);

			return CheckedTF;
		}
		/// <summary>
		/// Запуск конвертации
		/// </summary>
		async void ConvertGo(object sender, RoutedEventArgs e)
		{
			if (!IsSourceChosen)
				return;

			int[] CheckedTF = ConvertCheckedTF();
			if (CheckedTF.Length == 0)
				return;

			G.Hid(ConvertGoButton);
			G.Vis(ProcessPanel);
			TFpanel.IsEnabled = false;

			ProgressMain.Value = 0;
			ProgressSub.Value = 0;

			ConvertParam = new CDIparam()
			{
				ExchangeId = SourceUnit.ExchangeId,
				InstrumentId = SourceUnit.InstrumentId,
				Symbol = SourceUnit.Symbol,
				Decimals = SourceUnit.Decimals,
				Progress = new Progress<decimal>(v =>
				{
					ProgressMain.Value = ConvertParam.ProgressMainValue;
					ProgressSub.Value = (double)v;

					// Снятие галочки с очередного сконвертированного таймфрейма
					if (v == 100)
						(FindName("CheckTF" + ConvertParam.TimeFrame) as CheckBox).IsChecked = false;
				})
			};

			await Task.Run(() => ConvertProcess(ConvertParam, CheckedTF));

			new Candle();
			BYBIT.Instrument.CdiCountUpd(SourceUnit.InstrumentId);


			G.Vis(ConvertGoButton);
			G.Hid(ProcessPanel);
			TFpanel.IsEnabled = true;
			ResultListCreate();

			AutoProgon.PatternSearchSetup();
		}
		/// <summary>
		/// Процесс концертации в фоновом режиме
		/// </summary>
		void ConvertProcess(CDIparam PARAM, int[] CheckedTF)
		{
			var CDI = SourceUnit;
			PARAM.Bar = new ProBar((CheckedTF.Length + 1) * CDI.RowsCount);

			// Загрузка из базы исходного минутного таймфрейма
			int sbi = 0;
			var SubBar = new ProBar(CDI.RowsCount);
			var TF1 = new List<CandleUnit>();
			string sql = $"SELECT*FROM`{CDI.Table}`";
			my.Data.Delegat(sql, row =>
			{
				if (!PARAM.IsProcess)
					return false;

				TF1.Add(new CandleUnit(row));

				if (SubBar.Val(sbi++, PARAM.Progress))
				{
					PARAM.Bar.isUpd(sbi);
					PARAM.ProgressMainValue = (double)PARAM.Bar.Value;
				}
				return true;
			});

			if (!PARAM.IsProcess)
				return;

			for (int i = 0; i < CheckedTF.Length; i++)
			{
				PARAM.TimeFrame = CheckedTF[i];
				PARAM.TfNum = i;    // Счётчик для Main-прогресс-бара
				ConvertProcessTF(PARAM, TF1);

				if (!PARAM.IsProcess)
					return;
			}

			PARAM.ProgressMainValue = 100;
			PARAM.Progress.Report(100);
		}
		/// <summary>
		/// Процесс конвертации в выбранные таймфреймы
		/// </summary>
		void ConvertProcessTF(CDIparam PARAM, List<CandleUnit> TF1)
		{
			var SubBar = new ProBar(TF1.Count);
			PARAM.Progress.Report(0);

			Candle.CDIcreate(PARAM);

			// Определение начала первой свечи согласно таймфрейму
			int iBegin;
			for (iBegin = 0; iBegin < TF1.Count; iBegin++)
			{
				var src = TF1[iBegin];
				if (src.Unix == Candle.UnixTF(src.Unix, PARAM.TimeFrame))
					break;
			}
			var dst = new CandleUnit(TF1[iBegin++], PARAM.TimeFrame);

			var insert = new List<string>();
			int MainCount = (PARAM.TfNum + 1) * TF1.Count;
			for (int i = iBegin; i < TF1.Count; i++)
			{
				if (!PARAM.IsProcess)
					return;

				var src = TF1[i];

				if (!dst.Upd(src))
				{
					insert.Add(dst.Insert);
					Candle.DataInsert(PARAM.Table, insert, 500);
					dst = new CandleUnit(src, PARAM.TimeFrame);
				}

				if (!SubBar.Val(i, PARAM.Progress))
					continue;

				PARAM.Bar.isUpd(MainCount + i);
				PARAM.ProgressMainValue = (double)PARAM.Bar.Value;
			}

			PARAM.Progress.Report(100);
			Candle.DataInsert(PARAM.Table, insert);
			Candle.CDIupdate(PARAM, SourceId);
		}
		/// <summary>
		/// Отмена процесса конвертации
		/// </summary>
		void ConvertCancel(object s, RoutedEventArgs e) =>
			ConvertParam.IsProcess = false;

		#endregion



		/// <summary>
		/// Список таймфреймов с результатами конвертации
		/// </summary>
		public void ResultListCreate() =>
			ResultListBox.ItemsSource = IsSourceChosen ? Candle.ListOnIID(SourceUnit.InstrumentId, false) : null;

		/// <summary>
		/// Показ графика результата конвертации
		/// </summary>
		void ConverterResultChanged(object sender, SelectionChangedEventArgs e)
		{
			if (G.IsAutoProgon)
				return;

			var item = (sender as ListBox).SelectedItem as CDIunit;
			new AdvChart(ChartPanel, item);
		}

		/// <summary>
		/// Удаление результата конвертации
		/// </summary>
		void ConvertedX(object sender, MouseButtonEventArgs e) =>
			Candle.UnitDel((sender as Label).TabIndex);
	}
}
