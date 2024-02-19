using System;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using System.Windows.Media;

namespace MrRobot.Interface
{
    public class Spisok
    {
		public delegate void DLGT();
		public DLGT Updated { get; set; }

		/// <summary>
		/// Список 
		/// </summary>
		List<SpisokUnit> UnitList { get; set; }

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
		public virtual SpisokUnit UnitInitSecond(SpisokUnit unit, dynamic res) => unit;

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
				unit = UnitInitSecond(unit, res);
				UnitList.Add(unit);
				ID_UNIT.Add(unit.Id, unit);
			});

			Updated?.Invoke();
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
	}

	public class SpisokUnit
    {
		public SpisokUnit(dynamic res) =>
			Id = res.GetInt32("id");

		public string Num { get; set; }             // Порядковый номер для вывода в списке
		public int Id { get; set; }                 // ID единицы списка
		public string HistoryBegin { get; set; }    // Дата начала истории свечных данных
		public bool IsTrading { get; set; }         // Инструмент торгуется или нет
		public int CdiCount { get; set; }           // Количество скачанных свечных данных
		public SolidColorBrush CdiCountColor =>		// Скрытие количества свечных данных в выводе, если 0
			format.RGB(CdiCount > 0 ? "#777777" : "#FFFFFF");



		public string BaseCoin { get; set; }        // Название базовой монеты
		public string QuoteCoin { get; set; }       // Название котировочной монеты
		public string Symbol { get; set; }          // Название инструмента в виде "BTCUSDT"
		public string SymbolName =>					// Название инструмента в виде "BTC/USDT"
			$"{BaseCoin}/{QuoteCoin}"; 

		public double BasePrecision { get; set; }   // Точность базовой монеты
		public double MinOrderQty { get; set; }     // Минимальная сумма ордера
		public double TickSize { get; set; }        // Шаг цены
		public int Decimals =>						// Количество нулей после запятой
			format.Decimals(TickSize);


		// ---=== Exchange ===---
		public string Name { get; set; }            // Имя единицы списка
		public string Prefix { get; set; }			// Префикс для таблиц в базе
		public string Url { get; set; }				// Адрес сайта биржи
	}
}
