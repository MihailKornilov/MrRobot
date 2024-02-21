using System;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Interface
{
	public class Spisok
	{
		public delegate void DLGT();
		public DLGT Updated { get; set; }

		/// <summary>
		/// Список 
		/// </summary>
		protected List<SpisokUnit> UnitList { get; set; }

		/// <summary>
		/// Количество доступных единиц списка
		/// </summary>
		public int Count => UnitList.Count;

		/// <summary>
		/// Весь список
		/// </summary>
		public List<SpisokUnit> ListAll => UnitList;

		/// <summary>
		/// Ассоциативный массив списка по ID
		/// </summary>
		Dictionary<int, SpisokUnit> ID_UNIT { get; set; }

		/// <summary>
		/// Основной запрос для получения списка (обязательно для переопределения)
		/// </summary>
		public virtual string SQL { get; }

		/// <summary>
		/// Виртуальный метод для инициализации единицы списка (переопределять не обязательно)
		/// </summary>
		public virtual SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res) => unit;

		public Spisok()
		{
			// !!! Переделать на выплывающее сообщение в приложении
			if(SQL == null)
				throw new Exception("Свойство SQL должно быть обязательно переопределено.");

			UnitList = new List<SpisokUnit>();
			ID_UNIT = new Dictionary<int, SpisokUnit>();

			mysql.Delegat(SQL, res =>
			{
				var unit = new SpisokUnit(res);
				unit = UnitFieldsFill(unit, res);
				UnitList.Add(unit);
				ID_UNIT.Add(unit.Id, unit);
			});
		}

		/// <summary>
		/// Данные единицы списка по ID
		/// </summary>
		public SpisokUnit Unit(int id)
		{
			if(ID_UNIT.ContainsKey(id))
				return ID_UNIT[id];
			
			// !!! Сделать возврат пустой единицы списка
			return null;
		}

		/// <summary>
		/// Данные единицы списка по Свойству
		/// </summary>
		public SpisokUnit UnitOnField(string field, string val)
		{
			if (Count == 0)
				return null;

			var info = UnitList[0].GetType().GetProperty(field);
			foreach (var unit in UnitList)
			{
				string value = info.GetValue(unit).ToString().Trim();
				if (value.Length == 0)
					continue;
				if (value == val)
					return unit;
			}

			return null;
		}
		public int IdOnField(string field, int val)
		{
			if (Count == 0)
				return 0;

			var info = UnitList[0].GetType().GetProperty(field);
			foreach (var unit in UnitList)
			{
				int value = Convert.ToInt32(info.GetValue(unit));
				if (value == val)
					return unit.Id;
			}

			return 0;
		}

		/// <summary>
		/// Ассоциативный массив данных списка по Свойству
		/// </summary>
		public Dictionary<string, SpisokUnit> FieldASS(string field)
		{
			var send = new Dictionary<string, SpisokUnit>();

			if (Count == 0)
				return send;

			var info = UnitList[0].GetType().GetProperty(field);
			foreach (var unit in UnitList)
			{
				string value = info.GetValue(unit).ToString().Trim();
				if(value.Length > 0)
					send.Add(value, unit);
			}
			return send;
		}
	}

	public class SpisokUnit
	{
		public SpisokUnit(int id) => Id = id;
		// Конструктор с запросом из БД
		public SpisokUnit(dynamic res) => Id = res.GetInt32("id");

		public string Num { get; set; }             // Порядковый номер для вывода в списке
		public int Id { get; set; }                 // ID единицы списка
		public string Name { get; set; }            // Имя единицы списка
		public string Symbol { get; set; }          // Идентификатор инструмента в виде "BTCUSDT"
		public string HistoryBegin { get; set; }    // Дата начала истории свечных данных
		public bool IsTrading { get; set; }         // Инструмент торгуется или нет
		public int CdiCount { get; set; }           // Количество скачанных свечных данных
		public SolidColorBrush CdiCountColor =>		// Скрытие количества свечных данных в выводе, если 0
			format.RGB(CdiCount > 0 ? "#777777" : "#FFFFFF");




		// ---=== BYBIT ===---
		public string BaseCoin { get; set; }        // Название базовой монеты
		public string QuoteCoin { get; set; }       // Название котировочной монеты
		public string SymbolName =>					// Название инструмента в виде "BTC/USDT"
			$"{BaseCoin}/{QuoteCoin}"; 

		public double BasePrecision { get; set; }   // Точность базовой монеты
		public double MinOrderQty { get; set; }     // Минимальная сумма ордера
		public double TickSize { get; set; }        // Шаг цены
		public int Decimals =>						// Количество нулей после запятой
			format.Decimals(TickSize);



		// ---=== Exchange ===---
		public string Prefix { get; set; }			// Префикс для таблиц в базе
		public string Url { get; set; }             // Адрес сайта биржи



		// ---=== MOEX ===---
		public int MoexId { get; set; }				// ID инструмениа
		public int GroupId { get; set; }            // ID группы
		public int TypeId { get; set; }             // ID вида инструмента
		public string ShortName { get; set; }       // Краткое наименование



		// ---=== ДЛЯ РОБОТА ===---
		public double BaseBalance { get; set; }		// Баланс базовой монеты
		public double QuoteBalance { get; set; }	// Баланс котировочной монеты
		public double BaseCommiss { get; set; }		// Сумма комиссий исполненных ордеров базовой монеты
		public double QuoteCommiss { get; set; }	// Сумма комиссий исполненных ордеров базовой котировочной монеты

		public int CdiId { get; set; }				// ID свечных данных
		CDIunit CDI => Candle.Unit(CdiId);			// Свечные данные
		public string Table => CDI.Table;			// Имя таблицы со свечами
		public int RowsCount => CDI.RowsCount;		// Количество свечей в графике (в таблице)
		public int TimeFrame => CDI.TimeFrame;		// Таймферйм
		public string TF => CDI.TF;					// Таймфрейм 10m
	}
}
