/*
	Ордера в тестере
 */

using System;
using System.Windows.Media;
using System.Collections.Generic;
using static System.Console;
using RobotLib;

namespace RobotAPI
{
	public static partial class Robot
	{
		/// <summary>
		/// Создание нового ордера на покупку по текущему курсу
		/// </summary>
		public static TesterOrderUnit MARKET_BUY(decimal qty)
		{
			var Order = new TesterOrderUnit("Market", "BUY", PRICE, qty);
			return ORDER_OPEN(Order);
		}


		/// <summary>
		/// Создание нового ордера на продажу по текущему курсу
		/// </summary>
		public static TesterOrderUnit MARKET_SELL(decimal qty)
		{
			var Order = new TesterOrderUnit("Market", "SELL", PRICE, qty);
			return ORDER_OPEN(Order);
		}


		// Создание лимитного ордера на покупку
		public static TesterOrderUnit LIMIT_BUY(decimal price, decimal qty)
		{
			var Order = new TesterOrderUnit("Limit", "BUY", price, qty);
			return ORDER_OPEN(Order);
		}

		// Проверка лимитных ордеров, которые нужно сделать активными
		static void OrderLimitCheck()
		{
			foreach (TesterOrderUnit order in ORDERS)
			{
				if (order.IsActive)
					continue;

				if (order.Side == "BUY")
					if (order.PriceOpen >= LOW)
					{
						order.IsActive = true;
						order.DTimeOpen = DATE_TIME;
						order.BalanceUpd();
					}
			}

//			INSTRUMENT.BaseBalance += (Order.Qty - Order.Commission);
		}


		/// <summary>
		/// Создание нового ордера и внесение в базу данных
		/// </summary>
		static TesterOrderUnit ORDER_OPEN(TesterOrderUnit Order)
		{

			//string sql = "INSERT INTO `_order_spot` (" +
			//                "`exchangeId`," +
			//                "`instrumentId`," +
			//                "`type`," +
			//                "`side`," +
			//                "`dtimeExec`," +
			//                "`price`," +
			//                "`qty`," +
			//                "`cost`," +
			//                "`commissionBase`," +
			//                "`commissionQuote`" +
			//             ") VALUES (" +
			//                "1," +
			//               $"{INSTRUMENT.Id}," +
			//               $"'{Order.Type}'," +
			//               $"'{Order.Side}'," +
			//                "CURRENT_TIMESTAMP," +
			//               $"{PRICE}," +
			//               $"{Order.Qty}," +
			//               $"{Order.Cost}," +
			//               $"{(isBUY ? Order.Commission : 0)}," +
			//               $"{(!isBUY ? Order.Commission : 0)}" +
			//             ")";


			Order.Id = ORDERS.Count + 1;

			ORDERS.Add(Order);


			// Обновление балансов
			//if (isBUY)
			//{
			//	INSTRUMENT.BaseBalance += (Order.Qty - Order.Commission);
			//	INSTRUMENT.QuoteBalance -= cost;
			//	INSTRUMENT.BaseCommiss += Order.Commission;
			//}
			//else
			//{
			//	INSTRUMENT.BaseBalance -= Order.Qty;
			//	INSTRUMENT.QuoteBalance += (cost - Order.Commission);
			//	INSTRUMENT.QuoteCommiss += Order.Commission;
			//}

			return Order;
		}






		/// <summary>
		/// Закрытие ордера по текущему курсу
		/// </summary>
		public static bool ORDER_CLOSE(long orderId)
		{
			foreach(dynamic ord in ORDERS)
			{
				if (ord.Id != orderId)
					continue;

				//double profit = Math.Round(ord.Side == "BUY" ? PRICE - ord.PriceOpen : ord.PriceOpen - PRICE, INSTRUMENT.NolCount);
				//string sql = "UPDATE `_order_tester`" +
				//                "SET `dtimeClose`=CURRENT_TIMESTAMP," +
				//                $"`priceClose`={PRICE} ," +
				//                $"`profit`={profit} " +
				//            $"WHERE `id`={orderId}";
				//mysql.Query(sql);

				ORDERS.Remove(ord);

				return true;
			}

			WriteLine("Не удалось закрыть ордер #" + orderId);
			return false;
		}








		/// <summary>
		/// Удаление всех ордеров из базы (при инициализации)
		/// </summary>
		private static void OrderClear()
		{
			if (ORDERS != null && ORDERS.Count > 0)
			{
				//string sql = "DELETE FROM`_order_spot`";
				//mysql.Query(sql);
			}

			ORDERS = new List<object>();
		}
	}



	/// <summary>
	/// Шаблон для ордеров
	/// </summary>
	public class TesterOrderUnit
	{
		public TesterOrderUnit(string type, string side, decimal price, decimal qty)
		{
			IsActive = type == "Market";
			Type = type;
			Side = side;
			Qty  = qty;

			DTimeOpen = IsActive ? Robot.DATE_TIME : "";
			PriceOpen = price;

			BalanceUpd();
		}

		public bool IsActive { get; set; }			// Одрер активен или нет
		public string BaseCoin => Robot.INSTRUMENT.BaseCoin;
		public string QuoteCoin => Robot.INSTRUMENT.QuoteCoin;
		public int Decimals => Robot.INSTRUMENT.Decimals;
		public ulong Exp => format.Exp(Decimals);

		public long Id { get; set; }
		public string Num => $"#{Id}";
		public string Type { get; set; }		// Тип: Market, Limit
		public string TypeStr => IsActive ? "Active" : Type;
		public string Side { get; set; }		// Направление: BUY SELL
		public SolidColorBrush SideColor =>		// Окраска направления ордера
			format.RGB(Side == "BUY" ? "#44AA44" : "#CC4444");
		public string DTimeOpen { get; set; }	// Дата и время открытия
		public decimal PriceOpen { get; set; }	// Цена открытия
		public string PriceOpenStr => format.E(PriceOpen);
		public decimal Qty { get; set; }		// Объём
		public string QtyStr => format.E(Qty);
		public decimal Cost =>					// Стоимость ордера: Price * Qty
			PriceOpen * Qty;
		public string CostStr =>					
			format.Price(Cost, Decimals + format.Decimals(Qty));
		public int PN =>						// Изменение цены в пунктах
			IsActive ? (int)((Robot.PRICE - PriceOpen) * Exp) : 0;
		public SolidColorBrush PNcolor =>		// Окраска пунктов
			format.RGB(PN >= 0 ? "#22AA22" : "#CC2222");
		public decimal Profit =>
			PN / (decimal)Exp * Qty;
		public string ProfitStr =>
			Profit.ToString();

		// Обновление баланса котировочной монеты
		public void BalanceUpd()
		{
			if (!IsActive)
				return;
			
			Robot.INSTRUMENT.QuoteBalance -= Cost;
			Robot.INSTRUMENT.BaseBalance  += Qty;
		}
	}
}
