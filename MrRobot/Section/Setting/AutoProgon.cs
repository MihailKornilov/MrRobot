﻿using System;
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
            if(Candle.IsTFexist(PARAM.Symbol))
            {
                Converter();
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
        public static void Converter()
        {
            if (!Active)
                return;

            //SymbolChange();
            //return;


            // Переход на страницу 2:"Конвертер"
            SectionGo(2);

            // Выбор скачанной истории TF=1
            var unit = Candle.UnitOnSymbol(PARAM.Symbol);
            CDIpanel.Page(2).CdiId = unit.Id;

            // Выбор таймфреймов
            int[] TimeFrame = Array.ConvertAll(PARAM.ConvertTF.Split(','), x => int.Parse(x));
            bool ConvertGo = false;
            foreach (int tf in TimeFrame)
            {
                // Если свечные данные уже сконвертированы с таким таймфреймом, то сохранение ID
                if(Candle.IsTFexist(PARAM.Symbol, tf))
                    continue;

                (global.MW.Converter.FindName($"CheckTF{tf}") as CheckBox).IsChecked = true;
                ConvertGo = true;
            }

            // Если все таймфреймы были сконвертированы ранее, переход на Поиск паттернов
            if (!ConvertGo)
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
            PARAM.ConvertedIds = Candle.IdsOnSymbol(PARAM.Symbol, PARAM.ConvertTF);

            // Если поиск был по всем таймфреймам, переход на Тестер
            if (RobotSetup(PARAM.ConvertedIds.Length == 0))
                return;

            PARAM.ConvertedIds = PARAM.ConvertedIds.Reverse().ToArray();

            // Переход на страницу 3:"Поиск паттернов"
            SectionGo(3);

            // Установка настроек
            global.MW.Pattern.LengthSlider.Value = Convert.ToInt32(PARAM.PatternLength);
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

            if (RobotSetup(PARAM.Index >= PARAM.ConvertedIds.Length))
                return true;

            // Выбор свечных данных
            int index = PARAM.Index;
            int CdiId = PARAM.ConvertedIds[index];
            CDIpanel.Page(3).CdiId = CdiId;

            if (!PatternSearchAgain(CdiId))
                PARAM.Index++;

            // Нажатие на кнопку "Запуск поиска паттернов"
            ButtonClick(global.MW.Pattern.SearchGoButton);

            return true;
        }
        /// <summary>
        /// Новый поиск по тем же свечным данным, но процент ниже
        /// </summary>
        public static bool PatternSearchAgain(int CdiId)
        {
            int PatternLength = Convert.ToInt32(PARAM.PatternLength);
            int prc = 100;
            while (true)
            {
                var unit = Patterns.SUnitOnParam(CdiId, PatternLength, prc);
                if (unit == null)
                    break;
                if (unit.FoundCount > 0)
                    return false;
                if ((prc -= 10) < 50)
                    return false;
            }

            global.MW.Pattern.PrecisionPercentSlider.Value = prc;

            return true;
        }



        /// <summary>
        /// Тестирование паттерна роботом
        /// </summary>
        static bool RobotSetup(bool IsRS)
        {
            if (!IsRS)
                return false;

            // Переход на страницу 4:"Tester"
            SectionGo(4);

            // Идентификаторы свечных данных текущего Symbol
            int[] CDIids = Candle.IdsOnSymbol(PARAM.Symbol, PARAM.ConvertTF);

            var ids = new List<int>();
            foreach (int id in CDIids)
                if (Patterns.NoTestedCount(id) > 0)
                    ids.Add(id);

            if (SymbolChange(ids.Count == 0))
                return true;

            PARAM.ConvertedIds = ids.ToArray();
            PARAM.Index = 0;
            PARAM.IdLast = -1;

            RobotTest();

            return true;
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

            if (Patterns.NoTestedCount(id) < 2)
                PARAM.Index++;

            if (PARAM.IdLast != id)
            {
                CDIpanel.Page(4).CdiId = id;
                global.MW.Tester.RobotsListBox.SelectedIndex = 1;
                PARAM.IdLast = id;
                return;
            }

            global.MW.Tester.GlobalInit();
        }

        public static void RobotStart()
        {
            if (!Active)
                return;

            // Нажатие на кнопку "Запуск тестера без визуализации" после загрузки всех свечных данных
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
        public string FoundRepeatMin { get; set; }  // Исключать менее N нахождений
        public int Index { get; set; }              // Индекс для Конвертера, Тестера
        public int[] ConvertedIds { get; set; }     // ID сконвертированных свечных данных
        public int IdLast { get; set; }
    }
}
