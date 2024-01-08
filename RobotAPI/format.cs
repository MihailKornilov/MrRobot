using System;

namespace RobotAPI
{
    public static class format
    {
        /// <summary>
        /// Откидывание лишних нулей справа в дробных числах
        /// </summary>
        public static string NolDrop(string num)
        {
            int i;
            for(i = num.Length-1; i >= 0; i--)
                if (num[i] != '0' && num[i] != '.')
                    break;

            return num.Substring(0, i+1);
        }

        /// <summary>
        /// Избавление от E в маленьких числах
        /// </summary>
        public static string E(double num)
        {
            string numS = num.ToString();
            if (numS.IndexOf('E') == -1)
                return numS;

            return NolDrop(num.ToString("N12"));
        }

        /// <summary>
        /// Текущее время в виде 12:45:34
        /// </summary>
        public static string TimeNow()
        {
            return DateTime.Now.ToString().Substring(11, 8);
        }

    }
}
