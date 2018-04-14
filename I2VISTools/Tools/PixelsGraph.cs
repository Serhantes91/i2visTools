using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Tools
{
    public static class PixelsGraph
    {

        public static uint[] GetPixelsArray(double[,] colorGrid, List<int[]> colorMap)
        {
            return GetPixelsArray(colorGrid, colorMap, 0, 0, colorGrid.GetLength(1), colorGrid.GetLength(0));
        }

        public static uint[] GetPixelsArray(double[,] colorGrid, List<int[]> colorMap, int gridBegX, int gridBegZ, int gridEndX, int gridEndZ)
        {
            int img_X;
            int img_Z;

            int mt;

            img_X = Convert.ToInt16(gridEndX - gridBegX);
            img_Z = Convert.ToInt16(gridEndZ - gridBegZ);

            int red;
            int green;
            int blue;
            int alpha;

            uint[] pixels = new uint[img_X * img_Z];

            for (int x = 0; x < img_X; x++)
            {
                for (int y = 0; y < img_Z; y++)
                {
                    int i = img_X * y + x;

                    //mt = ColorGrid[x][y + (int)grid_beg_x];
                    mt = (int)colorGrid[y,x + (int)gridBegX];
                    if (Math.Abs(mt) >= 36)
                    {
                        mt = 0;
                    }

                    if (mt < 0) mt = 0;

                    red = colorMap[mt][0];
                    green = colorMap[mt][1];
                    blue = colorMap[mt][2];
                    alpha = 255;

                    if (i >= pixels.Length) continue;
                    pixels[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                }
            }

            return pixels;
        }

    }
}
