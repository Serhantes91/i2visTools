using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class GeoPhase
    {
        public GeoPhase()
        {
            Points = new List<PTPoint>();
        }

        public string Name { get; set; }
        public List<PTPoint> Points { get; set; }
        public Color? Color { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
