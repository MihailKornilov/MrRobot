using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using static System.Console;

using MySqlConnector;
using Newtonsoft.Json;
using MrRobot.inc;
using MrRobot.Connector;
using MrRobot.Interface;
using System.Security.Cryptography;
using System.Windows.Documents;
using System.Windows.Controls;

namespace MrRobot.Entity
{
	public class Candle
	{
		public delegate void DLGT();
		public static DLGT Updated { get; set; }

		/// <summary>
		/// Список доступных скачанных свечных данных
		/// </summary>
		static List<CDIunit> CDIlist { get; set; }
		/// <summary>
		/// Ассоциативный массив ID и свечных данных (для быстрого поиска)
		/// </summary>
		static Dictionary<int, CDIunit> ID_UNIT { get; set; }

		/// <summary>
		/// Загрузка из базы списка свечных данных
		/// </summary>
		public Candle()
		{
			CDIlist = new List<CDIunit>();
			ID_UNIT = new Dictionary<int, CDIunit>();

			string sql = "SELECT*" +
						 "FROM`_candle_data_info`" +
						 "ORDER BY`exchangeId`,`timeFrame`";
			my.Main.Delegat(sql, res =>
			{
				var unit = new CDIunit(res);
				CDIlist.Add(unit);
				ID_UNIT.Add(unit.Id, unit);
			});

			new Patterns();
			Updated?.Invoke();
		}

		/// <summary>
		/// Получение всего списка свечных данных
		/// </summary>
		public static List<CDIunit> ListAll(string txt = "")
		{
			if(txt.Length == 0)
				return CDIlist;

			var send = new List<CDIunit>();
			foreach (var v in CDIlist)
				if (v.Name.Contains(txt.ToUpper()))
					send.Add(v);
	
			return send;

		}

		/// <summary>
		/// Получение группы свечных данных
		/// </summary>
		public static List<CDIunit> ListGroup()
		{
			var ass = new Dictionary<string, int>();
			var send = new List<CDIunit>();
			foreach (var v in CDIlist)
			{
				if (ass.ContainsKey(v.Name))
				{
					int index = ass[v.Name];
					send[index].Num++;
					continue;
				}
				ass.Add(v.Name, send.Count);
				v.Num = 1;
				send.Add(v);
			}

			return send;
		}
		/// <summary>
		/// Получение списка свечных данных с таймфреймом 1m с учётом поиска
		/// </summary>
		public static List<CDIunit> List1m(string txt = "")
		{
			var send = new List<CDIunit>();
			var num = 0;
			foreach (var v in CDIlist)
			{
				if (v.TimeFrame != 1)
					continue;
				if (txt.Length > 0)
					if(!v.Name.Contains(txt.ToUpper()))
						continue;

				v.Num = num++;
				send.Add(v);
			}

			return send;
		}

		/// <summary>
		/// Получение списка свечных данных по ID инструмента
		/// </summary>
		// iid - ID инструмента
		// TF1Enable - включать таймфрейм 1
		public static List<CDIunit> ListOnIID(int iid, bool TF1Enable = true)
		{
			var send = new List<CDIunit>();

			foreach (var v in CDIlist)
			{
				if (v.InstrumentId != iid)
					continue;
				if (!TF1Enable && v.TimeFrame == 1)
					continue;

				send.Add(v);
			}

			return send;
		}

		/// <summary>
		/// Получение идентификаторов свечных данных по Name
		/// </summary>
		// name - Name инструмента в виде USDT/BTC
		// TFs - таймфреймы, будет получение
		public static int[] IdsOnSymbol(string name, string TFs)
		{
			var list = new List<int>();
			int[] ids = Array.ConvertAll(TFs.Split(','), x => int.Parse(x));
			
			foreach (var v in CDIlist)
				if (v.Name == name)
					if (ids.Contains(v.TimeFrame))
						list.Add(v.Id);

			return list.ToArray();
		}


