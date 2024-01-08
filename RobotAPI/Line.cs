/*
    Рисование линий при создании ордеров
 */
using System;

namespace RobotAPI
{
    public static partial class Robot
    {
        /// <summary>
        /// Рисование горизонтальных линий BUY,SELL,TP,SL на графике 
        /// </summary>
        private static string[] LineMass;
        private static void Line(string type, double price)
        {
            string content = type + ";" + price;

            if (LineMass == null)
            {
                LineMass = new string[1] { content };
                return;
            }

            string[] tmp = LineMass;
            int len = tmp.Length;
            LineMass = new string[len + 1];
            Array.Copy(tmp, LineMass, len);
            LineMass[len] = content;
        }
        public static string[] LineGet()
        {
            string[] tmp = LineMass;
            LineMass = null;
            return tmp;
        }
    }
}
