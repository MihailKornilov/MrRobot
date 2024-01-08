
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

using MrRobot.inc;

namespace MrRobot.Entity
{
    public static class Robots
    {
        static List<RobotUnit> RobotList;

        /// <summary>
        /// Создание списка роботов для выбора
        /// </summary>
        static void ListCreate(bool upd = false)
        {
            if (!upd && RobotList != null && RobotList.Count > 0)
                return;

            RobotList = new List<RobotUnit>
            {
                new RobotUnit { Name = "Не выбран" }
            };

            string sql = "SELECT*FROM`_robot`ORDER BY`name`";
            var mass = mysql.QueryList(sql);
            if (mass.Count == 0)
                return;

            int num = 1;
            foreach (Dictionary<string, string> v in mass)
                RobotList.Add(new RobotUnit
                {
                    Num = num++.ToString() + ".",
                    Id = Convert.ToInt32(v["id"]),
                    Name = v["name"],
                    Path = v["path"]
                });
        }

        public static List<RobotUnit> ListBox()
        {
            ListCreate();
            return RobotList;
        }

        /// <summary>
        /// Выбор файла робота и проверка на корректность
        /// </summary>
        public static void Load(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "DLL Files (*.dll)|*.dll";
            if (dialog.ShowDialog() == false)
            {
                error.Msg("Не удалось открыть файл.");
                return;
            }

            Assembly Asm;
            try
            {
                Asm = Assembly.LoadFrom(dialog.FileName);
            }
            catch
            {
                error.Msg("Не удалось загрузить файл робота.");
                return;
            }

            // Получение имени файла робота без расширения
            string[] spl = dialog.FileName.Split('\\');
            int c = spl.Length - 1;
            spl = spl[c].Split('.');
            string Name = spl[0];

            // Поиск типа (класса) в сборке .dll
            Type type = Asm.GetType(Name);
            if (type == null)
            {
                error.Msg("Данный файл не является файлом-роботом. Либо имя файла не совпадает с названием класса.");
                return;
            }

            // Создание экземпляра объекта типа (класса)
            //object obj = Activator.CreateInstance(type);

            // Проверка наличия метода Init
            if (type.GetMethod("Init") == null)
            {
                error.Msg("В классе отсутствует метод Init. Либо данный файл не является файлом-роботом.");
                return;
            }

            // Проверка наличия метода Step
            if (type.GetMethod("Step") == null)
            {
                error.Msg("В классе отсутствует метод Step. Либо данный файл не является файлом-роботом.");
                return;
            }

            // Проверка наличия метода Finish
            //if (type.GetMethod("Finish") == null)
            //{
            //    error.Msg("В классе отсутствует метод Finish. Либо данный файл не является файлом-роботом.");
            //    return;
            //}

            BaseInsert(dialog.FileName, Name);
            ListCreate();
            global.MW.Tester.RobotsListBox.ItemsSource = ListBox();
            global.MW.Trade.RobotsListBox.ItemsSource = ListBox();
        }

        /// <summary>
        /// Проверка наличия робота в базе. Если нет, то внесение.
        /// </summary>
        static void BaseInsert(string path, string name)
        {
            string sql = "SELECT " +
                            "COUNT(*)" +
                          "FROM`_robot`" +
                         $"WHERE`name`='{name}'";
            int count = Convert.ToInt32(mysql.QueryString(sql));
            if (count > 0)
            {
                error.Msg("Данный робот уже присутствует в списке.");
                return;
            }

            path = path.Replace('\\', '/');
            sql = $"INSERT INTO`_robot`(`name`,`path`)VALUES('{name}','{path}')";
            mysql.Query(sql);
        }
    }


    public class RobotUnit
    {
        public string Num { get; set; }     // Порядковый номер для отображения в списке
        // Ширина порядкового номера по условию
        public int NumWidth {
            get { return Id == 0 ? 0 : 23; }
        }
        public int Id { get; set; }
        public string Name { get; set; }    // Имя, а также название сборки и класса
        // Цвет названия по условию
        public string NameColor {
            get { return Id == 0 ? "#999" : "#000"; }
        }
        public string Path { get; set; }    // Полный путь к роботу на диске
    }
}
