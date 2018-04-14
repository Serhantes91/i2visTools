using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class IntRectangle
    {

        public IntRectangle() 
        {
            
        }

        public IntRectangle(uint x, uint y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public uint X { get; set; }
        public uint Y { get; set; }

        public uint Height { get; set; }
        public uint Width { get; set; }


    }


    public class CoordIntRectangle
    {
        public CoordIntRectangle()
        {
            
        }

        public CoordIntRectangle(int x1, int y1, int x2, int y2)
        {
            X1 = Math.Min(x1, x2);
            Y1 = Math.Min(y1, y2);
            X2 = Math.Max(x1, x2);
            Y2 = Math.Max(y1, y2);
        }

        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }

        public int kostyl { get; set; } //todo УДАЛИТЬ!

        public int Height { get { return Math.Abs(Y2 - Y1); } }
        public int Width { get { return Math.Abs(X2 - X1); } }

        public bool IsPointWithin(double x, double y)
        {
            return ((X1 <= x) && (X2 >= x) && (Y1 <= y) && (Y2 >= y));
        }

    }
}
