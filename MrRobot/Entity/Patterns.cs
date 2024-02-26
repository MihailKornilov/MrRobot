using System;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Entity
{
	public class Patterns
	{
		public Patterns()
		{
			SearchListCreate();
			PatternListCreate();
		}



		static List<SearchUnit> SearchAll { get; set; }       // Список всех поисков
		static List<SearchUnit> SearchResult { get; set; }    // Список поисков с результатами
		static Dictionary<int, SearchUnit> PSL { get; set; }  // Ассоциативный массив поисков
		static void SearchListCreate()
		{
			SearchAll = new List<SearchUnit>();
			SearchResult = new List<SearchUnit>();
			PSL = new Dictionary<int, SearchUnit>();

			string sql = "SELECT*" +
						 "FROM`_pattern_search`" +
						 "ORDER BY`id`DESC";
			my.Main.Delegat(sql, row =>
			{
				var unit = new SearchUnit(row);

				SearchAll.Add(unit);
				PSL.Add(unit.Id, unit);

				if (unit.FoundCount > 0)
					SearchResult.Add(unit);
			});
		}
		public static List<SearchUnit> SearchListAll() =>
			SearchResult;
		/// <summary>
		/// Данные поиска по ID
		/// </summary>
		public static SearchUnit SUnit(int id) =>
			PSL.ContainsKey(id) ? PSL[id] :  null;
		/// <summary>
		/// Получение ID поиска на основании параметров
		/// </summary>
		public static int SUnitIdOnParam(int CdiId, int PatternLength, int PrecisionPercent)
		{
			foreach (var S in SearchAll)
			{
				if (S.CdiId != CdiId)
					continue;
				if (S.PatternLength != PatternLength)
					continue;
				if (S.PrecisionPercent != PrecisionPercent)
					continue;

				return S.Id;
			}

			return 0;
		}
		/// <summary>
		/// Получение данных поиска на основании параметров
		/// </summary>
		public static SearchUnit SUnitOnParam(int CdiId, int PatternLength, int PrecisionPercent)
		{
			int id = SUnitIdOnParam(CdiId, PatternLength, PrecisionPercent);
			return SUnit(id);
		}
		/// <summary>
		/// Удаление поиска
		/// </summary>
		public static void SUnitDel(int id)
		{
			if (!PSL.ContainsKey(id))
				return;

			string sql = $"DELETE FROM`_pattern_found`WHERE`searchId`={id}";
			my.Main.Query(sql);

			sql = $"DELETE FROM`_pattern_search`WHERE`id`={id}";
			my.Main.Query(sql);

			new Patterns();
		}







		static List<object> PatternList { get; set; }           // Список паттернов
		/// <summary>
		/// Загрузка из базы списка свечных данных
		/// </summary>
		static void PatternListCreate()
		{
			PatternList = new List<object>();

			string sql = "SELECT*" +
						 "FROM`_pattern_found`" +
						 "ORDER BY`searchId`,`repeatCount` DESC";
			my.Main.Delegat(sql, row => PatternList.Add(new PatternUnit(row)));
		}

		/// <summary>
		/// Весь список паттернов
		/// </summary>
		public static List<object> ListAll() =>
			PatternList;
		/// <summary>
		/// Список паттернов определённого поиска
		/// </summary>
		public static List<PatternUnit> List(int searchId)
		{
			var list = new List<PatternUnit>();
			int num = 1;
			foreach(PatternUnit unit in PatternList)
				if(unit.SearchId == searchId)
				{
					unit.Num = num++;
					list.Add(unit);
				}

			return list;
		}
		public static List<PatternUnit> ProfitList(int prc = 0, string order = "id")
		{
			var list = new List<PatternUnit>();
			foreach (PatternUnit unit in PatternList)
			{
				if (!unit.IsTested)
					continue;
				if (unit.ProfitPercent == 0)
					continue;
				if (unit.ProfitPercent < prc)
					continue;

				list.Add(unit);
			}

			if (order == "id")
				return list;

			return list.OrderByDescending(x => x.ProfitPercent).ToList();
		}
		/// <summary>
		/// Индекс конкретного паттерна для указания в списке
		/// </summary>
		public static int Index(int searchId, int id)
		{
			if (id == 0)
				return 0;

			int index = 0;
			foreach(var unit in List(searchId))
			{
				if (unit.Id == id)
					return index;
				index++;
			}

			return index;
		}
		/// <summary>
		/// Количество паттернов, которые не прошли тест в указанных свечных данных
		/// </summary>
		public static int NoTestedCount(int cdiId)
		{
			int count = 0;

			foreach(PatternUnit unit in PatternList)
				if (unit.CdiId == cdiId)
					if (!unit.IsTested)
						count++;

			return count;
		}
	}







	/// <summary>
	/// Единица поиска паттернов
	/// </summary>
	public class SearchUnit
	{
		public SearchUnit(dynamic row)
		{
			Id				 = row.GetInt32("id");
			CdiId			 = row.GetInt32("cdiId");
			PatternLength	 = row.GetInt32("patternLength");
			PrecisionPercent = row.GetInt32("scatterPercent");
			FoundRepeatMin	 = row.GetInt32("foundRepeatMin");
			FoundCount		 = row.GetInt32("foundCount");
			Duration		 = row.GetString("duration");
			Dtime			 = format.DateOne(row.GetMySqlDateTime("added").ToString());
			TestedCount		 = row.GetInt32("testedCount");
		}
		public int Id { get; set; }             // ID поиска
		public int CdiId { get; set; }          // ID свечных данных

		public int PatternLength { get; set; }  // Длина паттерна
		public int PrecisionPercent { get; set; }// Точность в процентах
		public int FoundRepeatMin { get; set; } // Исключать менее N нахождений

		public int FoundCount { get; set; }     // Количество найденных паттернов
		public string Duration { get; set; }    // Время выполнения
		public string Dtime { get; set; }       // Дата и время поиска

		public int TestedCount { get; set; }    // Количество паттернов, которые прошли тест



		// ---=== ДЛЯ ВЫВОДА В СПИСОК ПОИСКОВ ===---
		public string IdStr { get { return "#" + Id; } }
		// Название инструмента
		public string Symbol { get { return Candle.Unit(CdiId).Name; } }
		// Таймфрейм в виде 10m
		public string TF { get { return Candle.Unit(CdiId).TF; } }
		// Количество свечей в графике
		public string CandlesCountStr
		{
			get
			{
				int count = Candle.Unit(CdiId).RowsCount;
				return Candle.CountTxt(count);
			}
		}
	}
	/// <summary>
	/// Единица паттерна
	/// </summary>
	public class PatternUnit
	{
		// ---=== ФОРМИРОВАНИЕ ПАТТЕРНА ===---
		public PatternUnit(dynamic row)
		{
			Id = row.GetInt32("id");
			SearchId = row.GetInt32("searchId");

			Size = row.GetInt32("size");
			StructDB = row.GetString("structure");
			string uxList = row.GetString("unixList");
			UnixList = Array.ConvertAll(uxList.Split(','), s => int.Parse(s)).ToList();

			ProfitCount = row.GetInt32("profitCount");
			LossCount = row.GetInt32("lossCount");
		}
		public PatternUnit(List<CandleUnit> list, int cdiId, int PrecisionPercent)
		{
			double PriceMax = 0;
			double PriceMin = list[0].Low;

			foreach (var cndl in list)
			{
				if (cndl.High == cndl.Low)
					return;

				if(PriceMax < cndl.High)
					PriceMax = cndl.High;
				if(PriceMin > cndl.Low)
					PriceMin = cndl.Low;
			}

			var CDI = Candle.Unit(cdiId);
			Size = (int)Math.Round((PriceMax - PriceMin) * CDI.Exp);

			CandleList = new List<CandleUnit>();
			foreach (var cndl in list)
				CandleList.Add(new CandleUnit(cndl, PriceMax, PriceMin, PrecisionPercent));

			StructFromCandle();
		}

		// Создание нового паттерна, используя существующий. Применяется в роботах.
		public PatternUnit Create(List<dynamic> list, int cdiId, int PrecisionPercent)
		{
			var newList = new List<CandleUnit>();
			foreach (var cndl in list)
				newList.Add(cndl);

			return new PatternUnit(newList, cdiId, PrecisionPercent);
		}
		public int Size { get; set; }   // Размер паттерна в пунктах
		public List<CandleUnit> CandleList { get; set; } // Состав паттерна из свечей
		// Сравнение паттернов
		public bool Compare(PatternUnit PU) => Compare(PU.CandleList);
		public bool Compare(List<CandleUnit> CL)
		{
			for (int k = 0; k < Length; k++)
			{
				var src = StructArr[k];
				var dst = CL[k];

				if (src[2] < dst.BodyMin)
					return false;
				if (src[2] > dst.BodyMax)
					return false;

				if (src[1] < dst.WickTopMin)
					return false;
				if (src[1] > dst.WickTopMax)
					return false;

				if (src[3] < dst.WickBtmMin)
					return false;
				if (src[3] > dst.WickBtmMax)
					return false;

				if (src[0] < dst.SpaceTopMin)
					return false;
				if (src[0] > dst.SpaceTopMax)
					return false;

				if (src[4] < dst.SpaceBtmMin)
					return false;
				if (src[4] > dst.SpaceBtmMax)
					return false;
			}
			return true;
		}
		// Содержание паттерна: свечи с учётом пустот сверху и снизу
		void StructFromCandle()
		{
			string[] arr = new string[CandleList.Count];

			for (int k = 0; k < CandleList.Count; k++)
				arr[k] = CandleList[k].Struct();

			StructDB = string.Join(";", arr);
		}


		// Длина паттерна
		int _Length;
		public int Length
		{
			get
			{
				if (_Length == 0)
					_Length = StructDB.Split(';').Length;
				return _Length;
			}
			set { _Length = value; }
		}



		// ---=== НАЙДЕННЫЕ ПАТТЕРНЫ ===---
		public int Id { get; set; }
		public string IdStr => $"#{Id}";
		public int Num { get; set; }            // Порядковый номер
		// Количество повторений паттерна на графике
		public int Repeat => UnixList.Count;
		// Времена найденных паттернов в формате UNIX
		public List<int> UnixList { get; set; } = new List<int>();

		// Структура паттерна: свечи в процентах
		public string Struct {
			get
			{
				string[] ST = new string[Length*5];
				int[] W = new int[Length]; // Ширина столбца для наглядного отображения паттерна
				var cndl = StructDB.Split(';');
				for (int i = 0; i < Length; i++)
				{
					var spl = cndl[i].Split(' ');
					for (int k = 0; k < 5; k++)
					{
						ST[i+Length*k] = spl[k];
						if (W[i] < spl[k].Length)
							W[i] = spl[k].Length;
					}
				}

				string[] send = new string[5];
				for (int i = 0; i < 5; i++)
				{
					string[] row = new string[Length];
					for (int k = 0; k < Length; k++)
					{
						row[k] = ST[i*Length+k];
						while(row[k].Length < 6)
							row[k] = " " + row[k];
					}

					send[i] = string.Join("  ", row);
				}

				return string.Join("\n", send);
			}
		}
		public string StructDB { get; set; }

		// Структура паттерна в виде массива процентов
		List<int[]> _StructArr;
		List<int[]> StructArr
		{
			get
			{
				if(_StructArr != null)
					return _StructArr;

				_StructArr = new List<int[]>();

				foreach(var cndl in StructDB.Split(';'))
				{
					int[] prc = Array.ConvertAll(cndl.Split(' '), s => (int)(double.Parse(s)*100));
					_StructArr.Add(prc);
				}

				return _StructArr;
			}
		}

		// Строка внесения паттерна в базу
		public string Insert(int SearchId)
		{
			return "(" +
				$"{SearchId}," +
				$"{Size}," +
				$"'{StructDB}'," +
				$"{Repeat}," +
				$"'{string.Join(",", UnixList.ToArray())}'" +
			")";
		}

		// Запись в файл о нейденном паттерне
		public void FoundSave()
		{
			string txt = $"Найдено совпадение: {Name} {TF}   id.{Id}   {StructDB}";
			G.LogWrite(txt, "found.txt");
		}




		// ---=== ИНФОРМАЦИЯ О ПОИСКЕ ===---
		// ID поиска из `_pattern_search`
		public int SearchId { get; set; }
		// Точность в процентах
		public int PrecisionPercent => Patterns.SUnit(SearchId).PrecisionPercent;
		// Исключать менее N нахождений
		public int FoundRepeatMin => Patterns.SUnit(SearchId).FoundRepeatMin;
		// Дата и время поиска
		public string Dtime => Patterns.SUnit(SearchId).Dtime;




		// ---=== ИНФОРМАЦИЯ О СВЕЧНЫХ ДАННЫХ ===---
		// ID свечных данных
		public int CdiId => Patterns.SUnit(SearchId).CdiId;
		// Название инструмента
		public string Symbol => Candle.Unit(CdiId).Symbol;
		public string Name => Candle.Unit(CdiId).Name;
		// Таймфрейм
		public int TimeFrame => Candle.Unit(CdiId).TimeFrame;
		// Таймфрейм в виде 10m
		public string TF => Candle.Unit(CdiId).TF;
		// Количество свечей в графике
		public string CandlesCountStr
		{
			get
			{
				int count = Candle.Unit(CdiId).RowsCount;
				return Candle.CountTxt(count);
			}
		}




		// Результат теста
		public bool IsTested => ProfitCount > 0 || LossCount > 0;
		public int ProfitCount { get; set; }    // Количество прибыльных результатов
		public int LossCount { get; set; }      // Количество убыточных результатов
		// Процент прибыльности
		public int ProfitPercent
		{
			get
			{
				if (ProfitCount == 0)
					return 0;
				return 100 - (int)Math.Round((double)LossCount / (double)ProfitCount * (double)100);
			}
		}


		// Сохранение результатов теста паттерна
		public void TesterSave()
		{
			string sql = "UPDATE`_pattern_found`" +
						$"SET`profitCount`={ProfitCount}," +
						   $"`lossCount`={LossCount} " +
						$"WHERE`id`={Id}";
			my.Main.Query(sql);

			sql = "UPDATE`_pattern_search`" +
				  "SET`testedCount`=(" +
									 "SELECT COUNT(*)" +
									 "FROM`_pattern_found`" +
									$"WHERE`searchId`={SearchId}" +
									"  AND(`profitCount`OR`lossCount`)" +
								   ") " +
				 $"WHERE`id`={SearchId}";
			my.Main.Query(sql);

			new Patterns();
			G.Pattern.PatternArchive.SearchList();
		}
	}
}
