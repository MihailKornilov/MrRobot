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
        private static AutoProgonParam PARAM { get; set; }

        public static void Go(AutoProgonParam param)
        {
            //global.MW.Setting.AutoProgonButton.IsEnabled = false;

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

        private static void ButtonClick(Button but)
        {
            var args = new RoutedEventArgs(Button.ClickEvent);
            but.RaiseEvent(args);
        }

        /// <summary>
        /// Смена на очередную валютную пару
        /// </summary>
        private static void SymbolChange()
        {
            if (PARAM.SymbolIndex >= PARAM.SymbolMass.Length)
            {
                Active = false;
                FoundCandle = null;
                //global.MW.Setting.AutoProgonButton.IsEnabled = true;
                WriteLine("------------------- AutoProgon FINISHED: " + PARAM.dur.Second());
                return;
            }

            int i = PARAM.SymbolIndex;
            PARAM.Symbol = PARAM.SymbolMass[i];
            PARAM.SymbolIndex++;

            HistoryDownload();
        }

        /// <summary>
        /// Скачивание исторических данных
        /// </summary>
        private static void HistoryDownload()
        {
            // Переход на страницу 1:"Скачивание исторических данных"
            //ButtonClick(global.MW.MainMenuButton_1);

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
            //ButtonClick(global.MW.MainMenuButton_2);

            // Выбор скачанной истории TF=1
            global.MW.Converter.SourceListBox.SelectedItem = Candle.InfoUnit(cdiId);

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
                         $" AND`scatterPercent`={PARAM.ScatterPercent}" +
                         $" AND`foundRepeatMin`<={PARAM.FoundRepeatMin} " +
                          " AND`candleNolAvoid`";
            string[] pfIds = mysql.Ids(sql).Split(',');


            // Выбор IDs, по которым не было поиска паттернов
            var idsNoSearch = new List<string>();
            foreach(string id in cdiIds.Split(','))
                if(!pfIds.Contains(id))
                    idsNoSearch.Add(id);

            // Если поиск был по всем таймфреймам, переход на Тестер
            if(idsNoSearch.Count == 0)
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
            //ButtonClick(global.MW.MainMenuButton_3);

            // Установка настроек
            global.MW.Pattern.PatternCandlesCount.Text = PARAM.PatternLength;
            global.MW.Pattern.PatternScatterPrc.Text = PARAM.ScatterPercent;
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
            global.MW.Pattern.SourceListBox.SelectedItem = Candle.InfoUnit(id);

            // Нажатие на кнопку "Новый поиск"
            ButtonClick(global.MW.Pattern.SearchNewButton);

            // Нажатие на кнопку "Запуск поиска паттернов"
            ButtonClick(global.MW.Pattern.SearchGoButton);

            return true;
        }


        public static string FoundCandle = "";   // Текущие найденные свечи (паттерн) для передачи роботу
        /// <summary>
        /// Тестирование паттерна роботом
        /// </summary>
        static void RobotSetup()
        {
            // Переход на страницу 4:"Tester"
            //ButtonClick(global.MW.MainMenuButton_4);

            // Идентификаторы свечных данных текущего Symbol
            string cdiIds = Candle.IdsOnSymbol(PARAM.Symbol, PARAM.ConvertTF);

            // Идентификаторы, которые не проходили тест
            string sql = "SELECT" +
                            "`id`,`cdiId`" +
                         "FROM`_pattern_search`" +
                        $"WHERE`cdiId`IN({cdiIds})" +
                           "AND`foundCount`" +
                           "AND`foundCount`>`testedCount`";
            PARAM.FoundNoTested = mysql.StringAss(sql);

            if (PARAM.FoundNoTested.Count == 0)
            {
                SymbolChange();
                return;
            }

            string keys = string.Join(",", PARAM.FoundNoTested.Keys.ToArray());
            sql = "SELECT" +
                     "`id`,`foundCount`" +
                  "FROM`_pattern_search`" +
                 $"WHERE`id`IN({keys})";
            PARAM.FoundCountAss = mysql.StringAss(sql);
            RobotTest();
        }

        static void RobotTest()
        {
            if (PARAM.FoundNoTested.Count == 0)
            {
                SymbolChange();
                return;
            }

            KeyValuePair<string, string> pair = PARAM.FoundNoTested.First();

            if (PARAM.FoundId != pair.Key)
            {
                PARAM.FoundId = pair.Key;
                PARAM.FoundCount = PARAM.FoundCountAss[pair.Key];

                // Установка свечных данных в Тестере
                var unit = Candle.InfoUnit(pair.Value);
                global.MW.Tester.InstrumentListBox.SelectedItem = unit;

                PARAM.TF = unit.TF;
            }

            // Загрузка списка паттернов, которые не прошли тест
            string sql = "SELECT" +
                            "`id`,`candle`" +
                         "FROM`_pattern_found`" +
                        $"WHERE`searchId`={PARAM.FoundId} " +
                            "AND`minutesList`IS NULL " +
                         "ORDER BY`id`" +
                         "LIMIT 1";
            var pfsItem = mysql.QueryOne(sql);
            if (pfsItem.Count == 0)
            {
                PARAM.FoundNoTested.Remove(PARAM.FoundId);
                RobotTest();
                return;
            }

            global.MW.Tester.RobotsListBox.SelectedIndex = -1;
            PARAM.FoundSpisokId = pfsItem["id"];
            FoundCandle = pfsItem["candle"];
            global.MW.Tester.RobotsListBox.SelectedIndex = 1;

            // Нажатие на кнопку "Запуск тестера без визуализации"
            ButtonClick(global.MW.Tester.NoVisualButton);
        }
        /// <summary>
        /// Сохранение результатов тестера
        /// </summary>
        public static void RobotResult(string res)
        {
            if (!Active)
                return;

            string[] spl = res.Split(';');
            int profit = Convert.ToInt32(spl[0]);
            int loss = Convert.ToInt32(spl[1]);
            string sql = "UPDATE`_pattern_found`" +
                        $"SET`profitCount`={profit}," +
                           $"`lossCount`={loss}," +
                           $"`minutesList`='{spl[2]}'" +
                        $"WHERE`id`={PARAM.FoundSpisokId}";
            mysql.Query(sql);

            // Обновление количества паттернов, которые прошли тест
            sql = "SELECT COUNT(*)" +
                  "FROM`_pattern_found`" +
                 $"WHERE`searchId`={PARAM.FoundId} " +
                    "AND`minutesList`IS NOT NULL";
            int TestedCount = mysql.Count(sql);

            sql = "UPDATE`_pattern_search`" +
                 $"SET`testedCount`={TestedCount} " +
                 $"WHERE`id`={PARAM.FoundId}";
            mysql.Query(sql);

            WriteLine(PARAM.Symbol + " " + PARAM.TF + ": " +
                      "Тест " + TestedCount + " из " + PARAM.FoundCount + ": " +
                      (profit + loss) + " (" + profit + "/" + loss + ")");
            WriteLine();

            RobotTest();
        }
    }

    public class AutoProgonParam
    {
        public Dur dur { get; set; } = new Dur();
        public string Symbol { get; set; }
        public string[] SymbolMass { get; set; }
        public int SymbolIndex { get; set; }
        public string ConvertTF { get; set; }       // Список таймфреймов черех запятую
        public string PatternLength { get; set; }   // Длина паттерна
        public string ScatterPercent { get; set; }  // Разброс в процентах
        public string FoundRepeatMin { get; set; }  // Исключать менее N нахождений
        public int Index { get; set; }              // Индекс для Конвертера, Тестера
        public int[] ConvertedIds { get; set; }     // ID сконвертированных свечных данных
        public string TF { get; set; }
        public string FoundId { get; set; }         // ID из `_pattern_search`
        public string FoundCount { get; set; }
        public Dictionary<string, string> FoundCountAss { get; set; }
        public string FoundSpisokId { get; set; }   // ID из `_pattern_found`
        public Dictionary<string, string> FoundNoTested { get; set; }
    }
}
