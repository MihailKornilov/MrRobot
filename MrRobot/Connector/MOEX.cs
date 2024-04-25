using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;

using Newtonsoft.Json;
using RobotLib;
using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Section;
using MrRobot.Interface;
using static MrRobot.Connector.MOEX;

namespace MrRobot.Connector
{
	public class MOEX
	{
		public const int ExchangeId = 2;        // ID МосБиржи
		public static Engine _Engine { get; set; }
		public static Market _Market { get; set; }
		public static BoardGroup _BoardGroup { get; set; }
		public static Board _Board { get; set; }
		public static SecGroup SGroup { get; set; }
		public static SecType SType { get; set; }
		public static Security Instrument { get; set; }

		public MOEX()
		{
			new Engine();
			new Market();
			new BoardGroup();
			new Board();
			new SecGroup();
			new SecType();
			new Security();
		}

		// Формирование страницы со всеми запросами ISS
//		public static void IssQueriesPage(SpisokUnit board)
//		{
//			string name = "MoexIssQueries";
//			string src = Path.GetFullPath($"Browser/{name}.tmp.html");
//			string dst = Path.GetFullPath($"Browser/{name}.html");

//			var read  = new StreamReader(src);
//			var write = new StreamWriter(dst);

//			string line;
//			while ((line = read.ReadLine()) != null)
//			{
///*
//				if (line.Contains("        /iss"))
//				{
//					line = line.Replace("        ", "");
//					line = $"    <a href='https://iss.moex.com{line}.json' target='_blank'>{line}</a>";
//				}
//				else if (line.Contains("        "))
//				{
//					line = line.Replace("        ", "");
//					line = $"    <div>{line}</div>";
//				}
//				else if (line.Length == 0)
//					line = "<br>";
//*/
//				line = line.Replace("[engine]", board.EngineName);
//				line = line.Replace("[market]", board.MarketName);
//				line = line.Replace("[board]",  board.Name);
//				line = line.Replace("[boardgroup]",  board.Group);
//				line = line.Replace("[security]", board.SecId);

//				write.WriteLine(line);
//			}
//			read.Close();
//			write.Close();
//		}

		// Информация о Бумаге и Режимы торгов
		public static dynamic[] SecurityInfoBoards(string secid)
		{
			var wc = new WebClient();
			wc.Encoding = Encoding.UTF8;
			string url = $"https://iss.moex.com/iss/securities/{secid}.json?" +
								"iss.meta=off" +
							   "&description.columns=name,title,value" +
							   "&boards.columns=" +
									"secid," +
									"boardid," +
									"is_primary," +
									"listed_from," +
									"listed_till," +
									"title," +
									"is_traded," +
									"decimals";
			WriteLine(url);
			string str = wc.DownloadString(url);
			dynamic json = JsonConvert.DeserializeObject(str);

			dynamic data = json.description.data;
			var SecInfoList = new List<SecurityInfoUnit>();
			for (int i = 0; i < data.Count; i++)
				SecInfoList.Add(new SecurityInfoUnit(data[i]));

			data = json.boards.data;
			var BoardsList = new List<BoardUnit>();
			for (int i = 0; i < data.Count; i++)
				BoardsList.Add(new BoardUnit(i+1, data[i]));

			return new dynamic[2] { SecInfoList, BoardsList };
		}

		// Параметры для загрузки свечных данных выбранного Режима торгов
		public static List<BorderUnit> BoardLoad(BoardUnit board)
		{
			var wc = new WebClient();
			wc.Encoding = Encoding.UTF8;
			string url = $"https://iss.moex.com/iss" +
								$"/engines/{board.EngineName}" +
								$"/markets/{board.MarketName}" +
								$"/boards/{board.Name}" +
								$"/securities/{board.SecId}" +
								 "/candleborders.json" +
								 "?iss.only=borders" +
								 "&iss.meta=off";
			WriteLine(url);
			string str = wc.DownloadString(url);
			dynamic json = JsonConvert.DeserializeObject(str);
			dynamic data = json.borders.data;

			var list = new List<BorderUnit>();
			if (data.Count == 0)
				return list;

			var dict = new Dictionary<int, BorderUnit>();
			for (int i = 0; i < data.Count; i++)
			{
				var unit = new BorderUnit(data[i]);
				dict.Add(unit.Interval, unit);
			}

			foreach (int i in BorderUnit.Sort)
				if (dict.ContainsKey(i))
					list.Add(dict[i]);

			return list;
		}

