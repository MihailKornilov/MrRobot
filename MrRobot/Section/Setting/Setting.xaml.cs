using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для Setting.xaml
    /// </summary>
    public partial class Setting : UserControl
    {
        public Setting()
        {
            InitializeComponent();
        }

/*
ВСЕ ИНСТРУМЕНТЫ НАЧАЛО ИСТОРИИ МЕНЕЕ 2022 года:

select CONCAT('"',`baseCoin`,'/',quoteCoin,'",') from _instrument where id in (
select id from _instrument where quoteCoin='usdt' and historyBegin<'2022-01-01 00:00:00' order by historyBegin
) order by symbol;
*/

        private void AutoProgonGo(object sender, RoutedEventArgs e)
        {
            if (global.IsAutoProgon)
                return;

            var param = new AutoProgonParam
            {
                //ConvertTF = "3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,25,30",
                ConvertTF = "30,25,20,15,10",
                //ConvertTF = "25,30",
                PatternLength = "3",
                PrecisionPercent = "100",
                //FoundRepeatMin = "4",
                FoundRepeatMin = "6",
                SymbolMass = new string[]
                {
                    //"SOL/USDT",
                    //"ETH/USDT",
                    //"BTC/USDT",
                    //"ALGO/USDT",
                    //"XRP/USDT",
                    //"AVAX/USDT",
                    //"LINK/USDT",
                    //"DOGE/USDT",
                    //"ICP/USDT",
                    //"MATIC/USDT",
                    //"DYDX/USDT",
                    //"DOT/USDT",
                    //"ADA/USDT",

                    "1INCH/USDT",
                    "1SOL/USDT",
                    "AAVE/USDT",
                    "AGLD/USDT",
                    "ANKR/USDT",
                    "ATOM/USDT",
                    "AVA/USDT",
                    "AXS/USDT",
                    "BAT/USDT",
                    "BCH/USDT",
                    "BICO/USDT",
                    "BNT/USDT",
                    "BOBA/USDT",
                    "BTG/USDT",
                    "C98/USDT",
                    "CAKE/USDT",
                    "CBX/USDT",
                    "CEL/USDT",
                    "CHZ/USDT",
                    "COMP/USDT",
                    "CRV/USDT",
                    "CWAR/USDT",
                    "DCR/USDT",
                    "DEVT/USDT",
                    "DGB/USDT",
                    "EGLD/USDT",
                    "ENJ/USDT",
                    "ENS/USDT",
                    "EOS/USDT",
                    "FIL/USDT",
                    "FTM/USDT",
                    "FTT/USDT",
                    "GALA/USDT",
                    "GALFT/USDT",
                    "GENE/USDT",
                    "GM/USDT",
                    "GODS/USDT",
                    "GRT/USDT",
                    "HBAR/USDT",
                    "HOT/USDT",
                    "IMX/USDT",
                    "IZI/USDT",
                    "JASMY/USDT",
                    "KLAY/USDT",
                    "KRL/USDT",
                    "KSM/USDT",
                    "LDO/USDT",
                    "LFW/USDT",
                    "LRC/USDT",
                    "LTC/USDT",
                    "LUNC/USDT",
                    "MANA/USDT",
                    "MKR/USDT",
                    "MX/USDT",
                    "NEXO/USDT",
                    "OMG/USDT",
                    "ONE/USDT",
                    "PERP/USDT",
                    "PLT/USDT",
                    "PSP/USDT",
                    "PTU/USDT",
                    "QNT/USDT",
                    "QTUM/USDT",
                    "REAL/USDT",
                    "REN/USDT",
                    "RNDR/USDT",
                    "RUNE/USDT",
                    "RVN/USDT",
                    "SAND/USDT",
                    "SHIB/USDT",
                    "SHILL/USDT",
                    "SIS/USDT",
                    "SLP/USDT",
                    "SNX/USDT",
                    "SOS/USDT",
                    "SPELL/USDT",
                    "SRM/USDT",
                    "STETH/USDT",
                    "SUSHI/USDT",
                    "TEL/USDT",
                    "THETA/USDT",
                    "TRIBE/USDT",
                    "TRVL/USDT",
                    "UMA/USDT",
                    "UNI/USDT",
                    "USDC/USDT",
                    "WAVES/USDT",
                    "WEMIX/USDT",
                    "WOO/USDT",
                    "XDC/USDT",
                    "XEC/USDT",
                    "XEM/USDT",
                    "XLM/USDT",
                    "XTZ/USDT",
                    "XYM/USDT",
                    "YFI/USDT",
                    "ZEN/USDT",
                    "ZIL/USDT",
                    "ZRX/USDT"
                }
            };

            AutoProgon.Go(param);
        }
    }
}

