using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class MetamorphicFacie
    {

        public MetamorphicFacie()
        {
            Points = new List<PTPoint>();
        }

        public string Name { get; set; }
        public List<PTPoint> Points;
        public Color? Color { get; set; }

        public bool IsPointWithin(PTPoint point)
        {
            bool isInside = false;
            for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
            {
                if (((Points[i].P > point.P) != (Points[j].P > point.P)) &&
                (point.T < (Points[j].T - Points[i].T) * (point.P - Points[i].P) / (Points[j].P - Points[i].P) + Points[i].T))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
