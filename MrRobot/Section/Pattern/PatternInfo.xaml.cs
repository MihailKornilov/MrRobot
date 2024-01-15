﻿using System;
using System.Windows;

using MrRobot.inc;
using MrRobot.Entity;
using System.Collections.Generic;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для PatternInfo.xaml
    /// </summary>
    public partial class PatternInfo : Window
    {
        public PatternInfo()
        {
            InitializeComponent();
            InfoShow();
        }

        void InfoShow()
        {
            var found = global.MW.Pattern.FoundListBox.SelectedItem as PatternFoundUnit;
            var CDI = Candle.Unit(found.CdiId);

            PatternInfoBox.Text =
                $"{CDI.Name} {CDI.TF} " +
                $"Struct:\n{found.Structure.Replace(';', '\n')}\n" +
                 "\n" +
                $"{FoundList(found)}\n";
        }



        List<PatternUnit> PatternList(PatternFoundUnit found)
        {
            ulong Exp = Candle.Unit(found.CdiId).Exp;
            var CandleList = new List<CandleUnit>();
            var PatternList = new List<PatternUnit>();
            int PatternLen = found.PatternLength; // длина паттерна
            foreach (var cndl in Candle.Data(found.CdiId))
            {
                if (CandleList.Count == 0 && !found.UnixList.Contains(cndl.Unix))
                    continue;

                CandleList.Add(cndl);

                if (CandleList.Count < PatternLen)
                    continue;

                var patt = new PatternUnit();
                patt.Create(CandleList, Exp);
                PatternList.Add(patt);

                CandleList = new List<CandleUnit>();
            }
            return PatternList;
        }
        string FoundList(PatternFoundUnit found)
        {
            var CDI = Candle.Unit(found.CdiId);
            string send = "";
            int step = 1;
            foreach (var patt in PatternList(found))
            {
                var cList = patt.CandleList;
                send += $"{step++}. " +
                        //$"{cList[0].Unix} " +
                        $"{format.DTimeFromUnix(cList[0].Unix)}  " +
                        $"Size: {patt.Size}\n" +
                        $"{CandleList(cList, CDI.NolCount)}" +
                        $"\n";
            }

            return send;
        }

        string CandleList(List<CandleUnit> list, int precission)
        {
            var send = "";
            foreach(var cndl in list)
                send += $"   High: {format.Price(cndl.High, precission)}" +
                        $"   Open: {format.Price(cndl.Open, precission)}" +
                        $"   Close: {format.Price(cndl.Close, precission)}" +
                        $"   Low: {format.Price(cndl.Low, precission)}\n";
            return send;
        }
    }
}
