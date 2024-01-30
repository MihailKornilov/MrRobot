using System;
using System.Reflection;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Console;

using static RobotAPI.Robot;
using MrRobot.Entity;
using MrRobot.inc;
using System.Threading;

namespace MrRobot.Section
{
    public partial class Trade : UserControl
    {
        public bool IsTradeInited { get; private set; }
        object ObjInstance { get; set; }
        MethodInfo Init;
        MethodInfo Step;
        MethodInfo Finish;
        bool RobotApply()
        {
            // Остановка робота
            if (IsTradeInited)
                return IsTradeInited = false;

            // Робот не выбран
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
        /// Установка инструмента, если выбран
        /// </summary>
        void InstrumentSet()
        {
            INSTRUMENT = InstrumentListBox.SelectedItem as InstrumentUnit;
        }

        void GlobalInit()
        {
            InstrumentSelectBlock.IsEnabled = IsTradeInited;
            InstrumentCancelLabel.IsEnabled = IsTradeInited;
            RobotsListBox.IsEnabled = IsTradeInited;
            RobotButton.Content = IsTradeInited ? "Старт" : "Стоп";

            if (!RobotApply())
                return;

            InstrumentSet();
            new CANDLE_NEW(format.TFass());
            new PATTERN(Patterns.ListAll(), true);
            new TradeChartTimer();
            TradeChartTimer.OutMethod += CandlesActualUpdate;

            TRADE_GLOBAL_INIT();
            LOGG.Method = Log;
            LOGG.Stat = delegate(string txt) { StaticLogBox.Text = txt; };
            Init.Invoke(ObjInstance, new object[] { new string[] { } });
            LOGG.Output();

            IsTradeInited = true;
        }

        /// <summary>
        /// Обновление первой свечи
        /// </summary>
        public void Candles_0_upd(CandleUnit cndl)
        {
            if (!IsTradeInited)
                return;

            //if (cndl.Unix == CANDLES[0].Unix)
            //    CANDLES[0] = cndl;
            //else
            //    CANDLES.Insert(0, cndl);

            TradeRobotStep();
        }

        /// <summary>
        /// Обновление списка сделок
        /// </summary>
        public void TradeListAdd(DepthUnit unit)
        {
            if (!IsTradeInited)
                return;

            TRADE_LIST.Add(unit);
        }

        /// <summary>
        /// Подгрузка заказанных свечей для Robot:Trade:CANDLE_LIST
        /// </summary>
        public async void CandlesActualUpdate(string txt)
        {
            EChart.Right(txt);

            if (!IsTradeInited)
                return;
            if (DateTime.Now.Second > 0)
                return;

            await Task.Run(() =>
            {
                CANDLE_NEW.Update();

                var SymbolList = CANDLES_ACTUAL.SymbolList();
                if (SymbolList.Count == 0)
                    return;

                foreach (string symbol in SymbolList.Keys)
                {
                    int start = SymbolList[symbol];
                    var list = Candle.WCkline(symbol, start);
                    CANDLES_ACTUAL.Save(symbol, list);
                }
            });
            TradeRobotStep();
        }

        /// <summary>
        /// Очередной шаг: добавление новой свечи, либо появилась новая сделка
        /// </summary>
        void TradeRobotStep()
        {
            TRADE_GLOBAL_STEP();

            // Выполнение очередного шага в Роботе
            Step.Invoke(ObjInstance, new object[] { });
            LOGG.Output();
        }

        /// <summary>
        /// Вывод информации в лог Тестера
        /// </summary>
        void Log(List<RobotAPI.LogUnit> list)
        {
            foreach (var row in list)
                LogList.Items.Add(row);

            while (LogList.Items.Count > 1000)
                LogList.Items.RemoveAt(0);

            int c = LogList.Items.Count - 1;
            LogList.ScrollIntoView(LogList.Items[c]);
        }
    }


    /// <summary>
    /// Класс, выводящий время в верхнем правом углу графика
    /// </summary>
    public class TradeChartTimer
    {
        static bool IsWorked { get; set; }

        public delegate void Dcall(string txt);
        public static Dcall OutMethod;

        public TradeChartTimer()
        {
            if (IsWorked)
                return;

            IsWorked = true;
            Start();
        }

        /// <summary>
        /// Запуск вывода времени
        /// </summary>
        static async void Start()
        {
            var prgs = new Progress<string>((v) =>
            {
                OutMethod?.Invoke(v);
            });
            await Task.Run(() => Process(prgs));
        }
        static void Process(IProgress<string> prgs)
        {
            int MinuteLast = -1;
            int SecondLast = -1;
            while (IsWorked)
            {
                Thread.Sleep(100);

                var now = DateTime.Now;
                if (SecondLast != now.Second)
                {
                    SecondLast = now.Second;
                    // Печать текущего времени в верхнем правом углу графика раз в секунду
                    prgs.Report(format.TimeNow());
                }

                if (now.Second > 0)
                    continue;
                if (format.MilliSec() < 100)
                    continue;
                if (MinuteLast == now.Minute)
                    continue;

                MinuteLast = now.Minute;

                //CandleFirst.Update(format.UnixNow());
                //global.MW.Trade.Candles_0_upd(CandleFirst);
                //ChartUpdate();
            }
        }
    }
}
