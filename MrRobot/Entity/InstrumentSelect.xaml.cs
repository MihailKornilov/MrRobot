using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public partial class InstrumentSelect : UserControl
    {
        public InstrumentSelect()
        {
            InitializeComponent();

            QuoteCoin();
        }

        /// <summary>
        /// Список котировочных монет с количествами
        /// </summary>
        void QuoteCoin()
        {
            string sql = "SELECT" +
                            "`quoteCoin`," +
                            "COUNT(*)`count`" +
                         "FROM`_instrument`" +
                         "GROUP BY`quoteCoin`" +
                         "ORDER BY`count`DESC";
            var list = mysql.QueryList(sql);
            foreach (dynamic row in list)
                QuoteCoinBox.Items.Add(new QCoinCount(row["quoteCoin"], row["count"]));
        }
        /// <summary>
        /// Поиск по котировочной монете
        /// </summary>
        void QuoteCoinChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QuoteCoinBox.SelectedIndex == -1)
                return;

            CDIcheck.IsChecked = false;

            var item = QuoteCoinBox.SelectedItem as QCoinCount;
            FindBox.Text = $"/{item.Coin}";
            QuoteCoinBox.SelectedIndex = -1;
        }

        /*
                /// <summary>
                /// Выбран инструмент
                /// </summary>
                void InstrumentChanged(object sender, MouseButtonEventArgs e) => InstrumentChanged();
                void InstrumentChanged()
                {
                    Depth.Start(item.Symbol, EChart.TradeCandlesActual(item));
                }

                /// <summary>
                /// Отмена выбора инструмента
                /// </summary>
                void InstrumentCancel(object sender, MouseButtonEventArgs e)
                {
                    Depth.Stop();
                    EChart.Empty();
                }

        */
    }

    /// <summary>
    /// Шаблон для котировочных монет (для быстрого поиска инструментов)
    /// </summary>
    public class QCoinCount
    {
        public QCoinCount(string coin, string count)
        {
            Coin = coin;
            Count = count;
        }

        public string Coin { get; set; }
        public string Count { get; set; }
    }


    class ISunit
    {
        public delegate void Dlg();
        public Dlg Changed = () => { };

        // Ассоциативный массив созданных ссылок
        static Dictionary<string, ISunit> ISlist { get; set; } = new Dictionary<string, ISunit>();
        static ISunit ISU { get; set; }
        static Border OpenPanel  { get => global.MW.ISPanel.OpenPanel; }
        static TextBox FindBox   { get => global.MW.ISPanel.FindBox; }
        static Label FoundCount  { get => global.MW.ISPanel.FoundCount; }
        static Label FoundCancel { get => global.MW.ISPanel.FoundCancel; }
        static CheckBox CDIcheck { get => global.MW.ISPanel.CDIcheck; }
        static ListBox ISBox     { get => global.MW.ISPanel.ISBox; }
        public static void Init(object sender, RoutedEventArgs RE)
        {
            FindBox.TextChanged += (s, e) =>
            {
                var items = Instrument.ListBox(ISU.FindTxt = FindBox.Text);
                ISBox.ItemsSource = items;
                bool isTxt = FindBox.Text.Length > 0;
                FoundCount.Content = isTxt ? $"найдено: {items.Count}" : "";
                global.Vis(FoundCancel, isTxt);
            };
            FoundCancel.MouseLeftButtonDown += (s, e) =>
            {
                FindBox.Text = "";
                CDIcheck.IsChecked = false;
            };
            CDIcheck.Checked   += (s, e) => FindBox.Text = "/HISTORY";
            CDIcheck.Unchecked += (s, e) => FindBox.Text = "";
            ISBox.MouseDoubleClick += (s, e) =>
            {
                if (ISBox.SelectedIndex == -1)
                    return;

                GridBack.Remove();
                var unit = ISBox.SelectedItem as InstrumentUnit;
                ISU.ChosenId = unit.Id;
            };
        }
        // Установка инструмента со стороны
        public static void Chose(string name, int iid)
        {
            if (!ISlist.ContainsKey(name))
                return;

            ISU = ISlist[name];
            ISU.ChosenId = iid;
        }

        // Создание ссылки для выбора инструмента из выпадающего списка
        public ISunit(TextBlock tb)
        {
            TB = tb;
            TB.MouseLeftButtonDown += Open;
            NoSelTxt = TB.Text;
            CancelAdd();
            ChosenApply();
            ISlist.Add(Name, this);
        }

        // Показывать циферки со скачанными свечными данными
        public bool WithHistory { get; set; } = false;
        TextBlock TB { get; set; }
        string Name { get => TB.Name; }
        string NoSelTxt { get; set; }
        public string PosPrefix { get => $"{Name}."; }

        TextBlock X { get; set; }
        // Вставка красного крестика для отмены выбора инструмента
        void CancelAdd()
        {
            X = new TextBlock();
            X.Text = "x";
            X.FontSize = TB.FontSize;
            X.Padding = new Thickness(TB.Padding.Left, TB.Padding.Top, TB.Padding.Right, TB.Padding.Bottom);
            X.Style = Application.Current.Resources["CancelRed"] as Style;
            X.MouseLeftButtonDown += (s, e) => ChosenId = 0;
            var panel = TB.Parent as Panel;
            panel.Children.Add(X);
        }

        // Текст строки поиска
        string FindTxt
        {
            get => position.Val($"{PosPrefix}FindTxt", "");
            set => position.Set($"{PosPrefix}FindTxt", value);
        }

        // Открытие окна со списком
        void Open(object sender, MouseButtonEventArgs e)
        {
            ISU = ISlist[Name];
            ISBox.ItemsSource = Instrument.ListBox(FindTxt);

            var win = global.MW.PointToScreen(new Point(0, 0));
            var el = TB.PointToScreen(new Point(0, 0));
            int left = (int)(el.X - win.X) - 64;
            int top = (int)(el.Y - win.Y) + 20;

            OpenPanel.Margin = new Thickness(left, top, 0, 0);
            global.Vis(CDIcheck, WithHistory);
            global.Vis(OpenPanel);

            new GridBack(global.MW.ISPanel);

            FindBox.Text = FindTxt;
            FindBox.Focus();
        }

        // ID выбранного инструмента
        int ChosenId
        {
            get => position.Val($"{PosPrefix}ChosenId", 0);
            set
            {
                position.Set($"{PosPrefix}ChosenId", value);
                ChosenApply();
            }
        }
        // Применение выбранного инструмента
        void ChosenApply()
        {
            bool isSel = ChosenId > 0;
            TB.Text = isSel ? Instrument.Unit(ChosenId).Name : NoSelTxt;
            TB.Style = Application.Current.Resources[$"TBLink{(isSel ? "Sel" : "")}"] as Style;
            global.Vis(X, isSel);
            Changed();
        }

        public InstrumentUnit IUnit { get => Instrument.Unit(ChosenId); }
    }
}
