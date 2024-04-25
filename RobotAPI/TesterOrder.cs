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
			var Order = new TesterOrderUnit() {
				Type = "Market",
				Side = "BUY",
				Qty = qty
			};

			return ORDER_OPEN(Order);
		}


		/// <summary>
		/// Создание нового ордера на продажу по текущему курсу
		/// </summary>
		public static TesterOrderUnit MARKET_SELL(decimal qty)
		{
			var Order = new TesterOrderUnit() {
				Type = "Market",
				Side = "SELL",
				Qty = qty
			};

			return ORDER_OPEN(Order);
		}



		/// <summary>
		/// Создание нового ордера и внесение в базу данных
		/// </summary>
		static TesterOrderUnit ORDER_OPEN(TesterOrderUnit Order)
		{
			//Order.BaseCoin = INSTRUMENT.BaseCoin;
			//Order.QuoteCoin = INSTRUMENT.QuoteCoin;

			//Order.DtimeOpen = DATE_TIME;
			//Order.PriceOpen = PRICE;
			//Order.PriceOpenStr = format.E(PRICE);

			//Order.QtyStr = format.E(Order.Qty);

			//double cost = PRICE * Order.Qty;
			//Order.Cost = cost;
			//Order.CostStr = format.E(cost);

			//bool isBUY = Order.Side == "BUY";
			//Order.SideColor = isBUY ? "#4A4" : "#C44";

			//Order.Commission = isBUY ? Order.Qty / 1000 : cost / 1000;
			//Order.CommissionStr = format.E(Order.Commission);
			//Order.CommissCoin = isBUY ? Order.BaseCoin : Order.QuoteCoin;

			//Line(Order.Side.ToLower(), PRICE);


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

				double profit = Math.Round(ord.Side == "BUY" ? PRICE - ord.PriceOpen : ord.PriceOpen - PRICE, INSTRUMENT.NolCount);
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
		public long Id { get; set; }
		public string Num => $"#{Id}";
		public string Type { get; set; }            // Тип: Market, Limit
		public string Side { get; set; }            // Направление: BUY SELL
		public SolidColorBrush SideColor =>			// Окраска направления ордера
			format.RGB(Side == "BUY" ? "#44AA44" : "#CC4444");
		public decimal PriceOpen { get; set; }		// Цена открытия
		public string PriceOpenStr { get; set; }
		public string DtimeOpen { get; set; }       // Дата и время открытия
		public decimal Qty { get; set; }			// Объём
		public string QtyStr { get; set; }
		public string CostStr =>					// Стоимость ордера: Price * Qty
			format.Price(PriceOpen * Qty, 5);
		public int PN { get; set; }                 // Изменение цены в пунктах
		public string PNcolor { get; set; }         // Окраска пунктов
		public double Profit { get; set; }
		public string ProfitStr { get; set; }
		public string ProfitColor { get; set; }
	}
}
