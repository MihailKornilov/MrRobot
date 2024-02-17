using System.Windows.Media;

namespace MrRobot.Interface
{
    // Информация об инструменте: базовый интерфейс
    interface IIunit
    {
        string Num { get; set; }            // Порядковый номер для вывода в списке
        int Id { get; set; }                // ID инструмента
        
        string Name { get; set; }           //

        string HistoryBegin { get; set; }   // Дата начала истории свечных данных
        int Decimals { get; set; }          // Количество нулей после запятой
        bool IsTrading { get; set; }        // Инструмент торгуется или нет

        int CdiCount { get; set; }          // Количество скачанных свечных данных
        SolidColorBrush CdiCountColor { get; set; }// Скрытие количества, если 0
    }


    // Информация об инструменте: поля для ByBit
    interface IIunitBYBIT
    {
        string BaseCoin { get; set; }       // Название базовой монеты
        string QuoteCoin { get; set; }      // Название котировочной монеты
        string Symbol { get; set; }         // Название инструмента в виде "BTCUSDT"
        double BasePrecision { get; set; }  // Точность базовой монеты
        double MinOrderQty { get; set; }    // Минимальная сумма ордера
        double TickSize { get; set; }       // Шаг цены
    }


    // Информация об инструменте: поля для МосБиржи
    interface IIunitMOEX
    {
        string SecId { get; set; }          // Код ценной бумаги
        string ShortName { get; set; }      // Краткое наименование
        int TypeId { get; set; }            // ID вида инструмента
        int GroupId { get; set; }           // ID группы инструмента
    }
}
