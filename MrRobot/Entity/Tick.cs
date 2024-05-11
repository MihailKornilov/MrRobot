using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

using MrRobot.inc;
using System.Diagnostics;
using CefSharp.DevTools.CSS;
using static MrRobot.Section.OBTdb;
using System.Windows.Media.Animation;
using MySqlConnector;

namespace MrRobot.Entity
{
	public class Tick
	{












		// Внесение информации о тиковых данных
		public static void TDIcreate(CDIparam prm)
		{
			string sql = "INSERT INTO`_tick_data_info`(" +
							"`exchangeId`," +
							"`instrumentId`," +
							"`priceDecimals`," +
							"`qtyDecimals`" +
						")VALUES(" +
							$"{prm.ExchangeId}," +
							$"{prm.InstrumentId}," +
							$"{prm.Decimals}," +
							$"{prm.QtyDecimals}" +
						")";
			prm.Id = my.Main.Query(sql);

			DataTableCreate(prm);
		}
		// Создание таблицы со тиковыми данными, если не существует
		static void DataTableCreate(CDIparam prm)
		{
			string sql = $"DROP TABLE IF EXISTS`{prm.Table}`";
			my.Tick.Query(sql);

			sql = $"CREATE TABLE`{prm.Table}`(" +
						 "`unix`	BIGINT UNSIGNED DEFAULT 0," +
						$"`price`	DECIMAL(20,{prm.Decimals}) UNSIGNED DEFAULT 0," +
						$"`qty`		DECIMAL(20,{prm.QtyDecimals}) UNSIGNED DEFAULT 0," +
						 "`isBuy`	TINYINT UNSIGNED DEFAULT 0," +
						"PRIMARY KEY(`unix`)" +
				  $")ENGINE=MyISAM DEFAULT CHARSET=cp1251";
			my.Tick.Query(sql);
		}
		// Внесение в базу сформированных тиковых записей
		public static void DataInsert(string table, List<string> insert)
		{
			if (insert.Count == 0)
				return;

			string sql = $"INSERT INTO`{table}`" +
						  "(`unix`,`price`,`qty`,`isBuy`)" +
						 $"VALUES{string.Join(",", insert.ToArray())}";
			my.Tick.Query(sql);

			insert.Clear();
		}
		// Обновление информации о свечных данных
		public static void TDIupdate(CDIparam prm)
		{
			string sql = "SELECT " +
							"COUNT(*)`count`," +
							"MIN(`unix`)`start`," +
							"MAX(`unix`)`finish`" +
						 $"FROM`{prm.Table}`";
			var data = my.Tick.Row(sql);

			var start  = data["start"].Substring(0, 10);
			var finish = data["finish"].Substring(0, 10);
			sql = "UPDATE`_tick_data_info`" +
				 $"SET`table`='{prm.Table}'," +
					$"`rowsCount`={data["count"]}," +
					$"`start`={start}," +
					$"`finish`={finish} " +
				 $"WHERE`id`={prm.Id}";
			my.Main.Query(sql);
		}
	}


	public class TickUnit
	{
		public TickUnit(dynamic v)
		{
			Unix  = v.T;
			Price = v.p;
			Qty   = v.q;
			IsBuy = v.m;
		}
		// Данные из базы
		public TickUnit(MySqlDataReader res)
		{
			Unix  = res.GetInt64("unix");
			Price = res.GetDecimal("price");
		}

		public long Unix { get; set; }		// Время тика в формате UnixMs
		public decimal Price { get; set; }	// Цена
		public decimal Qty { get; set; }    // Объём
		public bool IsBuy { get; set; }     // Покупка или продажа

		// Внесение в базу одного тика
		public string Insert =>
			$"({Unix},{Price},{Qty},{(IsBuy ? 1 : 0)})";

		// Запись для графика
		public string ToChart =>
			$"{{value:{Price},time:{(int)(Unix/1000)}}}";
	}
}
