using System.Windows;
using System.Windows.Controls;

using MrRobot.inc;
using MrRobot.Entity;
using MrRobot.Interface;

namespace MrRobot.Section
{
    public partial class SettingEntity : UserControl
    {
        public SettingEntity()
        {
            G.SettingEntity = this;
        }

        public void Init()
        {
            InitializeComponent();
            DataListCreate();
            Data_Market();
        }


        /// <summary>
        /// Виды данных
        /// </summary>
        string[] DataUnits = {
                "Биржи",
                "Инструменты",
                "Свечные данные",
                "Роботы"
            };
        /// <summary>
        /// Создание разделов меню с данными
        /// </summary>
        void DataListCreate()
        {
            foreach (string v in DataUnits)
                DataBox.Items.Add(new SettingEntityMenuUnit(v));

            DataBox.SelectionChanged += (s, e) => {
                int sel = DataBox.SelectedIndex;
                for (int i = 0; i < DataUnits.Length; i++)
                    (FindName($"DataPanel{i}") as Panel).Visibility = i == sel ? Visibility.Visible : Visibility.Collapsed;
            };
            DataBox.SelectedIndex = 0;
        }






        /////////// БИРЖИ ///////////////////////////////////////////////////
        void Data_Market()
        {
            MarketBox.ItemsSource = G.Exchange.ListAll;
            MarketSaveButton.Click += (s, e) =>
            {
                for(int i = 0; i < MarketBox.Items.Count; i++)
                {
                    var unit = MarketBox.Items[i] as SpisokUnit;

                    if (unit.Name.Trim().Length == 0)
                        continue;

                    string sql = "UPDATE`_market`" +
                                $"SET`name`='{unit.Name.Trim()}'," +
                                   $"`prefix`='{unit.Prefix.Trim()}'," +
                                   $"`url`='{unit.Url.Trim()}'" +
                                $"WHERE`id`={unit.Id}";
                    mysql.Query(sql);
                }

                G.Hid(MarketSaveButton);
                G.Vis(MarketSaveOk);
                new Exchange();
            };
            MarketSaveOkTB.MouseLeftButtonDown += (s, e) =>
            {
                G.Vis(MarketSaveButton);
                G.Hid(MarketSaveOk);
            };
        }
    }


    /// <summary>
    /// Единица списка-меню данных (сущностей)
    /// </summary>
    class SettingEntityMenuUnit
    {
        public SettingEntityMenuUnit(string name) => Name = name;
        public string Name { get; set; }
    }

}
