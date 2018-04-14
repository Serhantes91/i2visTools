using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class PTPoint
    {
        public PTPoint()
        {
            
        }

        /// <summary>
        /// Точка диаграммы Температура-Давление
        /// </summary>
        /// <param name="p">Давление</param>
        /// <param name="t">Температура</param>
        public PTPoint(double p, double t)
        {
            P = p;
            T = t;
        }

        /// <summary>
        /// Давление
        /// </summary>
        public double P { get; set; }
        /// <summary>
        /// Температура
        /// </summary>
        public double T { get; set; }
    }
}
