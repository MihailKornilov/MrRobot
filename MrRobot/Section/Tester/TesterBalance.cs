using System;
using System.Windows.Media;
using System.Windows.Controls;

using static RobotAPI.Robot;
using RobotLib;

namespace MrRobot.Section
{
	public partial class Tester : UserControl
	{
		void BalanceUpdate()
		{
			int baseDec = format.Decimals(INSTRUMENT.BasePrecision);    // Точность базовой монеты
			int quoteDec = baseDec + INSTRUMENT.Decimals;

			Balance.QuoteSum = format.Price(INSTRUMENT.QuoteBalance, quoteDec);
			Balance.BaseSum = format.Price(INSTRUMENT.BaseBalance, baseDec);

			int X = (int)Math.Round(INSTRUMENT.BaseBalance / (decimal)INSTRUMENT.MinOrderQty);
			Balance.BaseX = $"{INSTRUMENT.MinOrderQty}*{X}";
			Balance.QuoteCost = $"≈{format.Price(INSTRUMENT.BaseBalance * PRICE, quoteDec)}";

			Balance.Itog = INSTRUMENT.QuoteBalance + INSTRUMENT.BaseBalance * PRICE - Balance.QuoteStart;

			BalancePanel.DataContext = new Balance();
		}
		public class Balance
		{
			public static decimal BaseStart { get; set; } = 0;
			public static decimal QuoteStart { get; set; } = 100;
			public static string BaseCoin { get; set; }     // Название базовой монеты
			public static string QuoteCoin { get; set; }    // Название котировочной монеты
			public static string BaseSum { get; set; }
			public static string QuoteSum { get; set; }
			public static string BaseX { get; set; }        // Количество минимальных объёмов базовой монеты
			public static string QuoteCost { get; set; }	// Стоимость объёма в котировочной монете
			public static decimal Itog { get; set; }        // Промежуточный итог открытых ордеров в котировочной монете
			public static string ItogStr =>
				$"{(Itog > 0 ? "+" : "")}{Itog}";
			public static SolidColorBrush ItogClr =>
				format.RGB(Itog >= 0 ? "#20B26C" : "#EF454A");

		}
	}
}