		/// <summary>
		/// Единица информации свечных данных на основании ID
		/// </summary>
		public static CDIunit Unit(int id) => ID_UNIT.ContainsKey(id) ? ID_UNIT[id] : null;
		public static int Id(int id) => Unit(id) == null ? 0 : id;

		/// <summary>
		/// Свечные данные по Инструменту
		/// </summary>
		public static CDIunit UnitOnSymbol(string name, int tf = 1)
		{
			foreach (var cdi in CDIlist)
				if(cdi.Name == name)
					if (cdi.TimeFrame == tf)
						return cdi;

			return null;
		}

		/// <summary>
		/// Существуют ли свечные данные с указанный таймфреймом
		/// </summary>
		public static bool IsTFexist(string name, int tf = 1)
		{
			return UnitOnSymbol(name, tf) != null;
		}

		// Количество свечных данных для конкретного инструмента
		public static int CdiCount(int iid)
		{
			int count = 0;
			foreach(var cdi in CDIlist)
				if(cdi.InstrumentId == iid)
					count++;
			return count;
		}

		/// <summary>
		/// Удаление единицы свечных данных из списка
		/// </summary>
		public static void UnitDel(int id)
		{
			if (CDIlist == null)
				return;
			if (CDIlist.Count == 0)
				return;
			if(!ID_UNIT.ContainsKey(id))
				return;

			var unit = ID_UNIT[id];
			
			string sql = $"DROP TABLE IF EXISTS`{unit.Table}`";
			my.Main.Query(sql);

			sql = $"DELETE FROM`_candle_data_info`WHERE`id`={id}";
			my.Main.Query(sql);

			// Удаление Архива поисков паттернов
			sql = $"SELECT`id`FROM`_pattern_search`WHERE`cdiId`={id}";
			string ids = my.Main.Ids(sql);

			sql = $"DELETE FROM`_pattern_found`WHERE`searchId`IN({ids})";
			my.Main.Query(sql);

			sql = $"DELETE FROM`_pattern_search`WHERE`cdiId`={id}";
			my.Main.Query(sql);

			new Candle();
			BYBIT.Instrument.CdiCountUpd(unit.InstrumentId);
		}











		/// <summary>
		/// Создание таблицы со свечными данными, если не существует
		/// </summary>
		public static void TableCreate(CDIparam prm)
		{
			string sql = $"DROP TABLE IF EXISTS`{prm.Table}`";
			my.Main.Query(sql);

			sql = $"DELETE FROM`_candle_data_info`WHERE`id`={prm.Id}";
			my.Main.Query(sql);

			sql = $"CREATE TABLE`{prm.Table}`(" +
						"`unix` INT UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`high` DECIMAL(20,{prm.Decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`open` DECIMAL(20,{prm.Decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`close`DECIMAL(20,{prm.Decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`low`  DECIMAL(20,{prm.Decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
						"`vol`  DECIMAL(30,8) UNSIGNED DEFAULT 0 NOT NULL," +
						"PRIMARY KEY(`unix`)" +
				  $")ENGINE=MyISAM DEFAULT CHARSET=cp1251";
			my.Main.Query(sql);
		}
		public static string DataTableCreate(string exchange, string symbol, int tf, int decimals)
		{
			string TableName = $"{exchange}_{symbol.ToLower()}_{tf}";

			string sql = $"DROP TABLE IF EXISTS`{TableName}`";
			my.Main.Query(sql);
			
			sql = $"DELETE FROM`_candle_data_info`WHERE`table`='{TableName}'";
			my.Main.Query(sql);

			sql = $"CREATE TABLE`{TableName}`(" +
						"`unix` INT UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`high` DECIMAL(20,{decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`open` DECIMAL(20,{decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`close`DECIMAL(20,{decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
					   $"`low`  DECIMAL(20,{decimals}) UNSIGNED DEFAULT 0 NOT NULL," +
						"`vol`  DECIMAL(30,8) UNSIGNED DEFAULT 0 NOT NULL," +
						"PRIMARY KEY(`unix`)" +
				  $")ENGINE=MyISAM DEFAULT CHARSET=cp1251";
			my.Main.Query(sql);

			return TableName;
		}

