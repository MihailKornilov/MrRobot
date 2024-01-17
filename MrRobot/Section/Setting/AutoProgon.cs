using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    public static class AutoProgon
    {
        public static bool Active { get; private set; }
        static AutoProgonParam PARAM { get; set; }

        public static void Go(AutoProgonParam param)
        {
            global.MW.Setting.AutoProgonButton.IsEnabled = false;

            // Установка параметров в History
            global.MW.History.InstrumentFindBox.Text = "";
            global.MW.History.SetupPeriod.SelectedIndex = 7;
            global.MW.History.SetupTimeFrame.SelectedIndex = 0;

            // Установка параметров в Тестере
            if (!(bool)global.MW.Tester.UseTF1Check.IsChecked)
                global.MW.Tester.UseTF1Check.IsChecked = true;
            if ((bool)global.MW.Tester.VisualCheck.IsChecked)
                global.MW.Tester.VisualCheck.IsChecked = false;

            PARAM = param;
            Active = true;
            SymbolChange();
        }

        static void ButtonClick(Button but)
        {
            var args = new RoutedEventArgs(Button.ClickEvent);
            but.RaiseEvent(args);
        }

        /// <summary>
        /// Переход на выбранную страницу
        /// </summary>
        static void SectionGo(int num)
        {
            global.MW.MainMenuListBox.SelectedIndex = num - 1;
        }

        /// <summary>
        /// Смена на очередную валютную пару
        /// </summary>
        static bool SymbolChange(bool IsChange = true)
        {
            if(!IsChange)
                return false;
 
            if (PARAM.SymbolIndex >= PARAM.SymbolMass.Length)
            {
                Active = false;
                global.MW.Setting.AutoProgonButton.IsEnabled = true;
                WriteLine("------------------- AutoProgon FINISHED: " + PARAM.dur.Second());
                return true;
            }


            int i = PARAM.SymbolIndex;
            PARAM.Symbol = PARAM.SymbolMass[i];
            PARAM.SymbolIndex++;

            HistoryDownload();
            return true;
        }

        /// <summary>
        /// Скачивание исторических данных
        /// </summary>
        static void HistoryDownload()
        {
            // Переход на страницу 1:"Скачивание исторических данных"
            SectionGo(1);

            // Если свечные данные были скачаны ранее, переход на Конвертацию
            var unit = Candle.UnitTF(PARAM.Symbol);
            if (unit != null)
            {
                Converter(unit.Id);
                return;
            }

            // Выбор инструмента
            global.MW.History.InstrumentListBox.SelectedItem = Instrument.UnitOnSymbol(PARAM.Symbol);

            // Запуск скачивания - нажатие на кнопку
            ButtonClick(global.MW.History.DownloadGoButton);
        }



        /// <summary>
        /// Конвертация в выбранные таймфреймы
        /// </summary>
        public static void Converter(int cdiId)
        {
            if (!Active)
                return;

            // Переход на страницу 2:"Конвертер"
            SectionGo(2);

            // Выбор скачанной истории TF=1
            global.MW.Converter.SourceListBox.SelectedItem = Candle.Unit(cdiId);

            // Выбор таймфреймов
            string[] TimeFrame = PARAM.ConvertTF.Split(',');
            PARAM.ConvertedIds = new int[TimeFrame.Length];
            PARAM.Index = 0;
            foreach (string tf in TimeFrame)
            {
                // Если свечные данные уже сконвертированы с таким таймфреймом, то сохранение ID
                var unit = Candle.UnitTF(PARAM.Symbol, tf);
                if (unit != null)
                {
                    int index = PARAM.Index;
                    PARAM.ConvertedIds[index] = unit.Id;
                    PARAM.Index++;
                    continue;
                }

                var check = global.MW.Converter.FindName("CheckTF" + tf) as CheckBox;
                check.IsChecked = true;
            }

            // Если все таймфреймы были сконвертированы ранее, переход на Поиск паттернов
            if (PARAM.Index >= PARAM.ConvertedIds.Length)
            {
                PatternSearchSetup();
                return;
            }

            // Запуск конвертации - нажатие на кнопку
            ButtonClick(global.MW.Converter.ConvertGoButton);
        }



        /// <summary>
        /// Установка настроек для поиска паттернов
        /// </summary>
        public static void PatternSearchSetup()
        {
            if (!Active)
                return;

            // Список ID свечных данных, по которым нужно производить поиск паттернов
            string cdiIds = Candle.IdsOnSymbol(PARAM.Symbol, PARAM.ConvertTF);

            // Список ID свечных данных, по которым уже был поиск паттернов
            string sql = "SELECT`cdiId`" +
                         "FROM`_pattern_search`" +
                        $"WHERE`cdiId`IN({cdiIds})" +
                         $" AND`patternLength`={PARAM.PatternLength}" +
                         $" AND`scatterPercent`={PARAM.PrecisionPercent}";
            string[] pfIds = mysql.Ids(sql).Split(',');


            // Выбор IDs, по которым не было поиска паттернов
            var idsNoSearch = new List<string>();
            foreach (string id in cdiIds.Split(','))
                if (!pfIds.Contains(id))
                    idsNoSearch.Add(id);

            // Если поиск был по всем таймфреймам, переход на Тестер
            if (idsNoSearch.Count == 0)
            {
                RobotSetup();
                return;
            }

            PARAM.ConvertedIds = new int[idsNoSearch.Count];
            for (int i = 0; i < idsNoSearch.Count; i++)
            {
                string id = idsNoSearch[i];
                PARAM.ConvertedIds[i] = Convert.ToInt32(id);
            }

            // Переход на страницу 3:"Поиск паттернов"
            SectionGo(3);

            // Установка настроек
            global.MW.Pattern.LengthSlider.Value = Convert.ToInt32(PARAM.PatternLength);
            global.MW.Pattern.PrecisionPercentSlider.Value = Convert.ToInt32(PARAM.PrecisionPercent);
            global.MW.Pattern.FoundRepeatMin.Text = PARAM.FoundRepeatMin;

            PARAM.Index = 0;
            PatternSearch();
        }
        /// <summary>
        /// Процесс поиска паттернов
        /// </summary>
        public static bool PatternSearch()
        {
            if (!Active)
                return false;

            if (PARAM.Index >= PARAM.ConvertedIds.Length)
            {
                RobotSetup();
                return true;
            }

            // Выбор свечных данных
            int index = PARAM.Index;
            int id = PARAM.ConvertedIds[index];
            PARAM.Index++;
            global.MW.Pattern.SourceListBox.SelectedItem = Candle.Unit(id);

            // Нажатие на кнопку "Запуск поиска паттернов"
            ButtonClick(global.MW.Pattern.SearchGoButton);

            return true;
        }




        /// <summary>
        /// Тестирование паттерна роботом
        /// </summary>
        static void RobotSetup()
        {
            // Переход на страницу 4:"Tester"
            SectionGo(4);

            // Идентификаторы свечных данных текущего Symbol
            string cdiIds = Candle.IdsOnSymbol(PARAM.Symbol, PARAM.ConvertTF);

            if(SymbolChange(cdiIds == "0"))
                return;

            PARAM.ConvertedIds = Array.ConvertAll(cdiIds.Split(','), x => int.Parse(x));
            PARAM.Index = 0;

            RobotTest();
        }

        public static void RobotTest()
        {
            if (!Active)
                return;
            if (SymbolChange(PARAM.Index >= PARAM.ConvertedIds.Length))
                return;


            // Установка свечных данных в Тестере
            int index = PARAM.Index;
            int id = PARAM.ConvertedIds[index];

            if(Patterns.NoTestedCount(id) == 1)
                PARAM.Index++;

            global.MW.Tester.InstrumentListBox.SelectedItem = Candle.Unit(id);
            global.MW.Tester.RobotsListBox.SelectedIndex = 1;

            // Нажатие на кнопку "Запуск тестера без визуализации"
            ButtonClick(global.MW.Tester.NoVisualButton);
        }
    }

    public class AutoProgonParam
    {
        public Dur dur { get; set; } = new Dur();
        public string Symbol { get; set; }
        public string[] SymbolMass { get; set; }
        public int SymbolIndex { get; set; }
        public string ConvertTF { get; set; }       // Список таймфреймов через запятую
        public string PatternLength { get; set; }   // Длина паттерна
        public string PrecisionPercent { get; set; }// Точность в процентах
        public string FoundRepeatMin { get; set; }  // Исключать менее N нахождений
        public int Index { get; set; }              // Индекс для Конвертера, Тестера
        public int[] ConvertedIds { get; set; }     // ID сконвертированных свечных данных
    }
}
