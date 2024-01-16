using System;
using System.Reflection;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

using static RobotAPI.Robot;
using MrRobot.Entity;
using MrRobot.inc;

namespace MrRobot.Section
{
    public partial class Trade : UserControl
    {
        public bool IsTradeRobotInited { get; private set; }
        object ObjInstance { get; set; }
        MethodInfo Init;
        MethodInfo Step;
        MethodInfo Finish;
        private bool RobotApply()
        {
            // Не выбран инструмент
            if (position.Val("5.InstrumentListBox.Id", 0) == 0)
                return false;
            // Не выбран робот
            if (RobotsListBox.SelectedIndex <= 0)
                return false;

            var robot = RobotsListBox.SelectedItem as RobotUnit;

            var ASML = Assembly.LoadFrom(robot.Path);
            var type = ASML.GetType(robot.Name);
            ObjInstance = Activator.CreateInstance(type);
            Init = type.GetMethod("Init");
            Step = type.GetMethod("Step");
            Finish = type.GetMethod("Finish");

            return true;
        }


        /// <summary>
        /// Загрузка списка свечей выбранного инструмента
        /// </summary>
        void CandlesSet(List<object> list)
        {
            CANDLES = list;
        }

        void GlobalInit()
        {
            InstrumentSelectBlock.IsEnabled = IsTradeRobotInited;
            InstrumentCancelLabel.IsEnabled = IsTradeRobotInited;
            RobotsListBox.IsEnabled = IsTradeRobotInited;
            RobotButton.Content = IsTradeRobotInited ? "Старт" : "Стоп";

            if (IsTradeRobotInited)
            {
                IsTradeRobotInited = false;
                return;
            }

            if (!RobotApply())
                return;

            INSTRUMENT = InstrumentListBox.SelectedItem as InstrumentUnit;
            new CANDLE_NEW(format.TFass());

            TRADE_GLOBAL_INIT();

            object[] args = {
                new string[] {
                    ""
                }
            };
            Init.Invoke(ObjInstance, args);

            IsTradeRobotInited = true;
        }

        /// <summary>
        /// Обновление первой свечи
        /// </summary>
        public void Candles_0_upd(CandleUnit cndl)
        {
            if (!IsTradeRobotInited)
                return;

            if (cndl.Unix == CANDLES[0].Unix)
                CANDLES[0] = cndl;
            else
                CANDLES.Insert(0, cndl);

            TradeRobotStep();
        }

        /// <summary>
        /// Обновление списка сделок
        /// </summary>
        public void TradeListAdd(DepthUnit unit)
        {
            if (!IsTradeRobotInited)
                return;

            TRADE_LIST.Add(unit);
        }

        /// <summary>
        /// Подгрузка заказанных свечей для Robot:Trade:CANDLE_LIST
        /// </summary>
        public async void CandlesActualUpdate()
        {
            if (!IsTradeRobotInited)
                return;

            await Task.Run(() =>
            {
                CANDLE_NEW.Update();
                CANDLE_NEW.Show();

                var SymbolList = CANDLES_ACTUAL.SymbolList();
                if (SymbolList.Count == 0)
                    return;

                foreach (string symbol in SymbolList.Keys)
                {
                    int start = SymbolList[symbol];
                    var list = Candle.WCkline(symbol, start);
                    CANDLES_ACTUAL.Save(symbol, list);
                }
                TradeRobotStep();
            });
        }

        /// <summary>
        /// Очередной шаг: добавление новой свечи, либо появилась новая сделка
        /// </summary>
        void TradeRobotStep()
        {
            TRADE_GLOBAL_STEP();

            // Выполнение очередного шага в Роботе
            Step.Invoke(ObjInstance, new object[] { });
        }
    }
}
