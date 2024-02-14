using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Connector;

namespace MrRobot.Section
{
    public partial class HistoryMoex : UserControl
    {
        public HistoryMoex()
        {
            InitializeComponent();

            DataContext = new MoexDC();
            Market.Updated += () => DataContext = new MoexDC();

            // Установка фокуса на Быстрый поиск, если был переход на страницу МосБиржи
            History.MenuMethod += (id) => {
                if (id == 2)
                    FastBox.Focus();
            };


            // Фильтр "Быстрый поиск"
            FastBox.Text = SecurityFilter.FastTxt;
            FastBox.TextChanged += (s, e) =>
            {
                SecurityFilter.FastTxt = FastBox.Text;
                EngineBox.SelectedIndex = -1;
                DataContext = new MoexDC();
            };
            FastCancel.MouseLeftButtonDown += (s, e) =>
            {
                FastBox.Text = "";
                FastBox.Focus();
            };


            // Фильтр "Торговая система"
            EngineBox.SelectedIndex = MOEX.Engine.FilterIndex();
            EngineBox.SelectionChanged += (s, e) =>
            {
                if (EngineBox.SelectedIndex == -1)
                    return;
                SecurityFilter.EngineId = (EngineBox.SelectedItem as MoexUnit).Id;
                DataContext = new MoexDC();
            };


            // Фильтр "Рынки"
            MarketBox.SelectedIndex = MOEX.Market.FilterIndex();
            MarketBox.SelectionChanged += (s, e) =>
            {
                if (MarketBox.SelectedIndex == -1)
                    return;
                SecurityFilter.MarketId = (MarketBox.SelectedItem as MoexUnit).Id;
                DataContext = new MoexDC();
            };
        }

        void EngineIss(object sender, RoutedEventArgs e)
        {
            //MOEX.Engine.iss();
            //MOEX.Market.iss();
            //MOEX.Board.iss();
            //MOEX.BoardGroup.iss();
            //MOEX.SecurityGroup.iss();
            //MOEX.SecurityType.iss();
            //MOEX.SecurityСollections.iss();
            MOEX.Security.iss();
        }
    }

    /// <summary>
    /// Moex DataContext
    /// </summary>
    public class MoexDC
    {
        public MoexDC()
        {
            MOEX.Engine.CountFilter();
            MOEX.Market.CountFilter();
            FoundCount = MOEX.Security.FoundCount();
        }
        // Название Московской биржи из базы в заголовке
        public static string HdName { get => Market.Unit(2).Name; }
        // Количество бумаг в заголовке
        public static string HdSecurityCount { get => MOEX.Security.CountStr(); }


        // Видимость крестика отмены быстрого поиска
        public static Visibility FastCancelVis  { get => G.Vis(SecurityFilter.FastTxt.Length > 0); }
        public static List<MoexUnit> EngineList { get => MOEX.Engine.ListActual(); }


        // Видимость списка рынков
        public static Visibility MarketVis { get => G.Vis(SecurityFilter.EngineId > 0); }
        public static List<MoexUnit> MarketList { get => MOEX.Market.ListEngine(); }


        // Количество найденных бумаг
        public static int FoundCount { get; set; }
        // Текст с количеством найденных бумаг
        public static string FoundCountStr
        {
            get
            {
                if (FoundCount == 0)
                    return "Бумаг не найдено.";
                return $"Показан{format.End(FoundCount, "а", "о")} {MOEX.Security.CountStr(FoundCount)}";
            }
        }


        // Видимость списка бумаг
        public static Visibility SecurityListVis { get => G.Vis(FoundCount > 0); }
        // Список бумаг
        public static List<SecurityUnit> SecurityList { get => MOEX.Security.ListFilter(); }
    }

    /// <summary>
    /// Фильтр для вывода списка бумаг
    /// </summary>
    public class SecurityFilter
    {
        // Фильтрация единицы бумаги для вывода списка
        public static bool IsAllow(SecurityUnit unit)
        {
            if (MarketId > 0 && unit.MarketId != MarketId)
                return false;
            if (EngineId > 0 && unit.EngineId != EngineId)
                return false;
            if (!IsAllowFast(unit))
                return false;
            return true;
        }
        // Обработка текста Быстрого поиска
        public static bool IsAllowFast(SecurityUnit unit)
        {
            if (FastTxt.Length == 0)
                return true;
            //if (unit.Id.ToString() == FastTxt)
            //    return true;
            if (unit.SecId.ToLower().Contains(FastTxt))
                return true;
            if (unit.Name.ToLower().Contains(FastTxt))
                return true;
            if (unit.ShortName.ToLower().Contains(FastTxt))
                return true;
            return false;
        }
        // Быстрый поиск
        public static string FastTxt
        {
            get => position.Val($"1.2.SecurityFilter.FastTxt");
            set
            {
                position.Set($"1.2.SecurityFilter.FastTxt", value.ToLower());
                EngineId = 0;
            }
        }
        // Торговая система
        public static int EngineId
        {
            get => position.Val($"1.2.SecurityFilter.EngineId", 0);
            set
            {
                position.Set($"1.2.SecurityFilter.EngineId", value);
                MarketId = 0;
            }
        }
        // Рынок
        public static int MarketId
        {
            get => position.Val($"1.2.SecurityFilter.MarketId", 0);
            set => position.Set($"1.2.SecurityFilter.MarketId", value);
        }
    }
}
