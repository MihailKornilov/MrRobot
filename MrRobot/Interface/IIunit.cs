namespace MrRobot.Interface
{
    // Информация об инструменте: поля для МосБиржи
    interface IIunitMOEX
    {
        string SecId { get; set; }          // Код ценной бумаги
        string ShortName { get; set; }      // Краткое наименование
        int TypeId { get; set; }            // ID вида инструмента
        int GroupId { get; set; }           // ID группы инструмента
    }
}
