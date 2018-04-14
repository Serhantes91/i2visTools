using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Media;
using System.Xml.Schema;
using I2VISTools.ModelClasses;
using I2VISTools.Subclasses;
using MathNet.Numerics.Providers.LinearAlgebra;

namespace I2VISTools.Config
{
    public static class Tools
    {

        public static double PointsDistance(ModPoint p1, ModPoint p2)
        {
            return Math.Sqrt((p1.X - p2.X)*(p1.X - p2.X) + (p1.Y - p2.Y)*(p1.Y - p2.Y));
        }

        public static double PointsDistance(Marker p1, ModPoint p2)
        {
            return Math.Sqrt((p1.XPosition - p2.X) * (p1.XPosition - p2.X) + (p1.YPosition - p2.Y) * (p1.YPosition - p2.Y));
        }

        public static double PointsDistance(Marker p1, Marker p2)
        {
            return Math.Sqrt((p1.XPosition - p2.XPosition) * (p1.XPosition - p2.XPosition) + (p1.YPosition - p2.YPosition) * (p1.YPosition - p2.YPosition));
        }

        public static double PointsDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
        }

        /// <summary>
        /// Линейная функция y = kx + b по заданным k и b
        /// </summary>
        /// <param name="x"></param>
        /// <param name="k"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double LinearFunction(double x, double k, double b)
        {
            return k*x + b;
        }
        /// <summary>
        /// Линейная функция по двум заданным точкам
        /// </summary>
        /// <param name="x"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static double LinearFunction(double x, double x1, double y1, double x2, double y2)
        {
            var k = (y1 - y2)/(x1 - x2);
            var b = y1 - k*x1;
            return LinearFunction(x, k, b);
        }

        public static double ExtractNumberFromString(string line)
        {
            var numstring = Regex.Match(line, @"\d+").Value;
            return ParseOrDefaultDouble(numstring);
        }

        public static double ParseOrDefaultDouble(string text)
        {

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                text = text.Replace(".", ",");
            }

