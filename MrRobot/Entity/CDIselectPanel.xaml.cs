using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Collections.Generic;
using static System.Console;
using MrRobot.inc;

namespace MrRobot.Entity
{
    /// <summary>
    /// Логика взаимодействия для CDIselect.xaml
    /// </summary>
    public partial class CDIselectPanel : UserControl
    {
        public CDIselectPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Быстрый поиск
        /// </summary>
        void FindChanged(object sender, TextChangedEventArgs e)
        {
            LabelFound.Content = CDIpanel.LabelFound;
            FoundCancel.Visibility = LabelFound.Content.ToString().Length > 0 ? Visibility.Visible : Visibility.Hidden;
        }
        /// <summary>
        /// Отмена быстрого поиска
        /// </summary>
        void FoundCancelClick(object sender, MouseButtonEventArgs e)
        {
            FindBox.Text = "";
            FindBox.Focus();
        }
        /// <summary>
        /// Нажатие на группу свечных данных
        /// </summary>
        void GroupClick(object sender, SelectionChangedEventArgs e)
        {
            if (GroupBox.SelectedIndex == -1)
                return;

            var item = GroupBox.SelectedItem as CDIunit;
            FindBox.Text = item.Name;
            GroupBox.SelectedIndex = -1;

        }
        /// <summary>
        /// Свечные данные выбраны
        /// </summary>
        void CDIselected(object sender, MouseButtonEventArgs e) => CDIpanel.Selected();
    }

    public class CDIpageUnit
    {
        public delegate void Dcall();
        public static Dcall OutMethod;

        public CDIpageUnit(int page, string src = "выбрать")
        {
            Page = page;
            TBlinkSrc = src;
        }
        int Page { get; set; }   // Номер страницы
        public TextBlock TBLink { get; set; }
        string TBlinkSrc { get; set; }   // Текст ссылки, при нажатии на которую открывается окно выбора (конда свечные данные не выбраны)
        public void TBlinkTxt()
        {
            if (CdiId == 0)
                TBLink.Text = TBlinkSrc;
            else
            {
                var unit = Candle.Unit(CdiId);
                TBLink.Text = $"{unit.Name} {unit.TF}";
                TBLink.Foreground = format.RGB("004481");
            }

            TBLink.FontWeight = CdiId == 0 ? FontWeights.Normal : FontWeights.Medium;

            //if (CdiId == 0)
            //    TBLink.ClearValue(ForegroundProperty);
        }
        public List<CDIunit> Items()
        {
            if (Page == 2)
                return Candle.List1m(FindTxt);

            return Candle.ListAll(FindTxt);
        }
        // Показывать или нет группы свечных данных
        public Visibility GroupVisible { get { return Page == 2 ? Visibility.Collapsed : Visibility.Visible; } }
        // ID выбранных свечных данных
        public int CdiId
        {
            get => Candle.Id(position.Val($"{Page}.CDIselect.ID", 0));
            set
            {
                position.Set($"{Page}.CDIselect.ID", value);
                TBlinkTxt();
                OutMethod();
            }
        }
        // Текст строки поиска
        public string FindTxt
        {
            get { return position.Val($"{Page}.CDIselect.FindTxt", ""); }
            set { position.Set($"{Page}.CDIselect.FindTxt", value); }
        }
    }

    public class CDIpanel
    {
        static Dictionary<int, CDIpageUnit> PageASS;   // Ассоциативный массив с данными по каждой странице
        static CDIpageUnit PU
        {
            get
            {
                int page = position.MainMenu();
                if (!PageASS.ContainsKey(page))
                    return null;
                return PageASS[page];
            }
        }

        public static int CdiId
        {
            get => PU.CdiId;
            set { PU.CdiId = value; }
        }
        public static CDIunit CdiUnit() => Candle.Unit(CdiId);


        public CDIpanel()
        {
            PageASS = new Dictionary<int, CDIpageUnit>();
            foreach (int page in new int[]{2,3,4})
                PageASS.Add(page, new CDIpageUnit(page));
        }
        public static CDIpageUnit Page(int page)
        {
            return PageASS[page];
        }
        static Border OpenPanel  { get { return global.MW.CDIselectPanel.OpenPanel; } }
        static ListView GroupBox { get { return global.MW.CDIselectPanel.GroupBox; } }
        static ListBox CDIList   { get { return global.MW.CDIselectPanel.CDIList; } }
        static TextBox FindBox   { get { return global.MW.CDIselectPanel.FindBox; } }
        public static void PageChanged()
        {
            Hide();
            PU?.TBlinkTxt();
        }
        /// <summary>
        /// Показ списка свечных данных
        /// </summary>
        public static void Open(int left, int top)
        {
            if (Hide())
                return;

            CDIList.ItemsSource = PU.Items();
            GroupBox.ItemsSource = Candle.ListGroup();
            GroupBox.Visibility = PU.GroupVisible;

            OpenPanel.Margin = new Thickness(left, top, 0, 0);
            OpenPanel.Visibility = Visibility.Visible;

            global.MW.GridBack.Visibility = Visibility.Visible;

            FindBox.Text = PU.FindTxt;
            FindBox.Focus();
        }
        /// <summary>
        /// Скрытие списка свечных данных
        /// </summary>
        public static bool Hide()
        {
            if (OpenPanel.Visibility == Visibility.Visible)
            {
                OpenPanel.Visibility = Visibility.Collapsed;
                global.MW.GridBack.Visibility = Visibility.Collapsed;
                return true;
            }

            return false;
        }

        public static void Selected()
        {
            var unit = CDIList.SelectedItem as CDIunit;
            PU.CdiId = unit.Id;
            Hide();
        }

        public static string LabelFound
        {
            get
            {
                PU.FindTxt = FindBox.Text;
                CDIList.ItemsSource = PU.Items();
                return FindBox.Text.Length > 0 ? $"найдено: {CDIList.Items.Count}" : "";
            }
        }

        public static bool Lock
        {
            get => PU.TBLink.IsEnabled;
            set => PU.TBLink.IsEnabled = value;
        }
    }
}
