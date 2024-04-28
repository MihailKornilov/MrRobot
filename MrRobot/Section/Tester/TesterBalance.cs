using System;
using System.Windows.Controls;

using static RobotAPI.Robot;
using RobotLib;

namespace MrRobot.Section
{
	public partial class Tester : UserControl
	{
		const decimal BaseBalance = 0;
		const decimal QuoteBalance = 100;

		void BalanceUpdate()
		{
			int baseDec = format.Decimals(INSTRUMENT.BasePrecision);    // Точность базовой монеты
			int quoteDec = baseDec + INSTRUMENT.Decimals;

			QuoteBalanceSum.Content = format.Price(INSTRUMENT.QuoteBalance, quoteDec);

			BaseBalanceSum.Content = format.Price(INSTRUMENT.BaseBalance, baseDec);
			int X = (int)Math.Round(INSTRUMENT.BaseBalance / (decimal)INSTRUMENT.MinOrderQty);
			BaseBalanceX.Content = $"{INSTRUMENT.MinOrderQty}*{X}";
			BaseBalanceQuote.Content = $"≈{format.Price(INSTRUMENT.BaseBalance * PRICE, quoteDec)}";

			decimal itog = INSTRUMENT.QuoteBalance + INSTRUMENT.BaseBalance * PRICE - QuoteBalance;
			QuoteBalanceItog.Content = $"{(itog > 0 ? "+" : "")}{itog}";
		}
	}
}
