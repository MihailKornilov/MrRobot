using System.Windows;

using MrRobot.inc;
using MrRobot.Entity;

namespace MrRobot.Section
{
    /// <summary>
    /// Логика взаимодействия для PatternCode.xaml
    /// </summary>
    public partial class PatternCode : Window
    {
        public PatternCode()
        {
            InitializeComponent();

            var Unit = global.MW.Pattern.FoundListBox.SelectedItem as PatternFoundUnit;
            CodeCreate(Unit);
            PatternOnlyCreate(Unit);
        }

        void CodeCreate(PatternFoundUnit Unit)
        {
            var CDI = Candle.Unit(Unit.CdiId);

            string TF = format.TF(Unit.TimeFrame);
            string code = "";

            code += $"// Инструмент: {CDI.Name}\n" +
                    $"// Таймфрейм: {TF}\n" +
                    $"// ID поиска: {Unit.SearchId}\n" +
                    $"// Номер паттерна: {Unit.Num}\n" +
                    $"// Совпадения: {Unit.Repeat}\n\n";

            code += $"if (INSTRUMENT.Id != {CDI.InstrumentId})\n" +
                     "{\n" +
                    $"    PRINT(\"Выбран неверный инструмент. Должен быть {CDI.Name}.\");\n" +
                     "    return;\n" +
                     "}\n";

            code += $"if (INSTRUMENT.TimeFrame != {Unit.TimeFrame})\n" +
                     "{\n" +
                    $"    PRINT(\"Неверный таймфрейм инструмента. Должен быть {TF}.\");\n" +
                     "    return;\n" +
                     "}\n";

            code += $"if (CANDLES.Count < {Unit.PatternLength})\n" +
                     "    return;\n\n";

            string[] cndl = Unit.Cndl.Split('\n');
            for(int i = 0; i < Unit.PatternLength; i++)
            {
                string[] spl = cndl[Unit.PatternLength - i - 1].Split(' ');
                code += $"if (CANDLES[{i}].WickTop != {spl[0]} || CANDLES[{i}].Body != {spl[1]} || CANDLES[{i}].WickBtm != {spl[2]})\n" +
                         "    return;\n";
            }

            PatternCodeBox.Text = code;
            PatternCodeBox.Focus();
        }

        void PatternOnlyCreate(PatternFoundUnit Unit)
        {
            var CDI = Candle.Unit(Unit.CdiId);
            string content = Unit.Cndl.Replace('\n', ';');
            PatternOnly.Text = $"new PatternLook(\"{CDI.Symbol}\", {CDI.TimeFrame}, \"{content}\")";
        }
    }
}
