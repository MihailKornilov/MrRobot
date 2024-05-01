using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using static System.Console;

using Newtonsoft.Json;
using RobotLib;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;
using MrRobot.Interface;

namespace MrRobot.Section
{
	public partial class History : UserControl
	{
		/// <summary>
		/// Обновление валютных пар для ByBit
		/// </summary>
		async void InstrumentUpdateGo(object sender, RoutedEventArgs e)
		{
			InstrumentUpdateButton.Visibility = Visibility.Collapsed;
			InstrumentUpdateBar.Value = 0;
			InstrumentUpdateBarText.Content = "";
			InstrumentUpdateBarPanel.Visibility = Visibility.Visible;

			var progress = new Progress<decimal>(v => {
				InstrumentUpdateBar.Value = (double)v;
				InstrumentUpdateBarText.Content = v + "%";
			});
			await Task.Run(() => InstrumentUpdateProcess(progress));
			new BYBIT();
			HeadCountWrite();

			await Task.Run(() => Candle.DataControl(progress));
			new Candle();
			InstrumentUpdateButton.Visibility = Visibility.Visible;
			InstrumentUpdateBarPanel.Visibility = Visibility.Collapsed;
		}
		void InstrumentUpdateProcess(IProgress<decimal> Progress)
		{
			// Ассоциативный массив инструментов по Symbol
			var mass = BYBIT.Instrument.FieldASS("Symbol");
			string url = "https://api.bybit.com/v5/market/instruments-info?category=spot";
			string str = new WebClient().DownloadString(url);
			if (!str.Contains("spot"))
				return;

			dynamic json = JsonConvert.DeserializeObject(str);
			dynamic list = json.result.list;
			if (list.Count == 0)
				return;

			var bar = new ProBar(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if(bar.isUpd(i))
					Progress.Report(bar.Value);

				var v = list[i];
				var lsf = v.lotSizeFilter;
				string symbol = v.symbol;

				//Инструмент присутствует в списке
				if (mass.ContainsKey(symbol))
				{
					var unit = mass[symbol];
					InstrumentValueCheck(unit, "basePrecision", unit.BasePrecision, lsf.basePrecision);
					InstrumentValueCheck(unit, "qtyMin",		unit.MinOrderQty,	lsf.minOrderQty);
					InstrumentValueCheck(unit, "tickSize",		unit.TickSize,		v.priceFilter.tickSize);
					InstrumentValueCheck(unit, "isTrading",		unit.IsTrading,		v.status == "Trading" ? "1" : "0");
					BeginUpdate(unit);
					continue;
				}

				//Если отсутствует, внесение нового инструмента в базу
				string sql = "INSERT INTO`_instrument`(" +
								"`exchangeId`," +
								"`symbol`," +
								"`baseCoin`," +
								"`quoteCoin`," +
								"`basePrecision`," +
								"`qtyMin`," +
								"`tickSize`" +
							 ")VALUES(" +
							   $"{BYBIT.ExchangeId}," +
							   $"'{symbol}'," +
							   $"'{v.baseCoin}'," +
							   $"'{v.quoteCoin}'," +
							   $"{lsf.basePrecision}," +
							   $"{lsf.minOrderQty}," +
							   $"{v.priceFilter.tickSize}" +
							  ")";
				var instr = new SpisokUnit(my.Main.Query(sql));
				instr.Symbol = symbol;
				InstrumentLogInsert(instr, "Новый инструмент", "", (v.baseCoin + "/" + v.quoteCoin).ToString());
				BeginUpdate(instr);
			}
		}

		/// <summary>
		/// Проверка и обновление изменённых параметров в инструменте
		/// </summary>
		void InstrumentValueCheck(SpisokUnit unit, string param, dynamic oldV, dynamic newV)
		{
			string oldS = format.E(oldV);
			string newS = format.E(newV);

			if (oldS == newS)
				return;

			string sql = "UPDATE`_instrument`" +
						$"SET`{param}`='{newS}'" +
						$"WHERE`id`={unit.Id}";
			my.Main.Query(sql);

			InstrumentLogInsert(unit, $"Изменился параметр \"{param}\"", oldS, newS);
		}

		/// <summary>
		/// Внесение лога изменения в инструменте
		/// </summary>
		void InstrumentLogInsert(SpisokUnit unit, string about, string oldV, string newV)
		{
			string sql = "INSERT INTO `_instrument_log`(" +
							"`exchangeId`," +
							"`instrumentId`," +
							"`about`," +
							"`old`," +
							"`new`" +
						 ") VALUES (" +
							$"{BYBIT.ExchangeId}," +
							$"{unit.Id}," +
							$"'{about}'," +
							$"'{oldV}'," +
							$"'{newV}'" +
						 ")";
			my.Main.Query(sql);
		}

		/// <summary>
		/// Обновление начала истории по каждому инструменту ByBit
		/// </summary>
		void BeginUpdate(SpisokUnit unit)
		{
			if (unit.HistoryBegin != null && !unit.HistoryBegin.Contains("0001"))
				return;

			string start = "1577826000";    //2020-01-01 - начало истории для всех инструментов

			// Сначала получение списка по неделям
			if (!NewStart(unit.Symbol, "W", ref start))
				return;

			// Затем список по 15 мин., начиная с первого дня недели
			if (!NewStart(unit.Symbol, "15", ref start))
				return;

			// Затем список по 1 мин. для максимально точного получения начала истории
			if (!NewStart(unit.Symbol, "1", ref start))
				return;

			string sql = "UPDATE`_instrument`" +
						$"SET`historyBegin`=FROM_UNIXTIME({start})" +
						$"WHERE `id`={unit.Id}";
			my.Main.Query(sql);
		}

		bool NewStart(string symbol, string interval, ref string start)
		{
			var list = BYBIT.Kline(symbol, interval, start);
			if(list == null)
				return false;

			int count = list.Count;
			if (count == 0)
				return false;

			start = list[count - 1][0];
			start = start.Substring(0, 10);
			return true;
		}
	}
}
