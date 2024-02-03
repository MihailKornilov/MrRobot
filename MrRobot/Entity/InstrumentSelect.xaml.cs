using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static System.Console;

using MrRobot.inc;
using MrRobot.Section;

namespace MrRobot.Entity
{
    /// <summary>
    /// Логика взаимодействия для InstrumentSelect.xaml
    /// </summary>
    public partial class InstrumentSelect : UserControl
    {
        public InstrumentSelect()
        {
            InitializeComponent();

            //InstrumentQuoteCoin();
        }

        /*
                /// <summary>
                /// Список котировочных монет с количествами
                /// </summary>
                void InstrumentQuoteCoin()
                {
                    string sql = "SELECT" +
                                    "`quoteCoin`," +
                                    "COUNT(*)`count`" +
                                 "FROM`_instrument`" +
                                 "GROUP BY`quoteCoin`" +
                                 "ORDER BY`count`DESC " +
                                 "LIMIT 4";
                    var list = mysql.QueryList(sql);
                    var items = new List<FindUnit>();
                    foreach (dynamic row in list)
                    {
                        items.Add(new FindUnit
                        {
                            Coin = row["quoteCoin"],
                            Count = " (" + row["count"] + ")"
                        });
                    }

                    QuoteCoinBox.ItemsSource = items;
                }
                /// <summary>
                /// Поиск по котировочной монете
                /// </summary>
                void QuoteCoinChanged(object sender, SelectionChangedEventArgs e)
                {
                    if (QuoteCoinBox.SelectedIndex == -1)
                        return;
                    var item = QuoteCoinBox.SelectedItem as FindUnit;
                    InstrumentFindBox.Text = "/" + item.Coin;
                    QuoteCoinBox.SelectedIndex = -1;
                }
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


    class ISunit
    {
        public delegate void Dlg();
        public Dlg Changed = () => { };

        // Ассоциативный массив созданных ссылок
        static Dictionary<string, ISunit> ISlist { get; set; }
        static ISunit ISU { get; set; }
        static Border OpenPanel { get => global.MW.ISPanel.OpenPanel; }
        static TextBox FindBox  { get => global.MW.ISPanel.FindBox; }
        static ListBox ISBox    { get => global.MW.ISPanel.ISBox; }
        static void Init()
        {
            if (ISlist != null)
                return;

            ISlist = new Dictionary<string, ISunit>();
            global.MW.Loaded += (s, e) => MWloaded();
        }
        static void MWloaded()
        {
            FindBox.TextChanged += (s, e) => ISBox.ItemsSource = Instrument.ListBox(ISU.FindTxt = FindBox.Text);
            ISBox.MouseDoubleClick += (s, e) =>
            {
                if (ISBox.SelectedIndex == -1)
                    return;

                GridBack.Remove();
                var unit = ISBox.SelectedItem as InstrumentUnit;
                ISU.ChosenId = unit.Id;
            };
        }

        // Создание ссылки для выбора инструмента из выпадающего списка
        public ISunit(TextBlock tb)
        {
            Init();

            TB = tb;
            TB.MouseLeftButtonDown += (s, e) => Open();
            NoSelTxt = TB.Text;
            CancelAdd();

            ChosenApply();
            ISlist.Add(Name, this);
        }
        TextBlock TB { get; set; }
        string Name { get => TB.Name; }
        string NoSelTxt { get; set; }
        string PosPrefix { get => $"{position.MainMenu()}.{Name}."; }

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
        void Open()
        {
            ISU = ISlist[Name];
            ISBox.ItemsSource = Instrument.ListBox(FindTxt);

            var win = global.MW.PointToScreen(new Point(0, 0));
            var el = TB.PointToScreen(new Point(0, 0));
            int left = (int)(el.X - win.X) - 64;
            int top = (int)(el.Y - win.Y) + 20;

            OpenPanel.Margin = new Thickness(left, top, 0, 0);
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
