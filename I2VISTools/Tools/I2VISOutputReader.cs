using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace I2VISTools.Tools
{
    public static class I2VISOutputReader
    {

        public static double[,] GetTemperatureArrayFromTxt(string temperatureFile)
        {
            List<string> A = new List<string>();
            int s = 0;
            // считываем из файла значения температур
            using (StreamReader sr = new StreamReader(temperatureFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var sublines = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var subline in sublines)
                    {
                        A.Add(subline);
                        s++;
                    }
                }
            }

            int num = 3;
            int ind = 0;

            double value;
            int ii, jj;

            int coordX = Convert.ToInt16(A[1]);
            int coordZ = Convert.ToInt16(A[2]);

            var valueGrid = new double[coordZ + 1, coordX + 1];

            // Перезаполнение ColorGrid значениями температур
            while (num < s)
            {
                //value = Convert.ToDouble(A[num]);
                value = Config.Tools.ParseOrDefaultDouble(A[num]);

                jj = ind / coordZ;
                ii = ind % coordZ;
                valueGrid[ii,jj] = value - 273;
                ind++;
                num++;
            }

            return valueGrid;

        }


        public static double[,] GetRocksArrayFromTxt(string rocksFile)
        {
           
            List<string> A = new List<string>();
            long s = 0;

            using (StreamReader sr = new StreamReader(rocksFile))
            {
                string line;
                // Read and display lines from the file until the end of 
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    var sublines = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var subline in sublines)
                    {
                        A.Add(subline);
                        s++;
                    }
                }
            }

            string Time = A[0];
            int Coord_x = Convert.ToInt16(A[1]);
            int Coord_z = Convert.ToInt16(A[2]);

            //var ColorGrid = new List<List<double>>(Coord_x * Coord_z);
            var ColorGrid = new double[Coord_z+1,Coord_x+1];

            for (int i = 0; i < Coord_z; i++)
            {
                for (int j = 0; j < Coord_x; j++)
                {
                    ColorGrid[i,j] = 0;
                }
            }

            int num = 3;
            int ind = 1;

            double value;
            int num_colors;
            int material;
            int ind_vec1, ind_vec2;

            int jj, ii;

            while (num < s)
            {
                value = Convert.ToInt16(A[num]);

                if (value == -2)
                {

                    num_colors = Convert.ToInt16(A[num + 1]);
                    material = Convert.ToInt16(A[num + 2]);

                    ind_vec1 = ind;
                    ind_vec2 = ind + num_colors - 1;

                    ind = ind + num_colors;
                    num = num + 3;
                }
                else
                {
                    if (value == -1)
                    {
                        material = -999;
                    }
                    else
                    {
                        material = (int)value;
                    }
                    //ind_vec1 = ind_vec2 = ind;
                    ind_vec2 = ind;
                    ind_vec1 = ind;
                    ind++;
                    num++;
                }

                for (int n = ind_vec1; n <= ind_vec2; n++)
                {
                    jj = n / Coord_z;
                    ii = n % Coord_z;
                    ColorGrid[ii,jj] = material;
                }

            }

            return ColorGrid;
        }

    }
}