		/// <summary>
		/// Внесение в базу сформированных свечных записей
		/// </summary>
		public static void DataInsert(string table, List<string> insert, int CountMin = 0)
		{
			if (insert.Count == 0)
				return;
			if (CountMin > 0 && insert.Count < CountMin)
				return;

			string sql = $"INSERT INTO`{table}`" +
						  "(`unix`,`high`,`open`,`close`,`low`,`vol`)" +
						 $"VALUES{string.Join(",", insert.ToArray())}";
			my.Main.Query(sql);
			insert.Clear();
		}

		/// <summary>
		/// SQL запрос для внесения информации о свечных данных
		/// </summary>
		static int CDIqueryInsert(int id,
								  int exchangeId,
								  int instrumentId,
								  int tf,
								  int decimals,
								  string table="",
								  int rowsCount=0,
								  string begin = "",
								  string end = "",
								  int convertedFromId = 0)
		{
			string sql = "INSERT INTO`_candle_data_info`(" +
							"`id`," +
							"`exchangeId`," +
							"`instrumentId`," +
							"`timeFrame`," +
							"`table`," +
							"`decimals`," +
							"`rowsCount`," +
							"`begin`," +
							"`end`," +
							"`convertedFromId`" +
						")VALUES(" +
							$"{id}," +
							$"{exchangeId}," +
							$"{instrumentId}," +
							$"{tf}," +
							$"{decimals}," +
							$"'{table}'," +
							$"{rowsCount}," +
							$"'{begin}'," +
							$"'{end}'," +
							$"{convertedFromId}" +
						")ON DUPLICATE KEY UPDATE" +
							"`rowsCount`=VALUES(`rowsCount`)," +
							"`begin`=VALUES(`begin`)," +
							"`end`=VALUES(`end`)";
			return my.Main.Query(sql);
		}
		/// <summary>
		/// Внесение заголовка свечных данных
		/// </summary>
		public static void InfoCreate(CDIparam prm)
		{
			prm.Id = CDIqueryInsert(0, prm.ExchangeId, prm.InstrumentId, prm.TimeFrame, prm.Decimals);
			TableCreate(prm);
		}
		public static int InfoCreate(string table, int ConvertedFromId = 0)
		{
			string sql = "SELECT " +
							"COUNT(*)`count`," +
							"MIN(FROM_UNIXTIME(`unix`))`begin`," +
							"MAX(FROM_UNIXTIME(`unix`))`end`" +
						 $"FROM`{table}`" +
						  "LIMIT 1";
			var data = my.Main.Row(sql);
			int count = Convert.ToInt32(data["count"]);

			if (count == 0)
				return 0;

			string[] spl = table.Split('_');
			string prefix = spl[0];
			string symbol = spl[1].ToUpper();
			int tf = Convert.ToInt32(spl[2]);

			int ExchangeId = G.Exchange.UnitOnField("Prefix", prefix).Id;
			int iid = 0;
			switch (ExchangeId)
			{
				case 1: iid = BYBIT.Instrument.UnitOnField("Symbol", symbol).Id; break;
				case 2: iid =  MOEX.Instrument.UnitOnField("Symbol", symbol).Id; break;
			}

			//int CdiId = CDIqueryInsert(0, ExchangeId, iid, tf, table, count, data["begin"], data["end"], ConvertedFromId);

			return 0;
		}
		/// <summary>
		/// Проверка соответствия скачанных данных с заголовками
		/// </summary>
		public static void DataControl(string tableLike = "bybit_", IProgress<decimal> prgs = null)
		{
			var mass = new List<string>();
			string sql = $"SHOW TABLES LIKE '{tableLike}%'";
			my.Main.Delegat(sql, res => mass.Add(res.GetString(0)));

			if (mass.Count == 0)
				return;

			var bar = new ProBar(mass.Count);
			for (int i = 0; i < mass.Count; i++)
			{
				string tableName = mass[i];

				sql = $"SELECT " +
							$"COUNT(*)`count`," +
							$"MIN(FROM_UNIXTIME(`unix`))`begin`," +
							$"MAX(FROM_UNIXTIME(`unix`))`end`" +
					  $"FROM`{tableName}`";
				var res = my.Main.Row(sql);

				int count = Convert.ToInt32(res["count"]);
				string begin = res["begin"];
				string end = res["end"];

				if (bar.isUpd(i))
					prgs.Report(bar.Value);

				if (count == 0)
				{
					sql = $"DROP TABLE`{tableName}`";
					my.Main.Query(sql);

					sql = $"DELETE FROM`_candle_data_info`WHERE`table`='{tableName}'";
					my.Main.Query(sql);

					continue;
				}

				// Получение названия инструмента и таймфрейма из названия таблицы
				string[] sp = tableName.Split('_');
				string symbol = sp[1].ToUpper();
				int tf = Convert.ToInt32(sp[2]);

				// Получение данных об инструменте по его названию
				var Instr = BYBIT.Instrument.UnitOnField("Symbol", symbol);

				// Проверка на наличие заголовка истории
				sql = "SELECT*" +
					  "FROM`_candle_data_info`" +
					 $"WHERE`table`='{tableName}'";
				res = my.Main.Row(sql);
				int id = res.Count != 0 ? Convert.ToInt32(res["id"]) : 0;

				// Внесение заголовка истории
				//CDIqueryInsert(id, 1, Instr.Id, tf, tableName, count, begin, end);
			}
		}



