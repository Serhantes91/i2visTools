using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.InitClasses
{
    public class RockDescription
    {
        //ROCKS_DESCRIPTIONS/___NUM_______________NU(Pa^MM*s)___DE(J)_________DV(J/bar)_____SS(Pa)________MM(Power)_____LL(KOEF)____RO(kg/M^3)____bRo(1/K)______aRo(1/kbar)___CP(J/kg)______Kt(Wt/(m*K))__Ht(Wt/kg)
        /// <summary>
        /// Название породы
        /// </summary>
        public string RockName { get; set; }
        /// <summary>
        /// ID породы
        /// </summary>
        public int RockNum { get; set; }
        /// <summary>
        /// Вязкость
        /// </summary>
        public double Nu { get; set; }

        //TODO выяснить подробнее что за свойства и прописать их в summary
        public double De { get; set; }
        public double Dv { get; set; }
        public double Ss { get; set; }
        public double Mm { get; set; }
        public double Ll { get; set; }
        public double Ro { get; set; }
        public double bRo { get; set; }
        public double aRo { get; set; }
        public double Cp { get; set; }
        public double Kt { get; set; }
        public double Ht { get; set; }

        // TODO xz
        public double n0 { get; set; }
        public double n1 { get; set; }
        public double s0 { get; set; }
        public double s1 { get; set; }
        public double dh { get; set; }
        public double a0 { get; set; }
        public double a1 { get; set; }
        public double b0 { get; set; }
        public double b1 { get; set; }
        public double e0 { get; set; }
        public double e1 { get; set; }
        public double kf { get; set; }
        public double kp { get; set; }
    }
}
