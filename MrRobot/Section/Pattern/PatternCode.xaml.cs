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

            var Unit = G.Pattern.FoundListBox.SelectedItem as PatternUnit;
            CodeCreate(Unit);
            PatternOnlyCreate(Unit);
        }

        void CodeCreate(PatternUnit Unit)
        {
            var CDI = Candle.Unit(Unit.CdiId);

            string code = "";

            code += $"// Инструмент: {CDI.Name}\n" +
                    $"// Таймфрейм: {CDI.TF}\n" +
                    $"// ID поиска: {Unit.SearchId}\n" +
                    $"// Номер паттерна: {Unit.Num}\n" +
                    $"// Совпадения: {Unit.Repeat}\n\n";

            code += $"if (INSTRUMENT.Id != {CDI.InstrumentId})\n" +
                     "{\n" +
                    $"    PRINT(\"Выбран неверный инструмент. Должен быть {CDI.Name}.\");\n" +
                     "    return;\n" +
                     "}\n";

            code += $"if (INSTRUMENT.TimeFrame != {CDI.TimeFrame})\n" +
                     "{\n" +
                    $"    PRINT(\"Неверный таймфрейм инструмента. Должен быть {CDI.TF}.\");\n" +
                     "    return;\n" +
                     "}\n";

            code += $"if (CANDLES.Count < {Unit.Length})\n" +
                     "    return;\n\n";

            string[] cndl = Unit.Struct.Split('\n');
            for(int i = 0; i < Unit.Length; i++)
            {
                string[] spl = cndl[Unit.Length - i - 1].Split(' ');
                code += $"if (CANDLES[{i}].WickTop != {spl[0]} || CANDLES[{i}].Body != {spl[1]} || CANDLES[{i}].WickBtm != {spl[2]})\n" +
                         "    return;\n";
            }

            PatternCodeBox.Text = code;
            PatternCodeBox.Focus();
        }

        void PatternOnlyCreate(PatternUnit Unit)
        {
            var CDI = Candle.Unit(Unit.CdiId);
            PatternOnly.Text = $"new PatternLook(\"{CDI.Symbol}\", {CDI.TimeFrame}, \"{Unit.StructDB}\")";
        }
    }
}