		// Загрузка свечных данных
		public static void CandlesLoad(BoardUnit board, int interval, string from, string till)
		{
			var PARAM = new CDIparam()
			{
				ExchangeId = ExchangeId,
				InstrumentId = Instrument.FieldToId("Symbol", board.SecId),
				TimeFrame = interval,
				Decimals = board.Decimals
			};

			Candle.CDIcreate(PARAM);

			var wc = new WebClient();
			wc.Encoding = Encoding.UTF8;

			while (true)
			{
				string url = $"https://iss.moex.com/iss" +
									$"/engines/{board.EngineName}" +
									$"/markets/{board.MarketName}" +
									$"/boards/{board.Name}" +
									$"/securities/{board.SecId}" +
									 "/candles.json" +
									 "?iss.only=candles" +
									 "&candles.columns=begin,open,high,low,close,volume" +
									 "&iss.meta=off" +
									$"&interval={interval}" +
									$"&from={from}" +
									$"&till={till}";
				WriteLine(url);
				string str = wc.DownloadString(url);
				dynamic json = JsonConvert.DeserializeObject(str);
				dynamic data = json.candles.data;

				var insert = new List<string>();

				if (data.Count == 0)
					break;

				int count = data.Count;
				if (data.Count == 500) count--;

				for (int i = 0; i < count; i++)
					insert.Add(new CandleUnit(data[i]).Insert);

				Candle.DataInsert(PARAM.Table, insert);

				if (data.Count < 500)
					break;

				from = data[count][0];
			}

			Candle.CDIupdate(PARAM);
		}


		// Запрос и получение данных от биржи по указанным данным
		static dynamic WsIssData(string value)
		{
			var wc = new WebClient();
			wc.Encoding = Encoding.UTF8;
			string url = $"https://iss.moex.com/iss/index.json?iss.only={value}&iss.meta=off";
			WriteLine(url);
			string str = wc.DownloadString(url);
			dynamic json = JsonConvert.DeserializeObject(str);
			return json[value].data;
		}

		/// <summary>
		/// Торговая система
		/// </summary>

		public class Engine : Spisok
		{
			public override string SQL =>
				"SELECT*" +
				"FROM`_moex_engines`" +
				"ORDER BY`title`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.Name = res.GetString("name");
				unit.Title = res.GetString("title");
				return unit;
			}

			public Engine() : base() => _Engine = this;


			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("engines");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = $"({v[0]},'{v[1]}','{v[2]}')";
				}

				string sql = "INSERT INTO`_moex_engines`" +
								"(`id`,`name`,`title`)" +
							$"VALUES{string.Join(",", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)";
				my.Main.Query(sql);

