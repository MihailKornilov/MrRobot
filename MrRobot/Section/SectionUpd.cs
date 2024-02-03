using static System.Console;

using MrRobot.inc;

namespace MrRobot.Section
{
    public static class SectionUpd
    {
        public static bool[] Update = new bool[6];

        static bool isUpdateLater(int page) => Update[page] = position.MainMenu() != page;

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
        }
        public static void Section()
        {
        }
    }
}