		/// <summary>
		/// Количество свечей в виде текста: "1 234 свечи"
		/// </summary>
		public static string CountTxt(int count, bool useNum = true)
		{
			if (count == 0)
				return "";

			string countStr = useNum ? format.Num(count) : count.ToString();
			return $"{countStr} свеч{format.End(count, "а", "и", "ей")}";
		}

		/// <summary>
		/// Получение времени в формате UNIX для определённого таймфрейма
		/// </summary>
		public static int UnixTF(int unix, int tf = 1)
		{
			int MinuteTotal = unix / 60;
			int MinuteDay = MinuteTotal / 1440 * 1440;
			int ost = (MinuteTotal - MinuteDay) % tf;

			return (MinuteTotal - ost) * 60;
		}

		/// <summary>
		/// Загрузка свечей с бриржи для выбранного инструмента
		/// </summary>
		public static List<object> WCkline(string symbol, int start = 0)
		{
			var dur = new Dur();

			start = start == 0 ? format.UnixNow() - 59_880 : start + 60;
			string url = "https://api.bybit.com/v5/market/kline?category=spot" +
						"&symbol=" + symbol +
						"&interval=1" +
						"&start=" + start + "000" +
						"&limit=1000";
			string json = new WebClient().DownloadString(url);
			dynamic arr = JsonConvert.DeserializeObject(json);

			var TF1List = new List<object>();

			if (arr.retMsg == null)
				return TF1List;
			if (arr.retMsg != "OK")
				return TF1List;

			var list = arr.result.list;
			if (list.Count == 0)
				return TF1List;

			WriteLine($"{url}   {list.Count}   {dur.Second()}");

			for (int k = 0; k < list.Count; k++)
			{
				var cndl = new CandleUnit(list[k]);
				if (cndl.Unix >= start)
					TF1List.Add(cndl);
			}

			return TF1List;
		}
	}



