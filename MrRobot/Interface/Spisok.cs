using System;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using System.Windows;

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
		/// Ассоциативный массив списка по ID
		/// </summary>
		protected Dictionary<int, SpisokUnit> ID_UNIT { get; set; }

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

			my.Main.Delegat(SQL, res =>
			{
				var unit = new SpisokUnit(res);
				unit = UnitFieldsFill(unit, res);
				UnitList.Add(unit);
				ID_UNIT.Add(unit.Id, unit);
			});
		}




		/// <summary>
		/// Количество доступных единиц списка
		/// </summary>
		public int Count => UnitList.Count;

		/// <summary>
		/// Весь список
		/// </summary>
		public List<SpisokUnit> ListAll => UnitList;

		/// <summary>
		/// Весь список с нулевой записью
		/// </summary>
		public List<SpisokUnit> AllWithNull(string title = "не выбрано")
		{
			var list = new List<SpisokUnit> { UnitNull(title) };
			foreach (var unit in UnitList)
				list.Add(unit);
			return list;
		}


		/// <summary>
		/// Данные записи по ID
		/// </summary>
		public SpisokUnit Unit(int id) =>
			ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : UnitNull();

		/// <summary>
		/// Пустая запись
		/// </summary>
		public SpisokUnit UnitNull(string title = "Пустая запись")
		{
			var unit = new SpisokUnit(0);
			unit.Name  = title;
			unit.Title = title;
			return unit;
		}

		/// <summary>
		/// Данные единицы списка по Свойству
		/// </summary>
		public SpisokUnit UnitOnField(string field, string val)
		{
			if (Count == 0)
				return UnitNull();

			var info = UnitList[0].GetType().GetProperty(field);
			foreach (var unit in UnitList)
			{
				string value = info.GetValue(unit).ToString().Trim();
				if (value.Length == 0)
					continue;
				if (value == val)
					return unit;
			}

			return UnitNull();
		}
		public int FieldToId(string field, int val)
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
		public int FieldToId(string field, string val)
		{
			if (Count == 0)
				return 0;

			var info = UnitList[0].GetType().GetProperty(field);
			foreach (var unit in UnitList)
			{
				string value = info.GetValue(unit).ToString();
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
		public SolidColorBrush NullColor =>			// Цвет для нулевой записи
			format.RGB(Id > 0 ? "#000000" : "#999999");
		public int Count1 { get; set; }				// Количество записей в определённой группе
		public virtual Visibility Vis1 =>			// Видимость количества по условию [1]
			G.Vis(Count1 > 0);




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
		public int EngineId { get; set; }			// ID торговой системы
		public int MarketId { get; set; }			// ID рынка
		public int GroupId { get; set; }            // ID группы
		public int TypeId { get; set; }             // ID вида инструмента
		public string ShortName { get; set; }       // Краткое наименование
		public string Title { get; set; }           // Описание



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
