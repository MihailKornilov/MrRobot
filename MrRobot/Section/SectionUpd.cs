using static System.Console;

using MrRobot.Entity;
using MrRobot.inc;

namespace MrRobot.Section
{
    public static class SectionUpd
    {
        public static bool[] Update = new bool[6];

        private static bool isUpdateLater(int page)
        {
            Update[page] = position.MainMenu() != page;
            return Update[page];
        }

        public static void All()
        {
            History();
            Converter();
            Pattern();
            Tester();
        }

        /// <summary>
        /// Обновление списка инструментов в Истории
        /// </summary>
        // iid - ID инструмента
        public static void History()
        {
            if (isUpdateLater(1))
                return;

            global.MW.History.InstrumentListBoxFill();
            global.MW.History.DownloadedListCreate();
        }

        /// <summary>
        /// Обновление списка 1m таймфреймов в Конвертации
        /// </summary>
        public static void Converter()
        {
            if (isUpdateLater(2))
                return;
        }

        /// <summary>
        /// Обновление списка графиков в Поиске паттернов
        /// </summary>
        public static void Pattern()
        {
            if (isUpdateLater(3))
                return;
        }

        /// <summary>
        /// Обновление списка инструментов в Тестере
        /// </summary>
        public static void Tester()
        {
            if (isUpdateLater(4))
                return;

            global.MW.Tester.InstrumentListBox.ItemsSource = null;
            global.MW.Tester.InstrumentListBox.ItemsSource = Candle.ListAll();
        }
        public static void Section()
        {
        }
    }
}