	/// <summary>
	/// Единица информации свечных данных: Candle Data Info
	/// </summary>
	public class CDIunit
	{
		public CDIunit(dynamic res)
		{
			string begin = res.GetMySqlDateTime("begin").ToString();
			Id				= res.GetInt32("id");
			ExchangeId		= res.GetInt32("exchangeId");
			InstrumentId	= res.GetInt32("instrumentId");
			Table			= res.GetString("table");
			TimeFrame		= res.GetInt32("timeFrame");
			RowsCount		= res.GetInt32("rowsCount");
			DateBegin		= begin.Substring(0, 10);
			DateEnd			= res.GetMySqlDateTime("end").ToString().Substring(0, 10);
			UnixBegin		= format.UnixFromDate(begin);
			ConvertedFromId	= res.GetInt32("convertedFromId");
		}
		public int Num { get; set; }            // Порядковый номер
		public int Id { get; set; }             // ID свечных данных
		public int ExchangeId { get; set; }     // ID биржи
		public int InstrumentId { get; set; }   // ID инструмента
		public SpisokUnit IUnit						// Данные об инструменте
		{
			get
			{
				switch (ExchangeId)
				{
					default:
					case 1: return BYBIT.Instrument.Unit(InstrumentId);
					case 2: return  MOEX.Instrument.Unit(InstrumentId);
				}
			}
		}
		public string Symbol => IUnit.Symbol;   // Название инструмента в виде "BTCUSDT"
		public string Name						// Название инструмента в виде "BTC/USDT"
		{
			get
			{
				switch (ExchangeId)
				{
					default:
					case 1: return IUnit.SymbolName;
					case 2: return IUnit.Symbol;
				}
			}
		}
		public string Table { get; set; }       // Имя таблицы со свечами
		public int TimeFrame { get; set; }      // Таймфрейм в виде 15
		public string TF => format.TF(TimeFrame); // Таймфрейм в виде "15m"
		public int RowsCount { get; set; }      // Количество свечей в графике (в таблице)
		public string DateBegin { get; set; }   // Дата начала графика в формате 12.03.2022
		public string DateEnd { get; set; }     // Дата конца графика в формате 12.03.2022
		public string DatePeriod => $"{DateBegin}-{DateEnd}";  // Диапазон даты от начала до конца всего графика в формате 12.03.2022 - 30.11.2022
		public int UnixBegin { get; set; }      // Время начала графика в формате Unix
		public int ConvertedFromId { get; set; }// ID минутного таймфрейма, с которого была произведена конвертация


		public double TickSize => IUnit.TickSize;// Шаг цены
		public int Decimals => IUnit.Decimals;	 // Количество нулей после запятой
		public ulong Exp => format.Exp(Decimals);
	}

	/// <summary>
	/// Настройки для скачивания или конвертации свечных данных
	/// </summary>
	public class CDIparam
	{
		public bool IsProcess { get; set; } = true; // Флаг выполнения фонового процесса
		public int Id { get; set; }                 // ID свечных данных
		public int ExchangeId { get; set; }			// ID биржи
		public int InstrumentId { get; set; }		// ID инструмента
		public string Table							// Имя таблицы со свечными данными
		{
			get
			{
				string prefix = G.Exchange.Unit(ExchangeId).Prefix;
				return $"{prefix}_{Id}";
			}
		}
		public string Symbol { get; set; }
		public int TimeFrame { get; set; }
		public int Decimals { get; set; }			// Нулей после запятой
		public IProgress<decimal> Progress { get; set; }
		public ProBar Bar { get; set; }             // Основная линия Прогресс-бара


		// Для History
		public int CC { get; set; }                 // CandlesCount - сколько свечей загружено (в процессе)
		public int UnixStart { get; set; }
		public int UnixFinish { get; set; }

 
		// Для Converter
		public double ProgressMainValue { get; set; }// Значение, которое будет отображаться Main-прогресс-бар
		public int TfNum { get; set; }              // Номер конвертации, если было выбрано несколько таймфреймов
		public int[] ConvertedIds { get; set; }     // ID сконвертированных свечных данных
	}

