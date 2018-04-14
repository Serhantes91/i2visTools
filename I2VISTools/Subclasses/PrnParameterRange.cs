using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class PrnParameterRange
    {
        public string PrnMarking { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }

        public override string ToString()
        {
            string minString = Min.ToString("0.##");
            string maxString = Max.ToString("0.##");

            if (Math.Abs(Min) < 0.001) minString = Min.ToString("0.00e+00");
            if (Math.Abs(Max) < 0.001) maxString = Max.ToString("0.00e+00");

            if (Math.Abs(Min) > 10000) minString = Min.ToString("0.00e+00");
            if (Math.Abs(Max) > 10000) maxString = Max.ToString("0.00e+00");

            return PrnMarking + ": [" + minString + " ; " + maxString + "]";
        }
    }
}
