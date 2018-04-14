using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class InterpolationRectangle
    {

        /// <summary>
        /// Интерполяция значения точки по 4 значениям, окружающим точку (прямоугольник) 
        ///  a1------a3
        ///  |       |
        ///  |       |
        ///  a2------a4
        /// </summary>
        /// <param name="a1">Верхняя левая вершина прямоугольника</param>
        /// <param name="a4">Нижняя правая верщина прямоугольника</param>
        /// <param name="value1">Значение верхней левой вершины</param>
        /// <param name="value2">Значение нижней левой вершины</param>
        /// <param name="value3">Значение верхней правой вершины</param>
        /// <param name="value4">Знаечние нижней правой вершины</param>
        /// <param name="innerPoint">Интерполируемая точка</param>
        public InterpolationRectangle(ModPoint a1, ModPoint a4, double value1, double value2, double value3, double value4, ModPoint innerPoint)
        {            
            Apex1 = new InterPoint(a1.X, a1.Y, value1);
            Apex2 = new InterPoint(a1.X, a4.Y, value2);
            Apex3 = new InterPoint(a4.X, a1.Y, value3);
            Apex4 = new InterPoint(a4.X, a4.Y, value4);

            InnerPoint = innerPoint;
        }

        public InterPoint Apex1 { get; set; }
        public InterPoint Apex2 { get; set; }
        public InterPoint Apex3 { get; set; }
        public InterPoint Apex4 { get; set; }

        public ModPoint InnerPoint { get; set; }

        public double InterpolatedValue
        {
            get
            {
                var width = Apex4.X - Apex1.X;
                var height = Apex4.Y - Apex1.Y;

                var ux = InnerPoint.X - Apex1.X;
                var uy = InnerPoint.Y - Apex1.Y;

                var uv = Apex3.Value - Apex1.Value;
                var bv = Apex4.Value - Apex2.Value;

                var upperXInter = Apex1.Value + uv*(ux/width);
                var bottomXInter = Apex2.Value + bv*(ux/width);

                var yInter = bottomXInter - upperXInter;

                return upperXInter + yInter*(uy/height);
            }
        }

    }

    public class InterPoint
    {

        public InterPoint(double x, double y, double val)
        {
            X = x;
            Y = y;
            Value = val;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Value { get; set; }
    }
}