	/// <summary>
	/// Данные об одной свече
	/// </summary>
	public class CandleUnit
	{
		public CandleUnit() { }
		// Свечные данные из биржи
		public CandleUnit(dynamic v)
		{
			string str = v[0].ToString();
			if (str.Contains("000"))
				Unix = Convert.ToInt32(str.Substring(0, 10));
			else
				Unix = format.UnixFromDate(str);

			High   = v[2];
			Open   = v[1];
			Close  = v[4];
			Low    = v[3];
			Volume = v[5];
		}
		// Свечные данные из базы данных
		public CandleUnit(MySqlDataReader res)
		{
			Unix   = res.GetInt32("unix");
			High   = res.GetDouble("high");
			Open   = res.GetDouble("open");
			Close  = res.GetDouble("close");
			Low    = res.GetDouble("low");
			Volume = res.GetDouble("vol");
		}
		public CandleUnit(CandleUnit src, int tf)
		{
			Unix   = Candle.UnixTF(src.Unix, tf);
			High   = src.High;
			Open   = src.Open;
			Close  = src.Close;
			Low    = src.Low;
			Volume = src.Volume;
			TimeFrame = tf;
		}
		public CandleUnit(CandleUnit src, double PriceMax, double PriceMin, int PrecisionPercent)
		{
			Unix = src.Unix;
			High = src.High;
			Open = src.Open;
			Close = src.Close;
			Low = src.Low;
			PatternCalc(PriceMax, PriceMin, PrecisionPercent);
		}


		// Клонирование текущей свечи для создания новой с другим таймфреймом
		public CandleUnit Clone(int tf)
		{
			return new CandleUnit(this, tf);
		}


		public int Unix { get; set; }           // Время свечи в формате Unix согласно Таймфрейму
		public string DateTime => format.DTimeFromUnix(Unix);
		public double High { get; set; }        // Максимальная цена свечи
		public double Open { get; set; }        // Цена открытия
		public double Close { get; set; }       // Цена закрытия
		public double Low { get; set; }         // Минимальная цена
		public double Volume { get; set; }      // Объём


		public int TimeFrame { get; set; } = 1; // Таймфрейм свечи
		int UpdCount { get; set; } = 1;         // Количество обновлений свечи единичными таймфреймами
		public bool IsFull => UpdCount == TimeFrame; // Свеча заполнена единичными таймфреймами


		// Обновление свечи (для динамического графика)
		public void Update(int unix, double price = 0, double volume = 0)
		{
			// Unix-время свечи на основании Таймфрейма
			int UnixTF = Candle.UnixTF(unix, TimeFrame);

			if (Unix == UnixTF)
			{
				// Обновление цены закрытия
				if (price > 0)
				{
					Close = price;
					if (High < price)
						High = price;
					if (Low > price)
						Low = price;
				}

				// Обновление объёма
				Volume += volume;

				return;
			}

			// Новая свеча
			Unix = UnixTF;
			Close = price > 0 ? price : Close;
			High = Close;
			Open = Close;
			Low = Close;
			Volume = volume;
		}
		// Обновление свечи согласно таймфрейму
		public bool Upd(CandleUnit src)
		{
			if (Unix != Candle.UnixTF(src.Unix, TimeFrame))
				return false;

			Close = src.Close;

			if (High < src.High)
				High = src.High;
			if (Low > src.Low)
				Low = src.Low;

			Volume += src.Volume;

			UpdCount++;

			return true;
		}


		// Зелёная свеча или нет
//        public bool IsGreen { get { return Close >= Open; } }
		bool _IsGreen;
		bool _IsGreenSetted;
		public bool IsGreen
		{
			get
			{
				if (_IsGreenSetted)
					return _IsGreen;
				_IsGreen = Close >= Open;
				_IsGreenSetted = true;
				return _IsGreen;
			}
		}

