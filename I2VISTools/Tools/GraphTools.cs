using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using I2VISTools.Config;
using I2VISTools.Subclasses;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace I2VISTools.Tools
{
    public static class GraphTools
    {

        public static double PointsDistance(DataPoint p1, DataPoint p2)
        {
            if (!p1.IsDefined() || !p2.IsDefined()) return Double.NaN;
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        public static double DistanceToLine(List<DataPoint> line, DataPoint pt)
        {
            if (line == null || line.Count < 2) return double.NaN;

            var a = line[0].Y - line[1].Y;
            var b = line[1].X - line[0].X;
            var c = line[0].X*line[1].Y - line[1].X*line[0].Y;

            return Math.Abs(a*pt.X + b*pt.Y + c)/Math.Sqrt(a*a + b*b);
        }

        public static double DistanceToLine(List<ScreenPoint> line, ScreenPoint pt)
        {
            if (line == null || line.Count < 2) return double.NaN;

            var a = line[0].Y - line[1].Y;
            var b = line[1].X - line[0].X;
            var c = line[0].X * line[1].Y - line[1].X * line[0].Y;

            return Math.Abs(a * pt.X + b * pt.Y + c) / Math.Sqrt(a * a + b * b);
        }

        public static PlotModel CreateModelFromTxt(string rocksFile, string temperatureFile)
        {
            //defining size of the model
            int x_size = Config.GraphConfig.Instace.XSize;
            int z_size = Config.GraphConfig.Instace.YSize;

            // Defining zoom
            bool zoom = true;

            //defining beginning and end of zoomed area
            int x_beg = Config.GraphConfig.Instace.XBegin;
            int x_end = Config.GraphConfig.Instace.XEnd;
            int z_beg = Config.GraphConfig.Instace.YBegin;
            int z_end = Config.GraphConfig.Instace.YEnd;


            //recalculating to % of the image
            double x_beg_perc, z_beg_perc;
            double x_end_perc, z_end_perc;
            x_beg_perc = (double)x_beg / (double)x_size;
            x_end_perc = (double)x_end / (double)x_size;
            z_beg_perc = (double)z_beg / (double)z_size;
            z_end_perc = (double)z_end / (double)z_size;

            //List<double> isoterms = new List<double> {300, 600, 900, 1200, 1500};
            Dictionary<double, double> isoterms = Config.GraphConfig.Instace.Isoterms.ToDictionary<double, double, double>(isoterm => isoterm, isoterm => 0);


            // fname

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

            var ColorGrid = new double[Coord_z + 1, Coord_x + 1];

            for (int i = 0; i < Coord_z; i++)
            {
                for (int j = 0; j < Coord_x; j++)
                {
                    ColorGrid[i, j] = 0;
                }
            }

            List<int[]> C_map = GraphConfig.Instace.ColorMap.Select(x => new int[] { x.R, x.G, x.B }).ToList();

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
                    // Compressed: the next ?? indices are given the color material

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

            int mt;

            int img_X;
            int img_Z;
            double grid_beg_x=0, grid_end_x=0, grid_beg_z=0, grid_end_z=0;
            if (zoom == true)
            {
                grid_beg_x = Coord_x * x_beg_perc;
                grid_end_x = Coord_x * x_end_perc;
                grid_beg_z = Coord_z * z_beg_perc;
                grid_end_z = Coord_z * z_end_perc;

                img_X = Convert.ToInt16(grid_end_x - grid_beg_x);
                img_Z = Convert.ToInt16(grid_end_z - grid_beg_z);
            }
            else
            {
                grid_beg_x = 0;
                grid_beg_z = 0;

                img_X = Coord_x;
                img_Z = Coord_z;
            }

            // Create a writeable bitmap (which is a valid WPF Image Source
            // WriteableBitmap bitmap = new WriteableBitmap(img_X, img_Z, 96, 96, PixelFormats.Bgra32, null);
            // Create an array of pixels to contain pixel color values

            //uint[] pixels = new uint[img_X * img_Z];
            OxyColor[,] pixels = new OxyColor[img_X,img_Z];

            int red;
            int green;
            int blue;
            int alpha;

            for (int x = 0; x < img_X; x++)
            {
                for (int y = 0; y < img_Z; y++)
                {
                    int i = img_X * y + x;

                    //mt = ColorGrid[x][y + (int)grid_beg_x];
                    mt = (int)ColorGrid[y, x + (int)grid_beg_x];
                    if (Math.Abs(mt) >= 36)
                    {
                        mt = 0;
                    }

                    if (mt < 0) mt = 0;

                    red = C_map[mt][0];
                    green = C_map[mt][1];
                    blue = C_map[mt][2];
                    alpha = 255;

                    if (i >= pixels.Length) continue;
                    pixels[x, y] = OxyColor.FromArgb((byte)alpha, (byte)red, (byte)green, (byte)blue); 
                }
            }

            // apply pixels to bitmap
            // bitmap.WritePixels(new Int32Rect(0, 0, img_X, img_Z), pixels, img_X * 4, 0);

            var resultModel = new PlotModel();

            var xAxe = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = grid_beg_x,
                Maximum = grid_end_x,
                Title = "X",
                AbsoluteMinimum = grid_beg_x,
                AbsoluteMaximum = grid_end_x
            };

            var yAxe = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = grid_beg_z,
                Maximum = grid_end_z,
                Title = "Z",
                AbsoluteMinimum = grid_beg_z,
                AbsoluteMaximum = grid_end_z,
                StartPosition = 1,
                EndPosition = 0
            };

            resultModel.Axes.Add(xAxe);
            resultModel.Axes.Add(yAxe);

            OxyImage image = OxyImage.Create(pixels, ImageFormat.Png);

            var mapImage = new ImageAnnotation();

            mapImage.ImageSource = image;

            mapImage.X = new PlotLength(grid_beg_x + img_X/2d, PlotLengthUnit.Data);
            mapImage.Y = new PlotLength(grid_beg_z + img_Z/2d, PlotLengthUnit.Data);
            mapImage.Width = new PlotLength(img_X, PlotLengthUnit.Data);
            mapImage.Height = new PlotLength(img_Z, PlotLengthUnit.Data);

            mapImage.Interpolate = true;

            resultModel.Annotations.Add(mapImage);

            A.Clear();
            s = 0;
            using (StreamReader sr = new StreamReader(temperatureFile))
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

            num = 3;
            ind = 0;

            while (num < s)
            {
                //value = Convert.ToDouble(A[num]);
                value = Config.Tools.ParseOrDefaultDouble(A[num]);

                jj = ind / Coord_z;
                ii = ind % Coord_z;
                ColorGrid[ii,jj] = value - 273;
                ind++;
                num++;
            }

            //var columnCoords = new double[Coord_x+1];
            //var rowCoords = new double[Coord_z+1];

            //for (int i = 0; i < Coord_x+1; i++) columnCoords[i] = i;
            //for (int i = 0; i < Coord_z+1; i++) rowCoords[i] = i;

            //resultModel.Axes.Add(new LinearColorAxis
            //{
            //    Position = AxisPosition.Right,
            //    Palette = OxyPalettes.Jet(500),
            //    HighColor = OxyColors.Gray,
            //    LowColor = OxyColors.Black
            //});

            //var contour = new ContourSeries
            //{
            //    ColumnCoordinates = rowCoords,
            //    RowCoordinates = columnCoords,
            //    ContourLevels = new double[] {300, 500, 800, 1200, 1500},
            //    Data = ColorGrid
            //};

            //resultModel.Series.Add(contour);

            //byte[] colorData = { 255, 255, 255, 255 };
            //double mt_prev;

            //for (int j = 0; j < img_X; j++)
            //{
            //    for (int i = 0; i < img_Z; i++)
            //    {
            //        int pi = img_X * i + j;

            //        mt = (int)ColorGrid[i][j + (int)grid_beg_x];
            //        mt_prev = (i > 0) ? ColorGrid[i - 1][j + (int)grid_beg_x] : mt;

            //        foreach (double isoterm in isoterms.Keys.ToList())
            //        {
            //            if (isoterm > mt_prev && isoterm <= mt)
            //            {
            //                if (j == 0) isoterms[isoterm] = i;
            //                //Int32Rect rect = new Int32Rect(j, i, 1, 1);
            //                //bitmap.WritePixels(rect, colorData, 4, 0);
            //                alpha = 255;
            //                red = 255;
            //                blue = 255;
            //                green = 255;
            //                pixels[pi] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
            //            }
            //        }
            //    }
            //}






            //BitmapSource bitmapSource = BitmapSource.Create(img_X, img_Z, 96, 96, PixelFormats.Pbgra32, null, pixels, (img_X) * 4);
            //var visual = new DrawingVisual();
            //using (DrawingContext drawingContext = visual.RenderOpen())
            //{
            //    drawingContext.DrawImage(bitmapSource, new Rect(-100, -100, img_X, img_Z));
            //    drawingContext.DrawText(
            //        new FormattedText("Время = " + (Config.Tools.ParseOrDefaultDouble(Time) / 10e6).ToString("0.00") + " млн. лет", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            //            new Typeface("Segoe UI"), 32, Brushes.Black), new Point(img_X / 2 - 230, -150));

            //    foreach (var isoterm in isoterms.Keys)
            //    {
            //        if (isoterms[isoterm] != 0) drawingContext.DrawText(
            //        new FormattedText(isoterm.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            //            new Typeface("Segoe UI"), 12, Brushes.White), new Point(-100, isoterms[isoterm] - 100));
            //    }

            //    drawingContext.DrawText(
            //        new FormattedText((x_beg / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-100, img_Z - 95));

            //    drawingContext.DrawText(
            //        new FormattedText((x_end / 1000).ToString("0.# км"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(img_X - 110, img_Z - 95));

            //    var actWidth = x_end - x_beg;
            //    if (actWidth > 150000)
            //    {
            //        for (int i = x_beg + 50000; i < x_end; i += 50000)
            //        {
            //            drawingContext.DrawText(
            //            new FormattedText((i / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(((float)(i - x_beg) / (float)actWidth) * img_X - 110, img_Z - 95));
            //        }
            //    }


            //    drawingContext.DrawText(
            //        new FormattedText((z_beg / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, -110));

            //    drawingContext.DrawText(
            //        new FormattedText((z_end / 1000).ToString("0.#") + " км", CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, img_Z - 110));

            //    var actHeight = z_end - z_beg;
            //    if (actHeight > 100000)
            //    {
            //        for (int i = z_beg + 50000; i < z_end; i += 50000)
            //        {
            //            drawingContext.DrawText(
            //            new FormattedText((i / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
            //            new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, ((float)(i - z_beg) / (float)actHeight) * img_Z - 110));
            //        }
            //    }


            //}
            //var image = new DrawingImage(visual.Drawing);
            //ResultImage.Source = image;

            A.Clear();
            return resultModel;
        }


        public static DrawingImage DrawFromTxt(string rocksFile, string temperatureFile, Dictionary<int[], List<Point>> overlayPoints = null)
        {
            //размеры модели
            int x_size = Config.GraphConfig.Instace.XSize;
            int z_size = Config.GraphConfig.Instace.YSize;

            // нужен ли зум
            bool zoom = true;

            //Отображаемая область модели
            int x_beg = Config.GraphConfig.Instace.XBegin;
            int x_end = Config.GraphConfig.Instace.XEnd;
            int z_beg = Config.GraphConfig.Instace.YBegin;
            int z_end = Config.GraphConfig.Instace.YEnd;


            //Отображаемая область модели в процентах от общего размера
            double x_beg_perc, z_beg_perc;
            double x_end_perc, z_end_perc;
            x_beg_perc = (double)x_beg / (double)x_size;
            x_end_perc = (double)x_end / (double)x_size;
            z_beg_perc = (double)z_beg / (double)z_size;
            z_end_perc = (double)z_end / (double)z_size;

            // задаваемые изотермы
            Dictionary<double, double> isoterms = Config.GraphConfig.Instace.Isoterms.ToDictionary<double, double, double>(isoterm => isoterm, isoterm => 0);

            #region Считывание и отрисовка состава

            List<string> A = new List<string>();
            long s = 0;

            // Считываем из файла ID пород
            using (StreamReader sr = new StreamReader(rocksFile))
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

            string time = A[0]; // Модельное время
            int coordX = Convert.ToInt16(A[1]); // разрешение модели по X
            int coordZ = Convert.ToInt16(A[2]); // разрешение модели по Y

            var valueGrid = new List<List<double>>(coordX * coordZ); // массив из ID пород
            for (int i = 0; i <= coordZ; i++)
            {
                valueGrid.Add(new List<double>());
                for (int j = 0; j <= coordX; j++)
                {
                    valueGrid[i].Add(0);
                }
            }

            List<int[]> cMap = GraphConfig.Instace.ColorMap.Select(x => new int[] { x.R, x.G, x.B }).ToList(); // массив цветов пород (его порядковый индекс - ID породы) 
            if (overlayPoints != null) cMap.AddRange(overlayPoints.Keys);

            // заполнение массива номерами (ID) пород 

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
                    jj = n / coordZ;
                    ii = n % coordZ;
                    valueGrid[ii][jj] = material;
                }

            }

            if (overlayPoints != null)
            {
                var kX = Convert.ToDouble(x_size)/coordX ;
                var kZ = Convert.ToDouble(z_size)/coordZ ;

                foreach (var rgbArr in overlayPoints.Keys)
                {
                    var oind = cMap.IndexOf(rgbArr);
                    foreach (var pt in overlayPoints[rgbArr])
                    {
                        var ox = Convert.ToInt32( pt.X/kX );
                        var oz = Convert.ToInt32( pt.Y/kZ );

                        valueGrid[oz][ox] = oind;
                    }
                }
            }


            int mt;

            int imgX;
            int imgZ;
            double gridBegX, gridEndX, gridBegZ, gridEndZ;
            if (zoom)
            {
                gridBegX = coordX * x_beg_perc;
                gridEndX = coordX * x_end_perc;
                gridBegZ = coordZ * z_beg_perc;
                gridEndZ = coordZ * z_end_perc;

                imgX = Convert.ToInt16(gridEndX - gridBegX);
                imgZ = Convert.ToInt16(gridEndZ - gridBegZ);
            }
            else
            {
                gridBegX = 0;
                gridBegZ = 0;

                imgX = coordX;
                imgZ = coordZ;
            }


            uint[] pixels = new uint[imgX * imgZ]; //массив для хранения цветов всех пикселей

            int red;
            int green;
            int blue;
            int alpha;

            //заполнение пиксельного массива значениями в соответствии с cMap и номером пород
            for (int x = 0; x < imgX; x++)
            {
                for (int y = 0; y < imgZ; y++)
                {
                    int i = imgX * y + x;

                    mt = (int)valueGrid[y + (int)gridBegZ][x + (int)gridBegX];
                    if (Math.Abs(mt) >= cMap.Count )
                    {
                        mt = 0;
                    }

                    if (mt < 0) mt = 0;

                    red = cMap[mt][0];
                    green = cMap[mt][1];
                    blue = cMap[mt][2];
                    alpha = 255;

                    if (i >= pixels.Length) continue;
                    pixels[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                }
            }

            #endregion

            #region Считывание температуры и отрисовка изолиний

            A.Clear();
            s = 0;
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



            var ktX = coordX / Convert.ToDouble(A[1]);
            var ktZ = coordZ / Convert.ToDouble(A[2]);

            num = 3;
            ind = 0;

            // Перезаполнение valueGrid значениями температур
            while (num < s)
            {
                //value = Convert.ToDouble(A[num]);
                value = Config.Tools.ParseOrDefaultDouble(A[num]);

                jj = ind / Convert.ToInt32(A[2]);
                ii = ind % Convert.ToInt32(A[2]);

                //todo переделать учет возможной разницы разрешений температур и состава
                for (int i = Convert.ToInt32(ii * ktZ); i < (ii + 1) * ktZ; i++)
                {
                    for (int j = Convert.ToInt32(jj * ktX); j < (jj + 1) * ktX; j++)
                    {
                        valueGrid[i][j] = value - 273;
                    }
                }

                ind++;
                num++;
            }

            double tc, tc_prev_v, tc_prev_h;

            // задаем параметры цвета изолиний (белый)
            alpha = 255;
            red = 255;
            blue = 255;
            green = 255;

            // находим пиксели, где нужно рисовать соответствующие изолинии
            for (int j = 0; j < imgX; j++)
            {
                for (int i = 0; i < imgZ; i++)
                {
                    int pi = imgX * i + j; //индекс пикселя массива pixels текущей точки valueGrid

                    tc = valueGrid[i + (int)gridBegZ][j + (int)gridBegX]; //текущая температура
                    tc_prev_v = (i > 0) ? valueGrid[i + (int)gridBegZ - 1][j + (int)gridBegX] : tc; //температура в соседней верхней точке
                    tc_prev_h = (j > 0) ? valueGrid[i + (int)gridBegZ][j + (int)gridBegX - 1] : tc; //температура в соседней левой точке

                    // цикл по заданным значениям изолиний (изотерм)
                    foreach (double isoterm in isoterms.Keys.ToList())
                    {
                        // если значением текущей изотермы лежит в пределах текущей и соседней температуы, ...
                        if ((isoterm > tc_prev_v && isoterm <= tc) ||
                             (isoterm < tc_prev_v && isoterm >= tc) ||
                             (isoterm > tc_prev_h && isoterm <= tc) ||
                             (isoterm < tc_prev_h && isoterm >= tc))
                        {
                            // то закрашиваем текущий пиксель белым цветом 
                            pixels[pi] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                            if (j == 0) isoterms[isoterm] = i; // если это самая крайняя левая точка, то запоминаем её высоту (i), чтобы отобразить на ней подпись
                        }

                    }
                }
            }

            #endregion

            // сохранение пискельного массива в рисунок и отрисовка подписей

            BitmapSource bitmapSource = BitmapSource.Create(imgX, imgZ, 96, 96, PixelFormats.Pbgra32, null, pixels, (imgX) * 4);
            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(bitmapSource, new Rect(-100, -100, imgX, imgZ));
                drawingContext.DrawText(
                    new FormattedText("Время = " + (Config.Tools.ParseOrDefaultDouble(time) / 1e6).ToString("0.00") + " млн. лет", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 32, Brushes.Black), new Point(imgX / 2 - 230, -150));

                foreach (var isoterm in isoterms.Keys)
                {
                    if (isoterms[isoterm] != 0) drawingContext.DrawText(
                    new FormattedText(isoterm.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.White), new Point(-100, isoterms[isoterm] - 100));
                }

                drawingContext.DrawText(
                    new FormattedText((x_beg / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-100, imgZ - 95));

                drawingContext.DrawText(
                    new FormattedText((x_end / 1000).ToString("0.# км"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(imgX - 110, imgZ - 95));

                var actWidth = x_end - x_beg;
                if (actWidth > 150000)
                {
                    for (int i = x_beg + 50000; i < x_end; i += 50000)
                    {
                        drawingContext.DrawText(
                        new FormattedText((i / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(((float)(i - x_beg) / (float)actWidth) * imgX - 110, imgZ - 95));
                    }
                }


                drawingContext.DrawText(
                    new FormattedText((z_beg / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, -110));

                drawingContext.DrawText(
                    new FormattedText((z_end / 1000).ToString("0.#") + " км", CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, imgZ - 110));

                var actHeight = z_end - z_beg;
                if (actHeight > 100000)
                {
                    for (int i = z_beg + 50000; i < z_end; i += 50000)
                    {
                        drawingContext.DrawText(
                        new FormattedText((i / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.RightToLeft,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-105, ((float)(i - z_beg) / (float)actHeight) * imgZ - 110));
                    }
                }


            }
            var image = new DrawingImage(visual.Drawing);
            //ResultImage.Source = image;

            A.Clear();
            return image;
        }

        public static DrawingImage DrawFromPrn(string filePath, string par, IntRectangle drawArea)
        {
            var br = new BinaryReader(new FileStream(filePath, FileMode.Open));

            var A = br.ReadBytes(4);
            var xnumx = br.ReadInt64();
            var ynumy = br.ReadInt64();
            var mnumx = br.ReadInt64();
            var mnumy = br.ReadInt64();
            var marknum = br.ReadInt64();
            var xsize = br.ReadDouble();
            var ysize = br.ReadDouble();
            var pinit = new double[5] { br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble(), br.ReadDouble() };
            var gxkoef = br.ReadDouble();
            var gykoef = br.ReadDouble();
            var rocknum = br.ReadInt32();
            var bondnum = br.ReadInt64();
            var n1 = br.ReadInt32();
            var timesum = br.ReadDouble();
            //curpos0=4+2*4+16*8+rocknum*(8*24+4);
            //fseek(fdata,curpos0,'bof');
            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);
            br.BaseStream.Position = curpos0;

            var pr = new float[ynumy, xnumx];
            var vx = new float[ynumy, xnumx];
            var vy = new float[ynumy, xnumx];
            var exx = new float[ynumy, xnumx];
            var eyy = new float[ynumy, xnumx];
            var exy = new float[ynumy, xnumx];
            var sxx = new float[ynumy, xnumx];
            var syy = new float[ynumy, xnumx];
            var sxy = new float[ynumy, xnumx];
            var ro = new float[ynumy, xnumx];
            var nu = new float[ynumy, xnumx];
            var nd = new float[ynumy, xnumx];
            var mu = new float[ynumy, xnumx];
            var ep = new float[ynumy, xnumx];
            var et = new float[ynumy, xnumx];
            var pr0 = new float[ynumy, xnumx];
            var prb = new float[ynumy, xnumx];
            var dv = new float[ynumy, xnumx];
            var tk = new float[ynumy, xnumx];
            var cp = new float[ynumy, xnumx];
            var kt = new float[ynumy, xnumx];
            var ht = new float[ynumy, xnumx];

            for (int i = 0; i < xnumx; i++)
            {
                for (int j = 0; j < ynumy; j++)
                {
                    var vbuf = new float[3] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                    pr[j, i] = vbuf[0];
                    vx[j, i] = vbuf[1];
                    vy[j, i] = vbuf[2];
                    //var vbuf1 = new long[3] { br.ReadInt64(), br.ReadInt64(), br.ReadInt64() };
                    var te1 = br.ReadInt64();
                    var te2 = br.ReadInt64();
                    var te3 = br.ReadInt64();
                    var vbuf2 = new float[16] 
                    { br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), 
                        br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), 
                        br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), 
                        br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle()};

                    exx[j, i] = vbuf2[0];
                    eyy[j, i] = vbuf2[1];
                    exy[j, i] = vbuf2[2];
                    sxx[j, i] = vbuf2[3];
                    syy[j, i] = vbuf2[4];
                    sxy[j, i] = vbuf2[5];
                    ro[j, i] = vbuf2[6];
                    nu[j, i] = vbuf2[7];
                    nd[j, i] = vbuf2[8];
                    mu[j, i] = vbuf2[9];
                    ep[j, i] = vbuf2[10];
                    et[j, i] = vbuf2[11];
                    pr0[j, i] = vbuf2[12];
                    prb[j, i] = vbuf2[13];
                    dv[j, i] = vbuf2[14];
                    tk[j, i] = vbuf2[15];
                    var te4 = br.ReadInt64();
                    var vbuf4 = new float[3] { br.ReadSingle(), br.ReadSingle(), br.ReadSingle() };
                    cp[j, i] = vbuf4[0];
                    kt[j, i] = vbuf4[1];
                    ht[j, i] = vbuf4[2];

                }
            }

            br.BaseStream.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++) gx[i] = br.ReadSingle();

            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++) gy[i] = br.ReadSingle();

            var eii = new double[ynumy, xnumx];
            var sii = new double[ynumy, xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                for (int j = 0; j < ynumy; j++)
                {
                    eii[j, i] = 1e-16;
                    sii[j, i] = 1e+4;
                }
            }

            //TODO сверить с матлабом
            for (int i = 0; i < xnumx - 2; i++)
            {
                for (int j = 0; j < ynumy - 2; j++)
                {
                    eii[j + 1, i + 1] = Math.Sqrt(Math.Pow(exx[j + 1, i + 1], 2) + Math.Pow((exx[j + 1, i + 1] + exx[j + 2, i + 1] + exx[j + 1, i + 2] + exx[j + 2, i + 2]) / 4, 2));
                    sii[j + 1, i + 1] = Math.Sqrt(Math.Pow(sxx[j + 1, i + 1], 2) + Math.Pow((sxx[j + 1, i + 1] + sxx[j + 2, i + 1] + sxx[j + 1, i + 2] + sxx[j + 2, i + 2]) / 4, 2));
                }
            }

            var selectedPar = new float[ynumy, xnumx];

            switch (par)
            {
                case "pr":
                    selectedPar = pr;
                    break;
                case "vx":
                    selectedPar = vx;
                    break;
                case "vy":
                    selectedPar = vy;
                    break;
                case "exx":
                    selectedPar = exx;
                    break;
                case "eyy":
                    selectedPar = eyy;
                    break;
                case "exy":
                    selectedPar = exy;
                    break;
                case "sxx":
                    selectedPar = sxx;
                    break;
                case "syy":
                    selectedPar = syy;
                    break;
                case "sxy":
                    selectedPar = sxy;
                    break;
                case "ro":
                    selectedPar = ro;
                    break;
                case "nu":
                    selectedPar = nu;
                    break;
                case "nd":
                    selectedPar = nd;
                    break;
                case "mu":
                    selectedPar = mu;
                    break;
                case "ep":
                    selectedPar = ep;
                    break;
                case "pr0":
                    selectedPar = pr0;
                    break;
                case "prb":
                    selectedPar = prb;
                    break;
                case "dv":
                    selectedPar = dv;
                    break;
                case "tk":
                    selectedPar = tk;
                    break;
                case "cp":
                    selectedPar = cp;
                    break;
                case "kt":
                    selectedPar = kt;
                    break;
                case "ht":
                    selectedPar = ht;
                    break;
                default:
                    return null;
                    break;
            }


            //var x_grid_beg = Convert.ToInt64(((drawArea.X) / xsize) * xnumx);
            //var x_grid_end = Convert.ToInt64(((drawArea.X + drawArea.Width) / xsize) * xnumx);
            //var y_grid_beg = Convert.ToInt64((drawArea.Y / ysize) * ynumy);
            //var y_grid_end = Convert.ToInt64(((drawArea.Y + drawArea.Height) / ysize) * ynumy);

            var x_grid_beg = Array.FindIndex(gx, x => x >= drawArea.X);
            var x_grid_end = Array.FindIndex(gx, x => x >= drawArea.X + drawArea.Width);
            var y_grid_beg = Array.FindIndex(gy, y => y >= drawArea.Y);
            var y_grid_end = Array.FindIndex(gy, y => y >= drawArea.Y + drawArea.Height);

            if (x_grid_beg < 0) x_grid_beg = 0;
            if (x_grid_end > xnumx) x_grid_beg = (int)(xnumx - 1);
            if (y_grid_beg < 0) y_grid_beg = 0;
            if (y_grid_beg > ynumy) y_grid_beg = (int)(ynumy - 1);

            var drawingGridPart = new IntRectangle((uint)x_grid_beg, (uint)y_grid_beg, (uint)(x_grid_end - x_grid_beg), (uint)(y_grid_end - y_grid_beg));

            var img = DrawImageFromArray(selectedPar, drawingGridPart, "Время = " + (timesum / 1e6).ToString("0.00") + " млн. лет", gx, gy);
            br.Close();
            br.Dispose();
            return img;

        }


        private static DrawingImage DrawImageFromArray(float[,] valuesGrid, string header = "", float[] gx = null, float[] gy = null)
        {
            var drawArea = new IntRectangle(0, 0, (uint)valuesGrid.GetUpperBound(1), (uint)valuesGrid.GetUpperBound(0));
            return DrawImageFromArray(valuesGrid, drawArea, header, gx, gy);
        }

        private static DrawingImage DrawImageFromArray(float[,] valuesGrid, IntRectangle drawArea, string header = "", float[] gx = null, float[] gy = null)
        {

            double minVal = double.MaxValue;
            double maxVal = double.MinValue;

            for (int i = 0; i < valuesGrid.GetUpperBound(0); i++)
            {
                for (int j = 0; j < valuesGrid.GetUpperBound(1); j++)
                {
                    if (valuesGrid[i, j] < minVal) minVal = valuesGrid[i, j];
                    if (valuesGrid[i, j] > maxVal) maxVal = valuesGrid[i, j];
                }
            }

            return DrawImageFromArray(valuesGrid, minVal, maxVal, drawArea, header, gx, gy);

        }

        private static DrawingImage DrawImageFromArray(float[,] valuesGrid, double minVal, double maxVal, IntRectangle drawArea, string header = "", float[] gx = null, float[] gy = null)
        {

            if (valuesGrid == null) return null;
            if (valuesGrid.Length == 0) return null;

            int red;
            int green;
            int blue;
            int alpha;

            int img_X, img_Z;

            int gridXSize = valuesGrid.GetUpperBound(1);
            int gridZSize = valuesGrid.GetUpperBound(0);

            int beg_x;
            int beg_z;

            if (drawArea == null)
            {
                img_Z = gridZSize;
                img_X = gridXSize;
                beg_x = 0;
                beg_z = 0;
            }
            else
            {
                img_X = (drawArea.X + drawArea.Width <= gridXSize) ? (int)drawArea.Width : (int)(gridXSize - drawArea.X);
                img_Z = (drawArea.Y + drawArea.Height <= gridZSize) ? (int)drawArea.Height : (int)(gridZSize - drawArea.Y);
                beg_x = (int)drawArea.X;
                beg_z = (int)drawArea.Y;
            }

            uint[] pixels = new uint[img_X * img_Z];

            double curValue;

            for (int x = 0; x < img_X; x++)
            {
                for (int y = 0; y < img_Z; y++)
                {
                    int i = img_X * y + x;

                    curValue = valuesGrid[y, x + beg_x];

                    var curColor = Config.Tools.ColorFromValue(curValue, minVal, maxVal);

                    red = curColor.R;
                    green = curColor.G;
                    blue = curColor.B;
                    alpha = 255;

                    if (i >= pixels.Length) continue;
                    pixels[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                }
            }

            // рисуем шкалу
            uint[] legendPixels = new uint[30 * img_Z];
            double curLegVal = minVal;

            for (int x = 0; x < 30; x++)
            {
                for (int y = 0; y < img_Z; y++)
                {
                    int i = 30 * y + x;
                    curLegVal = y * ((maxVal - minVal) / img_Z) + minVal;
                    var curColor = Config.Tools.ColorFromValue(curLegVal, minVal, maxVal);

                    red = curColor.R;
                    green = curColor.G;
                    blue = curColor.B;
                    alpha = 255;

                    if (i >= legendPixels.Length) continue;
                    legendPixels[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                }

            }

            BitmapSource bitmapSource = BitmapSource.Create(img_X, img_Z, 96, 96, PixelFormats.Pbgra32, null, pixels, (img_X) * 4);
            BitmapSource legendSource = BitmapSource.Create(30, img_Z, 96, 96, PixelFormats.Pbgra32, null, legendPixels, (30) * 4);

            double beg_len, end_len, beg_dep, end_dep;

            // TODO сделать правильные подписи расстояний
            if (drawArea == null)
            {
                if (gx != null && gy != null)
                {
                    beg_len = gx[0] / 1000;
                    beg_dep = gy[0] / 1000;
                    end_len = gx.Last() / 1000;
                    end_dep = gy.Last() / 1000;
                }
                else
                {
                    beg_len = valuesGrid.GetLowerBound(1);
                    beg_dep = valuesGrid.GetLowerBound(0);
                    end_len = valuesGrid.GetUpperBound(1);
                    end_dep = valuesGrid.GetUpperBound(0);
                }
            }
            else
            {
                if (gx != null && gy != null)
                {
                    beg_len = gx[drawArea.X] / 1000;
                    beg_dep = gy[drawArea.Y] / 1000;
                    end_len = gx[drawArea.X + drawArea.Width] / 1000;
                    end_dep = gy[drawArea.Y + drawArea.Height] / 1000;
                }
                else
                {
                    beg_len = drawArea.X;
                    beg_dep = drawArea.Y;
                    end_len = drawArea.X + drawArea.Width;
                    end_dep = drawArea.Y + drawArea.Height;
                }

            }

            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, img_X, img_Z));

                if (!String.IsNullOrWhiteSpace(header)) drawingContext.DrawText(
                    new FormattedText(header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 32, Brushes.Black), new Point(img_X / 2 - 230, -50));

                drawingContext.DrawImage(legendSource, new Rect(img_X + 20, 0, 30, img_Z));

                drawingContext.DrawText(
                    new FormattedText((minVal > 10e5) ? minVal.ToString("0.00E+00") : minVal.ToString("0.###"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(img_X + 55, -10));
                drawingContext.DrawText(
                    new FormattedText(((maxVal - minVal) / 2 > 10e5) ? ((maxVal - minVal) / 2).ToString("0.00E+00") : ((maxVal - minVal) / 2).ToString("0.###"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(img_X + 55, img_Z / 2 - 10));
                drawingContext.DrawText(
                    new FormattedText((maxVal > 10e5) ? maxVal.ToString("0.00E+00") : maxVal.ToString("0.###"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(img_X + 55, img_Z - 10));

                drawingContext.DrawText(
                    new FormattedText(beg_len.ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(0, img_Z + 3));
                drawingContext.DrawText(
                    new FormattedText(end_len.ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(img_X - 20, img_Z + 3));


                if (gx != null && drawArea != null && drawArea.Width > 100)
                {
                    var barNum = drawArea.Width / 50;
                    for (int i = 1; i < barNum; i++)
                    {
                        drawingContext.DrawText(
                        new FormattedText((gx[drawArea.X + 50 * i - 1] / 1000).ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(50 * i - 10, img_Z + 3));
                    }
                }


                drawingContext.DrawText(
                    new FormattedText(beg_dep.ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-30, 0));
                drawingContext.DrawText(
                    new FormattedText(end_dep.ToString("0.#"), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.Black), new Point(-30, img_Z - 20));

            }
            var image = new DrawingImage(visual.Drawing);
            return image;


        }


        public static void SaveDrawingToFile(Drawing drawing, string fileName, double scale)
        {
            var drawingImage = new Image();
            drawingImage.Source = new DrawingImage(drawing);
            //drawingImage.Source.Freeze();
            var width = drawing.Bounds.Width * scale;
            var height = drawing.Bounds.Height * scale;
            drawingImage.Arrange(new Rect(0, 0, width, height));

            var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingImage);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        public static MemoryStream SaveDrawingToStream(Drawing drawing, double scale)
        {
            var drawingImage = new Image();
            drawingImage.Source = new DrawingImage(drawing);
            //drawingImage.Source.Freeze();
            var width = drawing.Bounds.Width * scale;
            var height = drawing.Bounds.Height * scale;
            drawingImage.Arrange(new Rect(0, 0, width, height));

            var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(drawingImage);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            //using (var stream = new FileStream(fileName, FileMode.Create))
            //{
            //    encoder.Save(stream);
            //}

            var stream = new MemoryStream();
            encoder.Save(stream);

            return stream;


        }

        public static MemoryStream StreamFromTemperature(string temperatureFile, int isotermCross = 0)
        {

            //размеры модели
            int x_size = Config.GraphConfig.Instace.XSize;
            int z_size = Config.GraphConfig.Instace.YSize;

            // нужен ли зум
            bool zoom = true;

            //Отображаемая область модели
            //int x_beg = Config.GraphConfig.Instace.XBegin;
            //int x_end = Config.GraphConfig.Instace.XEnd;
            //int z_beg = Config.GraphConfig.Instace.YBegin;
            //int z_end = Config.GraphConfig.Instace.YEnd;

            int x_beg = 0;
            int x_end = 4000;
            int z_beg = 0;
            int z_end = 400;


            //Отображаемая область модели в процентах от общего размера
            double x_beg_perc, z_beg_perc;
            double x_end_perc, z_end_perc;
            x_beg_perc = (double)x_beg / (double)x_size;
            x_end_perc = (double)x_end / (double)x_size;
            z_beg_perc = (double)z_beg / (double)z_size;
            z_end_perc = (double)z_end / (double)z_size;

            // задаваемые изотермы
            Dictionary<double, double> isoterms = Config.GraphConfig.Instace.Isoterms.ToDictionary<double, double, double>(isoterm => isoterm, isoterm => 0);

            
            List<string> A = new List<string>();
            long s = 0;

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

            string time = A[0]; // Модельное время
            int coordX = Convert.ToInt16(A[1]); // разрешение модели по X
            int coordZ = Convert.ToInt16(A[2]); // разрешение модели по Y

            var valueGrid = new List<List<double>>(coordX * coordZ); // массив из ID пород
            for (int i = 0; i <= coordZ; i++)
            {
                valueGrid.Add(new List<double>());
                for (int j = 0; j <= coordX; j++)
                {
                    valueGrid[i].Add(0);
                }
            }


            // заполнение массива номерами (ID) пород 

            int num = 3;
            int ind = 1;

            double value;
            int num_colors;
            int material;
            int ind_vec1, ind_vec2;

            int jj, ii;

            int mt;

            int imgX;
            int imgZ;
            double gridBegX, gridEndX, gridBegZ, gridEndZ;
            
                gridBegX = coordX * x_beg_perc;
                gridEndX = coordX * x_end_perc;
                gridBegZ = coordZ * z_beg_perc;
                gridEndZ = coordZ * z_end_perc;

                imgX = Convert.ToInt16(gridEndX - gridBegX);
                imgZ = Convert.ToInt16(gridEndZ - gridBegZ);
            

            uint[] pixels = new uint[imgX * imgZ]; //массив для хранения цветов всех пикселей

            int red;
            int green;
            int blue;
            int alpha;

            var ktX = coordX / Convert.ToDouble(A[1]);
            var ktZ = coordZ / Convert.ToDouble(A[2]);

            num = 3;
            ind = 0;

            // Перезаполнение valueGrid значениями температур
            while (num < s)
            {
                //value = Convert.ToDouble(A[num]);
                value = Config.Tools.ParseOrDefaultDouble(A[num]);

                jj = ind / Convert.ToInt32(A[2]);
                ii = ind % Convert.ToInt32(A[2]);

                //todo переделать учет возможной разницы разрешений температур и состава
                for (int i = Convert.ToInt32(ii * ktZ); i < (ii + 1) * ktZ; i++)
                {
                    for (int j = Convert.ToInt32(jj * ktX); j < (jj + 1) * ktX; j++)
                    {
                        valueGrid[i][j] = value - 273;
                    }
                }

                ind++;
                num++;
            }

            double tc, tc_prev_v, tc_prev_h;

            // задаем параметры цвета изолиний (белый)
            alpha = 255;
            red = 255;
            blue = 255;
            green = 255;

            // находим пиксели, где нужно рисовать соответствующие изолинии
            for (int j = 0; j < imgX; j++)
            {
                for (int i = 0; i < imgZ; i++)
                {
                    int pi = imgX * i + j; //индекс пикселя массива pixels текущей точки valueGrid

                    tc = valueGrid[i + (int)gridBegZ][j + (int)gridBegX]; //текущая температура
                    tc_prev_v = (i > 0) ? valueGrid[i + (int)gridBegZ - 1][j + (int)gridBegX] : tc; //температура в соседней верхней точке
                    tc_prev_h = (j > 0) ? valueGrid[i + (int)gridBegZ][j + (int)gridBegX - 1] : tc; //температура в соседней левой точке

                    // цикл по заданным значениям изолиний (изотерм)
                    foreach (double isoterm in isoterms.Keys.ToList())
                    {
                        // если значением текущей изотермы лежит в пределах текущей и соседней температуы, ...
                        if ((isoterm > tc_prev_v && isoterm <= tc) ||
                             (isoterm < tc_prev_v && isoterm >= tc) ||
                             (isoterm > tc_prev_h && isoterm <= tc) ||
                             (isoterm < tc_prev_h && isoterm >= tc))
                        {
                            // то закрашиваем текущий пиксель белым цветом 
                            pixels[pi] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                            if (j == isotermCross) isoterms[isoterm] = i; // если это самая крайняя левая точка, то запоминаем её высоту (i), чтобы отобразить на ней подпись
                        }

                    }
                }
            }


            // сохранение пискельного массива в рисунок и отрисовка подписей

            BitmapSource bitmapSource = BitmapSource.Create(imgX, imgZ, 96, 96, PixelFormats.Pbgra32, null, pixels, (imgX) * 4);
            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, imgX, imgZ));
                
                foreach (var isoterm in isoterms.Keys)
                {
                    if (isoterms[isoterm] != 0) drawingContext.DrawText(
                    new FormattedText(isoterm.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"), 12, Brushes.White), new Point(0, isoterms[isoterm]));
                }

                
            }

            var image = new DrawingImage(visual.Drawing);

            return SaveDrawingToStream(image.Drawing, 1);

        }
    }
}
