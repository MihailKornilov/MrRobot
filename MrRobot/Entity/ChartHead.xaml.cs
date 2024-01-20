using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace MrRobot.Entity
{
    public partial class ChartHead : UserControl
    {
        public ChartHead()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обновление всех полей заголовка
        /// </summary>
        public void Update(CDIunit item)
        {
            HeadSymbol.Text = item.Name;
            HeadTimeFrame.Text = item.TF;
            HeadPeriod.Text = item.DatePeriod;
            HeadCandleCount.Text = Candle.CountTxt(item.RowsCount);
        }

        public void Symbol(string txt = "")
        {
            HeadSymbol.Text = txt;
        }
        public void Period(string txt = "")
        {
            HeadPeriod.Text = txt;
        }
        public void CandleCount(int count = 0)
        {
            HeadCandleCount.Text = Candle.CountTxt(count);
        }

        /// <summary>
        /// Переход на страницу биржи выбранного инструмента
        /// </summary>
        private void SiteGo(object sender, MouseButtonEventArgs e)
        {
            var block = sender as TextBlock;
            if (block == null)
                return;

            Process.Start("https://www.bybit.com/ru-RU/trade/spot/" + block.Text);
        }
    }
}