				new Engine();
			}
		}


		/// <summary>
		/// Рынки
		/// </summary>
		public class Market : Spisok
		{
			public override string SQL =>
				"SELECT*" +
				"FROM`_moex_markets`" +
				"ORDER BY`engineId`,`id`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.EngineId = res.GetInt32("engineId");
				unit.Name = res.GetString("name");
				unit.Title = res.GetString("title");
				return unit;
			}

			public Market() : base() => _Market = this;


			// Список рынков с учётом фильтра
			//public List<SpisokUnit> ListEngine()
			//{
			//	var send = new List<MoexUnit>();

			//	if(SecurityFilter.EngineId == 0)
			//		return send;

			//	for (int i = 0; i < UnitList.Count; i++)
			//	{
			//		var unit = UnitList[i];
			//		if (unit.EngineId == SecurityFilter.EngineId)
			//			if(unit.SecurityCount > 0)
			//				send.Add(unit);
			//	}
			//	return send;
			//}

			//// Обновление количеств бумаг на основании фильтра
			//public static void CountFilter()
			//{
			//	if (SecurityFilter.EngineId == 0)
			//		return;

			//	// Обнуление количеств бумаг
			//	foreach (var unit in ListEngine())
			//		unit.SecurityCountFilter = 0;

			//	foreach (var sec in Instrument.ListAll)
			//	{
			//		if (sec.EngineId != SecurityFilter.EngineId)
			//			continue;
			//		if (!SecurityFilter.IsAllowFast(sec))
			//			continue;

			//		var unit = Unit(sec.MarketId);
			//		if (unit != null)
			//			unit.SecurityCountFilter++;
			//	}
			//}

			// Порядковый номер в списке
			//public int FilterIndex()
			//{
			//	var list = ListEngine();
			//	for (int i = 0; i < list.Count; i++)
			//		if (list[i].Id == SecurityFilter.MarketId)
			//			return i;
			//	return 0;
			//}




			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("markets");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = $"({v[0]},{v[1]},'{v[4]}','{v[5]}')";
				}

				string sql = "INSERT INTO`_moex_markets`" +
								"(`id`,`engineId`,`name`,`title`)" +
							$"VALUES{string.Join(",", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`engineId`=VALUES(`engineId`)," +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)";
				my.Main.Query(sql);

				new Market();
			}
		}





		/// <summary>
		/// Группы режимов торгов
		/// </summary>
		public class BoardGroup : Spisok
		{
			public override string SQL =>
				"SELECT*FROM`_moex_boardgroups`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.EngineId  = res.GetInt32("engineId");
				unit.MarketId  = res.GetInt32("marketId");
				unit.Name	   = res.GetString("name");
				unit.Title	   = res.GetString("title");
				unit.IsTrading = res.GetInt16("isTrading") == 1;
				return unit;
			}

			public BoardGroup() : base() => _BoardGroup = this;

			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("boardgroups");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = "(" +
									$"{v[0]}," +
									$"{v[1]}," +    // engineId
									$"{v[4]}," +    // marketId
									$"'{v[6]}'," +  // name
									$"'{v[7]}'," +  // title
									$"{v[10]}" +    // isTrading
								")";
				}

				string sql = "INSERT INTO`_moex_boardgroups`(" +
								"`id`," +
								"`engineId`," +
								"`marketId`," +
								"`name`," +
								"`title`," +
								"`isTrading`" +
							$")VALUES{string.Join(",", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`engineId`=VALUES(`engineId`)," +
							 "`marketId`=VALUES(`marketId`)," +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)," +
							 "`isTrading`=VALUES(`isTrading`)";
				my.Main.Query(sql);

				new BoardGroup();
			}
		}

		/// <summary>
		/// Режимы торгов
		/// </summary>
		public class Board : Spisok
		{
			public override string SQL =>
				"SELECT*FROM`_moex_boards`";

			Dictionary<string, int> NAME_ID { get; set; } = new Dictionary<string, int>();

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.EngineId = res.GetInt32("engineId");
				unit.MarketId = res.GetInt32("marketId");
				unit.GroupId = res.GetInt32("groupId");
				unit.Name = res.GetString("name");
				unit.Title = res.GetString("title");
				unit.IsTrading = res.GetInt16("isTrading") == 1;
				NAME_ID.Add(unit.Name, unit.Id);
				return unit;
			}

			public Board() : base() => _Board = this;


			// ID режима торгов по названию
			public int IdOnName(string name) =>
				NAME_ID.ContainsKey(name) ? NAME_ID[name] : 0;

			// ID Торговой системы по ID режима торгов
			int EngineId(int boardId) =>
				ID_UNIT.ContainsKey(boardId) ? ID_UNIT[boardId].EngineId : 0;

			// Название Торговой системы по ID режима торгов
			public string EngineName(int boardId) =>
				_Engine.Unit(EngineId(boardId)).Name;


			// ID Рынка по ID режима торгов
			int MarketId(int boardId) =>
				ID_UNIT.ContainsKey(boardId) ? ID_UNIT[boardId].MarketId : 0;

			// Название Рынка по ID режима торгов
			public string MarketName(int boardId) =>
				_Market.Unit(MarketId(boardId)).Name;


			// Название Группы режима по ID режима торгов
			public string Group(int boardId)
			{
				int GroupId = Unit(boardId).GroupId;
				return _BoardGroup.Unit(GroupId).Name;
			}


			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("boards");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = "(" +
									$"{v[0]}," +
									$"{v[1]}," +
									$"{v[2]}," +
									$"{v[3]}," +
									$"'{v[4]}'," +
									$"'{v[5]}'," +
									$"{v[6]}" +
								")";
				}

				string sql = "INSERT INTO`_moex_boards`(" +
								"`id`," +
								"`groupId`," +
								"`engineId`," +
								"`marketId`," +
								"`name`," +
								"`title`," +
								"`isTrading`" +
							$")VALUES{string.Join(",", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`groupId`=VALUES(`groupId`)," +
							 "`engineId`=VALUES(`engineId`)," +
							 "`marketId`=VALUES(`marketId`)," +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)," +
							 "`isTrading`=VALUES(`isTrading`)";
				my.Main.Query(sql);

				new Board();
			}
		}






		/// <summary>
		/// Группы бумаг
		/// </summary>
		public class SecGroup : Spisok
		{
			public override string SQL =>
				"SELECT*" +
				"FROM`_moex_securitygroups`" +
				"ORDER BY`title`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.Name = res.GetString("name");
				unit.Title = res.GetString("title");
				return unit;
			}

			public SecGroup() : base() => SGroup = this;



			// Записи, в которых есть бумаги
			public List<SpisokUnit> ListActual()
			{
				var send = new List<SpisokUnit>();
				foreach (var unit in AllWithNull("любая"))
					//if (unit.Id == 0 || unit.SecCount > 0)
						send.Add(unit);
				return send;
			}

			// Порядковый номер в списке
			public int FilterIndex()
			{
				var list = ListActual();
				for (int i = 0; i < list.Count; i++)
					if (list[i].Id == SecurityFilter.GroupId)
						return i;
				return 0;
			}

			// Обновление количеств бумаг на основании фильтра
			public void CountFilter()
			{
				// Обнуление количеств бумаг
				foreach (var unit in ListActual())
					unit.Count1 = 0;

				foreach (var sec in Instrument.ListAll)
					if (SecurityFilter.IsAllowFast(sec))
						Unit(sec.GroupId).Count1++;
			}




			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("securitygroups");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = "(" +
									$"{v[0]}," +
									$"'{v[1]}'," +  // name
									$"'{v[2]}'" +  // title
								")";
				}

				string sql = "INSERT INTO`_moex_securitygroups`(" +
								"`id`," +
								"`name`," +
								"`title`" +
							$")VALUES{string.Join(",\n", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)";
				my.Main.Query(sql);

				new SecGroup();
			}
		}
		/// <summary>
		/// Виды бумаг
		/// </summary>
		public class SecType : Spisok
		{
			public override string SQL =>
				"SELECT*" +
				"FROM`_moex_securitytypes`" +
				"ORDER BY`engineId`,`id`";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.EngineId = res.GetInt32("engineId");
				unit.GroupId  = res.GetInt32("groupId");
				unit.Name	  = res.GetString("name");
				unit.Title	  = res.GetString("title");
				return unit;
			}

			public SecType() : base() => SType = this;

			// Загрузка данных с биржи
			public static void iss()
			{
				var data = WsIssData("securitytypes");
				string[] values = new string[data.Count];
				for (int i = 0; i < data.Count; i++)
				{
					var v = data[i];
					values[i] = "(" +
									$"{v[0]}," +
									$"{v[1]}," +    // engineId
									$"{SGroup.FieldToId("Name", v[6].ToString())}," +    // groupId
									$"'{v[4]}'," +  // name
									$"'{v[5]}'" +   // title
								")";
				}

				string sql = "INSERT INTO`_moex_securitytypes`(" +
								"`id`," +
								"`engineId`," +
								"`groupId`," +
								"`name`," +
								"`title`" +
							$")VALUES{string.Join(",", values)}" +
							 "ON DUPLICATE KEY UPDATE" +
							 "`engineId`=VALUES(`engineId`)," +
							 "`groupId`=VALUES(`groupId`)," +
							 "`name`=VALUES(`name`)," +
							 "`title`=VALUES(`title`)";
				my.Main.Query(sql);

				new SecType();
			}
		}
		/// <summary>
		/// Бумаги
		/// </summary>
		public class Security : Spisok
		{
			public override string SQL =>
					"SELECT" +
						"`id`," +
						"`moexId`," +
						"`groupId`," +
						"`typeId`," +
						"`symbol`," +
						"`shortName`," +
						"`name`," +
						"`isTrading`" +
					"FROM`_instrument`" +
				   $"WHERE`exchangeId`={ExchangeId}";

			public override SpisokUnit UnitFieldsFill(SpisokUnit unit, dynamic res)
			{
				unit.MoexId	   = res.GetInt32("moexId");
				unit.GroupId   = res.GetInt32("groupId");
				unit.TypeId    = res.GetInt32("typeId");
				unit.Symbol	   = res.GetString("symbol");
				unit.ShortName = res.GetString("shortName");
				unit.Name	   = res.GetString("name");
				unit.IsTrading = res.GetInt16("isTrading") == 1;
				return unit;
			}

			public Security() : base()
			{
				UnitList = UnitList.OrderBy(x => x.Symbol).ToList();
				Instrument = this;
			}


			// Текст: "1403 бумаги"
			public string CountStr(int c = -1)
			{
				c = c == -1 ? Count : c;
				return $"{c} бумаг{format.End(c, "а", "и", "")}";
			}

			public int FoundCount()
			{
				int count = 0;
				foreach (var unit in UnitList)
					if (SecurityFilter.IsAllow(unit))
					count++;
				return count;
			}

			public List<SpisokUnit> ListFilter()
			{
				var send = new List<SpisokUnit>();
				foreach (var unit in UnitList)
				{
					if (SecurityFilter.IsAllow(unit))
						send.Add(unit);
					if (send.Count >= 300)
						break;
				}
				return send;
			}

			// Загрузка данных с биржи
			public static void iss()
			{
				// Обнуление активности существующих инструментов
				string sql = "UPDATE`_instrument`" +
							 "SET`isTrading`=0 " +
							$"WHERE`exchangeId`={ExchangeId}";
				my.Main.Query(sql);

				var wc = new WebClient();
				wc.Encoding = Encoding.UTF8;

				int start = 0;
				while (true)
				{
					string url = "https://iss.moex.com/iss/securities.json" +
									 "?iss.meta=off" +
									 "&securities.columns=" +
											"id," +
											"group," +
											"type," +
											"secid," +
											"shortname," +
											"name" +
									 "&is_trading=1" +
									$"&start={start}";
					string str = wc.DownloadString(url);
					dynamic json = JsonConvert.DeserializeObject(str);
					var data = json.securities.data;

					WriteLine(url);

					if (data.Count == 0)
						break;

					string[] values = new string[data.Count];
					for (int i = 0; i < data.Count; i++)
					{
						var v = data[i];
						int id = Instrument.FieldToId("MoexId", Convert.ToInt32(v[0]));
						int groupId = SGroup.FieldToId("Name", v[1].ToString());
						int typeId  = SType.FieldToId("Name", v[2].ToString());
						string shortName = v[4].ToString().Replace("'", ""),
									name = v[5].ToString().Replace("'", "");
						values[i] = "(" +
										$"{id}," +
										$"{ExchangeId}," +
										$"{v[0]}," +        // MoexId
										$"{groupId}," +
										$"{typeId}," +
										$"'{v[3]}'," +      // secId
										$"'{shortName}'," +
										$"'{name}'," +
										 "1" +
									")";
					}

					sql = "INSERT INTO`_instrument`(" +
								"`id`," +
								"`exchangeId`," +
								"`moexId`," +
								"`groupId`," +
								"`typeId`," +
								"`symbol`," +
								"`shortName`," +
								"`name`," +
								"`isTrading`" +
						 $")VALUES{string.Join(",\n", values)}" +
						   "ON DUPLICATE KEY UPDATE" +
								"`groupId`=VALUES(`groupId`)," +
								"`typeId`=VALUES(`typeId`)," +
								"`symbol`=VALUES(`symbol`)," +
								"`shortName`=VALUES(`shortName`)," +
								"`name`=VALUES(`name`)," +
								"`isTrading`=VALUES(`isTrading`)";
					my.Main.Query(sql);

					start += 100;
				}

				new Security();
			}
		}
	}



	/// <summary>
	/// Единица данных информации о бумаге
	/// </summary>
	public class SecurityInfoUnit
	{ 
		public SecurityInfoUnit(dynamic v)
		{
			Name = v[0];
			Title = v[1];
			Value = v[2];
		}
		public string Name { get; set; }
		string _Title;
		public string Title
		{
			get => $"{_Title}:";
			set => _Title = value;
		}
		public string Value { get; set; }
		public string ValueWeight => Name == "SECID" ? "Medium" : "Normal";
	}


	/// <summary>
	/// Единица данных Режима торгов бумаги
	/// </summary>
	public class BoardUnit
	{ 
		public BoardUnit(int num, dynamic v)
		{
			Num = $"{num}.";
			SecId = v[0];
			Name = v[1];
			IsPrimary = v[2];
			ListedFrom = v[3];
			ListedTill = v[4];
			Title = v[5];
			IsTraded = v[6];
			Decimals = v[7];
		}
		public string Num { get; set; }
		public string SecId { get; set; }
		public string EngineName => _Board.EngineName(Id);
		public string MarketName => _Board.MarketName(Id);
		public int Id => _Board.IdOnName(Name);
		public string Name { get; set; }
		public string NameWeight => IsPrimary ? "Bold" : "Normal";
		public string Group => _Board.Group(Id);
		public string Title { get; set; }
		public string ListedFrom { get; set; }
		public string ListedTill { get; set; }
		public SolidColorBrush ListedColor => format.RGB(IsTraded ? "#000000" : "#AAAAAA");
		public bool IsPrimary { get; set; }
		public bool IsTraded { get; set; }
		public SolidColorBrush ItemBG => format.RGB(IsTraded ? "#DDFFDD" : "#F8F8F8");
		public int Decimals { get; set; }
	}


	/// <summary>
	/// Доступные таймфреймы и даты для загрузки свечной истории
	/// </summary>
	public class BorderUnit
	{
		public BorderUnit(dynamic v)
		{
			Begin = Convert.ToDateTime(v[0]);
			End = Convert.ToDateTime(v[1]);
			Interval = v[2];
		}
		public int Interval { get; set; }
		public DateTime Begin { get; set; }
		public DateTime End { get; set; }
		public string TF => Duration(Interval);


		// Ассоциативный массив таймфреймов с описаниями
		static string Duration(int i)
		{
			var ass = new Dictionary<int, string>();
			ass.Add( 1, "Минута");
			ass.Add(10, "10 минут");
			ass.Add(60, "Час");
			ass.Add(24, "День");
			ass.Add( 7, "Неделя");
			ass.Add(31, "Месяц");
			ass.Add( 4, "Квартал");
			return ass[i];
		}
		// Порядок отображенмя таймфреймов
		public static int[] Sort => new[] { 1, 10, 60, 24, 7, 31, 4 };
	}
}
