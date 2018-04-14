using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.InitClasses
{
    public class CalcGrid
    {
//        /GRID_PARAMETERS_DESCRIPTIONS
//      2041-xnumx
//       201-ynumy
//         8-mnumx
//        -4-Mnumy -5 +5
//   4000000-xsize(m)
//    400000-ysize(m)
//      0000-pinit(Pa)
//         0-GXKOEF(m/sek^2)
//   9.80665-GYKOEF(m/sek^2)
//0-timesum(Years)
//100-nonstab 0.5-dx 0.5-dy
    
        
        /// <summary>
        /// Кол-во узлов по оси X
        /// </summary>
        public int Xnumx { get; set; }
        /// <summary>
        /// Кол-во узлов по оси Y
        /// </summary>
        public int Ynumy { get; set; }

        public int Mnumx { get; set; }
        public int Mnumy { get; set; }

        // TODO выяснить что это и изменить названия свойств
        public int Mnumy_left { get; set; }
        public int Mnumy_rigth { get; set; }

        /// <summary>
        /// Размер области по оси X (в метрах)
        /// </summary>
        public int Xsize { get; set; }
        /// <summary>
        /// Размер области по оси Y (в метрах)
        /// </summary>
        public int Ysize { get; set; }

        public double Pinit { get; set; }

        public double GxKoef { get; set; }
        public double GyKoef { get; set; }

        public double TimeSum { get; set; }

        // TODO выяснить
        public int NonStab { get; set; }
    
        public double dx { get; set; }
        public double dy { get; set; }
    }
}