            double tmp = 0;
            Double.TryParse(text, out tmp);
            return tmp;
        }

        public static double ParseOrDefaultDouble(string text, double def)
        {

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                text = text.Replace(".", ",");
            }

            double tmp = def;
            Double.TryParse(text, out tmp);
            return tmp;
        }

        public static int ParseOrDefaultInt(string text)
        {
            int tmp = 0;
            Int32.TryParse(text, out tmp);
            return tmp;
        }

        public static byte ParseOrDefaultByte(string text, byte defaultValue)
        {
            byte tmp = 0;
            if (byte.TryParse(text, out tmp)) return tmp;
            else return defaultValue;
        }

        public static int ParseOrDefaultInt(string text, int defaultValue)
        {
            int tmp = defaultValue;
            if (Int32.TryParse(text, out tmp)) return tmp;
            else return defaultValue;
        }

        public static uint ParseOrDefaultUInt(string text, uint defaultValue)
        {
            uint tmp = defaultValue;
            UInt32.TryParse(text, out tmp);
            return tmp;
        }

        public static bool OnlyNumeric(string text)
        {
            Regex regex = new Regex("^[0-9]*$"); //regex that allows numeric input only
            //Regex regex = new Regex(@"^\d*\,?\d*$");
            return !regex.IsMatch(text);
        }

        public static Color ColorFromValue(double value, double min, double max)
        {
            var result = new Color();
            result.A = 255;

            if (value < min || value > max)
            {
                result.R = 255;
                result.G = 255;
                result.B = 255;
                return result;
                //throw new Exception("Заданное значение находится вне заданного диапазона!");
            }

            var rangeLength = max - min;
            var quarterRange = rangeLength/4;

            var valueLength = value - min;
            var valuePosition = valueLength/rangeLength;

            if (valuePosition >= 0 && valuePosition <= 0.25)
            {
                result.R = 0;
                result.G = Convert.ToByte((valueLength/quarterRange)*255);
                result.B = 255;
            }
            if (valuePosition > 0.25 && valuePosition <= 0.5)
            {
                result.R = 0;
                result.G = 255;
                result.B = Convert.ToByte(255 - (((valueLength - quarterRange) / quarterRange) * 255));
            }
            if (valuePosition > 0.5 && valuePosition <= 0.75)
            {
                result.R = Convert.ToByte(((valueLength - 2*quarterRange) / quarterRange) * 255);
                result.G = 255;
                result.B = 0;
            }
            if (valuePosition > 0.75 && valuePosition <= 1)
            {
                result.R = 255;
                result.G = Convert.ToByte(255 - (((valueLength - 3*quarterRange) / quarterRange) * 255));
                result.B = 0;
            }

            return result;
        }

        public static double[,] Transpose(double[,] initArray)
        {

            var resArray = new double[initArray.GetLength(1), initArray.GetLength(0)];

            for (int i = 0; i < initArray.GetLength(0); i++)
            {
                for (int j = 0; j < initArray.GetLength(1); j++)
                {
                    resArray[j, i] = initArray[i, j];
                }
            }

            return resArray;
        }

        public static void FillArray(double[,] arr, double value)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                for (int j = 0; j < arr.GetLength(1); j++)
                {
                    arr[i, j] = value;
                }
            }
        }

        public static double[,] InterpolateArray(double[,] initArray, int xsize, int ysize)
        {
            if (xsize == initArray.GetLength(0) && ysize == initArray.GetLength(1)) return initArray;

            var resArray = new double[xsize, ysize];
            FillArray(resArray, double.NaN);

            //todo учесть, что исходный массив может быть больше интерполируемого
            var xinit = initArray.GetLength(0);
            var yinit = initArray.GetLength(1);

            var ktX = Convert.ToDouble(xsize-1)/Convert.ToDouble(xinit-1);
            var ktY = Convert.ToDouble(ysize-1)/Convert.ToDouble(yinit-1);

            var xNodes = new List<int>(); //строчные индексы опорных значений интерполируемого массива
            var yNodes = new List<int>(); //столбовые индексы опорных значений интерполируемого массива

            //равномерно распределяем элементы исходного массива в интерполируемый
            for (int i = 0; i < xinit; i++)
            {
                int ni = Convert.ToInt32(i * ktX);
                xNodes.Add(ni);

                for (int j = 0; j < yinit; j++)
                {    
                    int nj = Convert.ToInt32(j*ktY);
                    resArray[ni,nj] = initArray[i, j];

                    if (i==0) yNodes.Add(nj);

                }
            }

            //интерполируем

            for (int i = 0; i < xsize; i++)
            {
                for (int j = 0; j < ysize; j++)
                {

                    if (double.IsNaN(resArray[i, j]))
                    {
                        var li = LeftInd(xNodes, i);
                        var lj = LeftInd(yNodes, j);
                        var ri = RightInd(xNodes, i);
                        var rj = RightInd(yNodes, j);

                        var interpol = new InterpolationRectangle(new ModPoint(li, lj), new ModPoint(ri, rj), resArray[li, lj], resArray[li, rj], resArray[ri, lj], resArray[ri, rj], new ModPoint(i, j) );
                        resArray[i, j] = interpol.InterpolatedValue;

                    }

                }
            }

            return resArray;
        }

        public static List<List<double>> Interpolate2DList(List<List<double>> initList, int xsize, int ysize)
        {
            var arr = new double[initList.Count, initList[0].Count];

            for (int i = 0; i < initList.Count; i++)
            {
                for (int j = 0; j < initList[i].Count; j++)
                {
                    arr[i, j] = initList[i][j];
                }
            }

            var resAr = InterpolateArray(arr, xsize, ysize);

            var res = new List<List<double>>();
            for (int i = 0; i < resAr.GetLength(0); i++)
            {
                res.Add(new List<double>());
                for (int j = 0; j < resAr.GetLength(1); j++)
                {
                    res.Last().Add(resAr[i,j]);
                }
            }
            return res;
        }


        private static int LeftInd(List<int> indexes, int ind)
        {
            if (indexes[0] <= ind && indexes[1] > ind) return indexes[0];
            for (int i = 1; i < indexes.Count; i++)
            {
                if (indexes[i] > ind) return indexes[i - 1];
            }

            if (ind == indexes.Last()) return indexes[indexes.Count - 2];

            return -1;
        }

        private static int RightInd(List<int> indexes, int ind)
        {
            if (ind == indexes.Last()) return indexes.Last();
            for (int i = 1; i < indexes.Count; i++)
            {
                if (indexes[i] > ind) return indexes[i];
            }

            return -1;
        }


    }

    //class RGBColor

}