		// Размеры в процентах умноженные на 100
		public int SpaceTop { get; set; }   // Верхнее пустое поле 
		public int WickTop  { get; set; }   // Верхний хвост
		public int Body     { get; set; }   // Тело
		public int WickBtm  { get; set; }   // Нижний хвост
		public int SpaceBtm { get; set; }   // Нижнее пустое поле


		// Минимальные и максимальные размеры на основании PrecisionPercent (для сравнения паттернов) 
		public int SpaceTopMin { get; set; }
		public int SpaceTopMax { get; set; }
		public int WickTopMin  { get; set; }
		public int WickTopMax  { get; set; }
		public int BodyMin     { get; set; }
		public int BodyMax     { get; set; }
		public int WickBtmMin  { get; set; }
		public int WickBtmMax  { get; set; }
		public int SpaceBtmMin { get; set; }
		public int SpaceBtmMax { get; set; }


		// Внесение в базу одной свечи
		public string Insert => $"({Unix},{High},{Open},{Close},{Low},{Volume})";
		public string View => $"{DateTime}   ОТКР {Open}   МАКС {High}   МИН {Low}   ЗАКР {Close}";


		// Обновление первой свечи в графике
		public string CandleToChart(bool withColor = false, bool msec = false)
		{
			string u000 = msec ? "000" : "";
			return "{" +
				$"time:{format.TimeZone(Unix)}{u000}," +
   (withColor ? $"color:'#{(IsGreen ? "60CE5E" : "FF324D")}'," : "") +
				$"high:{High}," +
				$"open:{Open}," +
				$"close:{Close}," +
				$"low:{Low}" +
			"}";
		}
		// Обновление объёма в графике
		public string VolumeToChart()
		{
			string color = Close > Open ? "#127350" : "#86303E";
			return "{" +
				$"time:{format.TimeZone(Unix)}," +
				$"value:{Volume}," +
				$"color:\"{color}\"" +
			"}";
		}

		// Присвоение процентных соотношений свечи относительно размера паттерна
		void PatternCalc(double PriceMax, double PriceMin, int PrecisionPercent)
		{
			// Размер паттерна в пунктах
			double Size = (PriceMax - PriceMin) / 10000;

			// Значения в процентах относительно размера паттерна
			SpaceTop = (int)Math.Round((PriceMax - High) / Size);
			WickTop  = (int)Math.Round((High - (IsGreen ? Close : Open)) / Size);
			Body     = (int)Math.Round((Close - Open) / Size);
			WickBtm  = (int)Math.Round(((IsGreen ? Open : Close) - Low) / Size);
			SpaceBtm = (int)Math.Round((Low - PriceMin) / Size);

			// Процент расхождения паттернов
			double prc = Math.Round((double)(100 - PrecisionPercent + 1) / 200, 3);

			int diff = (int)Math.Round(SpaceTop * prc);
			SpaceTopMin = SpaceTop - diff;
			SpaceTopMax = SpaceTop + diff;

			diff = (int)Math.Round(WickTop * prc);
			WickTopMin = WickTop - diff;
			WickTopMax = WickTop + diff;

			diff = (int)Math.Round(Body * prc) * (IsGreen ? 1 : -1);
			BodyMin = Body - diff;
			BodyMax = Body + diff;

			diff = (int)Math.Round(WickBtm * prc);
			WickBtmMin = WickBtm - diff;
			WickBtmMax = WickBtm + diff;

			diff = (int)Math.Round(SpaceBtm * prc);
			SpaceBtmMin = SpaceBtm - diff;
			SpaceBtmMax = SpaceBtm + diff;
		}
		// Структура свечи с учётом пустот сверху и снизу
		public string Struct()
		{
			return $"{(decimal)SpaceTop/100} " +
				   $"{(decimal)WickTop /100} " +
				   $"{(decimal)Body/100} " +
				   $"{(decimal)WickBtm/100} " +
				   $"{(decimal)SpaceBtm/100}";
		}
	}
}
