﻿using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using RobotLib;
using MrRobot.inc;
using MrRobot.Connector;
using MrRobot.Interface;
using MrRobot.Entity;

namespace MrRobot.Section
{
	public partial class HistoryMoex : UserControl
	{
		public HistoryMoex()
		{
			G.HistoryMoex = this;

			new MOEX();
			Init();
		}

		public void Init()
		{
			DataContext = new MoexDC();
			G.Exchange.Updated += () => DataContext = new MoexDC();
			
			InitializeComponent();

			// Установка фокуса на Быстрый поиск, если был переход на страницу МосБиржи
			History.MenuMethod += id => {
				if (id == 2)
					FastBox.Focus();
			};


			// Фильтр "Быстрый поиск"
			FastBox.Text = SecurityFilter.FastTxt;
			FastBox.TextChanged += (s, e) =>
			{
				SecurityFilter.FastTxt = FastBox.Text;
				DataContext = new MoexDC();
				GroupBox.SelectedIndex = 0;
			};
			FastCancel.MouseLeftButtonDown += (s, e) =>
			{
				FastBox.Text = "";
				FastBox.Focus();
			};

			// Фильтр "Группа"
			GroupBox.SelectedIndex = MOEX.SGroup.FilterIndex();
			GroupBox.SelectionChanged += (s, e) =>
			{
				if (GroupBox.SelectedIndex == -1)
					return;
				SecurityFilter.GroupId = (GroupBox.SelectedItem as SpisokUnit).Id;
				DataContext = new MoexDC();
			};

			// Выбрана Бумага
			SecurityBox.SelectionChanged += (s, e) =>
			{
				if (SecurityBox.SelectedIndex == -1)
					return;

				var unit = SecurityBox.SelectedItem as SpisokUnit;
				var arr = MOEX.SecurityInfoBoards(unit.Symbol);

				SecurityInfoBox.ItemsSource = arr[0];
				var first = SecurityInfoBox.Items[0];
				SecurityInfoBox.ScrollIntoView(first);

				BoardsBox.ItemsSource = arr[1];
				first = BoardsBox.Items[0];
				BoardsBox.ScrollIntoView(first);

				G.Vis(InfoPanel);
			};

			// Выбран Режим торгов
			BoardsBox.SelectionChanged += (s, e) =>
			{
				if (BoardsBox.SelectedIndex == -1)
					return;

				var board = BoardsBox.SelectedItem as BoardUnit;
				var src = MOEX.BoardLoad(board);

				G.Vis(LoadPanel, src.Count > 0);
				G.Vis(LoadNoPanel, src.Count == 0);

				if(src.Count == 0)
					return;

				LoadInterval.ItemsSource = src;
				LoadInterval.SelectedIndex = 0;
			};

			// Выбран таймфрейм
			LoadInterval.SelectionChanged += (s, e) =>
			{
				if (LoadInterval.SelectedIndex == -1)
					return;

				var unit = LoadInterval.SelectedItem as BorderUnit;

				LoadBegin.SelectedDate = unit.Begin;
				LoadBegin.DisplayDateStart = unit.Begin;
				LoadBegin.DisplayDateEnd = unit.End;

				LoadEnd.SelectedDate = unit.End;
				LoadEnd.DisplayDateStart = unit.Begin;
				LoadEnd.DisplayDateEnd = unit.End;
			};

			// Кнопка запуска загрузки свечных данных
			LoadGoButton.Click += (s, e) =>
			{
				var board = BoardsBox.SelectedItem as BoardUnit;
				var unit = LoadInterval.SelectedItem as BorderUnit;

				var begin = LoadBegin.SelectedDate;
				var end = LoadEnd.SelectedDate;

				if (begin > end)
					return;

				MOEX.CandlesLoad(board,
								 unit.Interval,
								 begin.Value.ToString("yyyy-MM-dd"),
								 end.Value.ToString("yyyy-MM-dd 23:59:59"));
				new Candle();
			};
		}

		void IssUpdate(object sender, RoutedEventArgs e)
		{
			//MOEX.Engine.iss();
			//MOEX.Market.iss();
			//MOEX.Board.iss();
			//MOEX.BoardGroup.iss();
			//MOEX.SecGroup.iss();
			//MOEX.SecType.iss();
			//MOEX.Security.iss();
		}
	}

	/// <summary>
	/// Moex DataContext
	/// </summary>
	public class MoexDC
	{
		public MoexDC()
		{
			MOEX.SGroup.CountFilter();
			FoundCount = MOEX.Instrument.FoundCount();
		}
		// Название Московской биржи из базы в заголовке
		public static string HdName { get => G.Exchange.Unit(2).Name; }
		// Количество бумаг в заголовке
		public static string HdSecurityCount { get => MOEX.Instrument.CountStr(); }


		// Видимость крестика отмены быстрого поиска
		public static Visibility FastCancelVis  { get => G.Vis(SecurityFilter.FastTxt.Length > 0); }


		public static List<SpisokUnit> GroupList { get => MOEX.SGroup.ListActual(); }
		public static List<SpisokUnit> TypeList { get => MOEX.SType.AllWithNull("не выбран"); }


		// Количество найденных бумаг
		public static int FoundCount { get; set; }
		// Текст с количеством найденных бумаг
		public static string FoundCountStr
		{
			get => FoundCount == 0
					?  "Бумаг не найдено."
					: $"Показан{format.End(FoundCount, "а", "о")} {MOEX.Instrument.CountStr(FoundCount)}";
		}


		// Видимость списка бумаг
		public static Visibility SecurityListVis { get => G.Vis(FoundCount > 0); }
		// Список бумаг
		public static List<SpisokUnit> SecurityList { get => MOEX.Instrument.ListFilter(); }
	}

	/// <summary>
	/// Фильтр для вывода списка бумаг
	/// </summary>
	public class SecurityFilter
	{
		// Фильтрация единицы бумаги для вывода списка
		public static bool IsAllow(SpisokUnit unit)
		{
			//if (MarketId > 0 && unit.MarketId != MarketId)
			//    return false;
			//if (EngineId > 0 && unit.EngineId != EngineId)
			//    return false;
			if (GroupId > 0 && unit.GroupId != GroupId)
				return false;
			if (!IsAllowFast(unit))
				return false;
			return true;
		}
		// Обработка текста Быстрого поиска
		public static bool IsAllowFast(SpisokUnit unit)
		{
			if (FastTxt.Length == 0)
				return true;
			if (unit.Symbol.ToLower().Contains(FastTxt))
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
				GroupId = 0;
				EngineId = 0;
			}
		}
		// Группа
		public static int GroupId
		{
			get => position.Val($"1.2.SecurityFilter.GroupId", 0);
			set
			{
				position.Set($"1.2.SecurityFilter.GroupId", value);
				TypeId = 0;
			}
		}
		// Вид бумаги
		public static int TypeId
		{
			get => position.Val($"1.2.SecurityFilter.TypeId", 0);
			set => position.Set($"1.2.SecurityFilter.TypeId", value);
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
