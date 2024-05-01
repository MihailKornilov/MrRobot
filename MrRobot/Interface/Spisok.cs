using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;

using RobotLib;

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
		/// Лимитированный список
		/// fields: поле, по которым производится поиск
		/// </summary>
		public List<SpisokUnit> ListLimit(int limit = 100,
										  string sort = "Id",
										  bool desc = false,
										  string fields = "",
										  string txt = "")
		{
			if(Count == 0)
				return UnitList;

			// Сортировка по ключу возрастанию или убыванию
			var prop = UnitList[0].GetType().GetProperty(sort);
			var sorted = desc ? 
						 UnitList.OrderByDescending(x => prop.GetValue(x)).ToList()
						 :
						 UnitList.OrderBy(x => prop.GetValue(x)).ToList();

			// Текст быстрого поиска
			var isTxt = fields.Length > 0 && txt.Length > 0;
			if(isTxt)
				prop = UnitList[0].GetType().GetProperty(fields);

			var send = new List<SpisokUnit>();
			foreach (var unit in sorted)
			{
				if (isTxt)
				{
					string value = prop.GetValue(unit).ToString().ToLower();
					if (!value.Contains(txt))
						continue;
				}

				send.Add(unit);

				if (--limit == 0)
					return send;
			}
			return send;
		}





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

		public SpisokUnit(dynamic res) =>			// Конструктор с запросом из БД
			Id = res.GetInt32("id");

		public bool IsNull =>						// Пустая запись
			Id == 0;
		public string Num { get; set; }             // Порядковый номер для вывода в списке
		public int Id { get; set; }                 // ID единицы списка


		// ---=== Общие переменные ===---
		public int Int01 { get; set; }				// Значение INT32 1
		public decimal Dec01 { get; set; }           // Значение DECIMAL 1
		public double Dbl01 { get; set; }           // Значение DOUBLE 1
		public double Dbl02 { get; set; }           // Значение DOUBLE 2
		public double Dbl03 { get; set; }           // Значение DOUBLE 3
		public double Dbl04 { get; set; }           // Значение DOUBLE 4
		public string Dbl01str => format.E(Dbl01);  // Значение DOUBLE в формате STRING с избавлением от E
		public string Dbl02str => format.E(MinOrderQty);
		public string Dbl03str => format.E(BasePrecision);
		public string Dbl04str => format.E(TickSize);

		public double Dbl05 { get; set; }           // Значение DOUBLE 5
		public string Dbl05str { get; set; }
		public SolidColorBrush Dbl05clr { get; set; }

		public double Dbl06 { get; set; }           // Значение DOUBLE 6
		public string Dbl06str { get; set; }

		public double Dbl07 { get; set; }           // Значение DOUBLE 7

		public string Str01 { get; set; }
		public string Str02 { get; set; }

		public long   Lng01 { get; set; }           // Значение LONG 1
		public string Lng01str { get; set; }

		public DateTime DTime01 { get; set; }




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

		public decimal BasePrecision { get; set; }	// Точность базовой монеты
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
		public decimal BaseBalance { get; set; }		// Баланс базовой монеты
		public decimal QuoteBalance { get; set; }	// Баланс котировочной монеты
		//public double BaseCommiss { get; set; }		// Сумма комиссий исполненных ордеров базовой монеты
		//public double QuoteCommiss { get; set; }	// Сумма комиссий исполненных ордеров базовой котировочной монеты

		public int CdiId { get; set; }				// ID свечных данных
		CDIunit CDI => Candle.Unit(CdiId);			// Свечные данные
		public string Table => CDI.Table;			// Имя таблицы со свечами
		public int RowsCount => CDI.RowsCount;		// Количество свечей в графике (в таблице)
		public int TimeFrame => CDI.TimeFrame;		// Таймферйм
		public string TF => CDI.TF;					// Таймфрейм 10m
	}
}
