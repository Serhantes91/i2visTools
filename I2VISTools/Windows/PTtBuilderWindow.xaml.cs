using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using I2VISTools.Config;
using I2VISTools.ModelClasses;
using I2VISTools.Subclasses;
using I2VISTools.Tools;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using FlowDirection = System.Windows.FlowDirection;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Path = System.IO.Path;
using TextBox = System.Windows.Controls.TextBox;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for PTtBuilderWindow.xaml
    /// </summary>
    public partial class PTtBuilderWindow : Window
    {

        List<ModPoint> _selectedPoints = new List<ModPoint>();

        List<uint> _selectedIndexes = new List<uint>();

        private string selectedFile;


        private bool _forArea = false;
        private bool _isDragging = false;
        private RectangleAnnotation _currentRectangle;

        public PTtBuilderWindow()
        {
            InitializeComponent();
            MarkersList.ItemsSource = _selectedPoints;

            _currentRectangle = new RectangleAnnotation();
            _currentRectangle.Fill = OxyColor.FromArgb(128, 0, 0, 0);
            
        }

        private FolderBrowserDialog fbd;
        private List<string> files;

        private void LoadPrnButton_Click(object sender, RoutedEventArgs e)
        {
            //fbd = new FolderBrowserDialog();
            //fbd.SelectedPath = @"E:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500";
            //fbd.ShowNewFolderButton = false;

            //DialogResult result = fbd.ShowDialog();
            //if (result != System.Windows.Forms.DialogResult.OK) return;

            //files = Directory.GetFiles(fbd.SelectedPath).ToList().Where(x => x.EndsWith(".prn")).ToList();

            //if (files.Count == 0)
            //{
            //    MessageBox.Show("В указаном каталоге нет подходящих файлов!");
            //    return;
            //}

            var ofd = new OpenFileDialog();
            //ofd.InitialDirectory = @"E:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500\";
            //ofd.InitialDirectory = @"E:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500\";
            
            ofd.Multiselect = true;
            ofd.Filter = @"PRN-файлы|*.prn";

            ofd.ShowDialog();

            files = ofd.FileNames.ToList();

            PrnFilesListBox.ItemsSource = files;

        }

        double[,] arr;

        private int imgX, imgZ;
        private int stX, endX, stZ, endZ;

        private double ltX, ltY, rbX, rbY;


        private void LoadTxtButton_Click(object sender, RoutedEventArgs e)
        {
            //fbd = new FolderBrowserDialog();
            //fbd.SelectedPath = @"E:\Sergey\offsite\results\inversed";
            //fbd.ShowNewFolderButton = false;

            

            //DialogResult result = fbd.ShowDialog();
            //if (result != System.Windows.Forms.DialogResult.OK) return;

            //files = Directory.GetFiles(fbd.SelectedPath).ToList().Where(x => x.Contains(".txt")).ToList();

            //if (files.Count == 0)
            //{
            //    MessageBox.Show("В указаном каталоге нет подходящих файлов!");
            //    return;
            //}

            var ofd = new OpenFileDialog
            {
                Filter = @"Текстовый файл|*.txt",
                Title = @"Выберите файл, соответствующий " + selectedFile
            };
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK ) return;

            var curFile = ofd.FileName;

            List<int[]> C_map = GraphConfig.Instace.ColorMap.Select(x => new int[] { x.R, x.G, x.B }).ToList();

            arr = I2VISOutputReader.GetRocksArrayFromTxt(curFile);

            stX = Config.Tools.ParseOrDefaultInt(StXTb.Text, 0);
            endX = Config.Tools.ParseOrDefaultInt(EndXTb.Text, arr.GetLength(1));
            stZ = Config.Tools.ParseOrDefaultInt(StZTb.Text, 0);
            endZ = Config.Tools.ParseOrDefaultInt(EndZTb.Text, arr.GetLength(0));

            if (stX < 0) stX = 0;
            if (endX > arr.GetLength(1) || endX < stX) endX = arr.GetLength(1);
            if (stZ < 0) stZ = 0;
            if (endZ > arr.GetLength(0) || endZ < stZ) endZ = arr.GetLength(0);

            imgX = endX - stX;
            imgZ = endZ - stZ;

            //var pxls = PixelsGraph.GetPixelsArray(arr, C_map);

            //var pxls = PixelsGraph.GetPixelsArray(arr, C_map, stX, stZ, endX, endZ);
            
            //BitmapSource bitmapSource = BitmapSource.Create(imgX, imgZ, 96, 96, PixelFormats.Pbgra32, null, pxls, (imgX) * 4);

            //var visual = new DrawingVisual();
            //using (DrawingContext drawingContext = visual.RenderOpen())
            //{
            //    drawingContext.DrawImage(bitmapSource, new Rect(0, 0, imgX, imgZ));

            //}

            //InputImage.Source = new DrawingImage(visual.Drawing);

            var model = new PlotModel();
            

            var xAxe = new LinearAxis
            {
                Minimum = stX,
                Maximum = endX,
                AbsoluteMinimum = stX,
                AbsoluteMaximum = endX,
                Position = AxisPosition.Bottom
            };

            var yAxe = new LinearAxis
            {
                Minimum = stZ,
                Maximum = endZ,
                AbsoluteMinimum = stZ,
                AbsoluteMaximum = endZ,
                Position = AxisPosition.Left,
                StartPosition = 1,
                EndPosition = 0
            };


            List<OxyColor> colors = GraphConfig.Instace.ColorMap;

            var cAxe = new LinearColorAxis
            {
                Palette = new OxyPalette(colors),
                HighColor = colors.Last(),
                LowColor = colors[0],
                AbsoluteMinimum = 0,
                AbsoluteMaximum = colors.Count - 1,
                Minimum = 0,
                Maximum = colors.Count - 1,
                MajorStep = 1
            };

            model.Axes.Add(xAxe);
            model.Axes.Add(yAxe);
            model.Axes.Add(cAxe);



            var hs = new HeatMapSeries
            {
                X0 = stX,
                X1 = endX,
                Y0 = stZ,
                Y1 = endZ
            };

            hs.Data = new double[endX - stX + 1, endZ - stZ + 1];

            //int ii = tarY2 - tarY1; 
            int ii = 0;

            for (int i = stZ; i < endZ; i++)
            {
                int jj = 0;
                for (int j = stX; j < endX; j++)
                {

                    hs.Data[jj, ii] = arr[i , j];
                    //        model.Annotations.Add(new PointAnnotation() {X = j, Y = i, Fill = colors[(int)arr[i,j]], Size = 6} );
                    jj++;
                }
                ii++;
            }

            model.Series.Add(hs);

            plotView.Model = model;

            xAxe.Zoom(yAxe.Scale);

            model.InvalidatePlot(false);


            model.MouseDown += (sen, ev) =>
            {
                if (ev.ChangedButton == OxyMouseButton.Left)
                {
                    var pt = xAxe.InverseTransform(ev.Position.X, ev.Position.Y, yAxe);

                    if (_forArea)
                    {
                        if (!model.Annotations.Contains(_currentRectangle)) model.Annotations.Add(_currentRectangle);
                        _currentRectangle.MinimumX = pt.X;
                        _currentRectangle.MinimumY = pt.Y;
                        _currentRectangle.MaximumX = pt.X + 1;
                        _currentRectangle.MaximumY = pt.Y + 1;
                        _isDragging = true;
                        return;
                    }

                    var selected_pt = new ModPoint(pt.X, pt.Y)
                    {
                        Type = (int)(arr[Convert.ToInt32(pt.Y), Convert.ToInt32(pt.X)]),
                        Name = selectedFile
                    };
                    _selectedPoints.Add(selected_pt);
                    MarkersList.Items.Refresh();
                }
            };

            model.MouseMove += (sen, ev) =>
            {
                if (!_isDragging) return;

                var pt = xAxe.InverseTransform(ev.Position.X, ev.Position.Y, yAxe);
                _currentRectangle.MaximumX = pt.X;
                _currentRectangle.MaximumY = pt.Y;
                model.InvalidatePlot(false);
            };

            model.MouseUp += (sen, ev) =>
            {
                if (_isDragging) _currentRectangle.Tag = "done";
                _isDragging = false;
                //model.Annotations.Remove(_currentRectangle);
                
            };

            TipLabel.Visibility = Visibility.Visible;

        }

        


        private void PttTrackButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (files == null || files.Count == 0)
            {
                MessageBox.Show("Вы не загрузили prn-файлы!");
                return;
            }

            if (plotView.Model == null)
            {
                MessageBox.Show("Вы не загрузили txt!");
                return;
            }

            if (IndexRb.IsChecked == true)
            {
                if (_selectedIndexes.Count <=0) return;

                var allMarkers = files.OrderBy(Config.Tools.ExtractNumberFromString).Select(file => GetMarkersFromPrn(file, _selectedIndexes)).ToList();

                var labels = allMarkers.Select(sets => sets[0].Age.ToString("0.00E+0")).ToList();

                var markersDic = CategorizeMarkers(allMarkers);


                var pttWnd = new PTtWindow(markersDic, labels);
                pttWnd.Show();

            }
            else
            {
                if (_selectedPoints.Count <= 0) return;
                if (_selectedPoints.Count == 1)
                {
                    var mrk = GetMarkersByPositionAndType(selectedFile, _selectedPoints.Last());
                    mrk.PrnSource = selectedFile;

                    var finalMarkers = new List<Marker>();
                    var labels = new List<string>();

                    foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString))
                    {
                        if (file == selectedFile)
                        {
                            finalMarkers.Add(mrk);
                            labels.Add(mrk.Age.ToString("0.00E+0"));
                            continue;
                        }
                        var marker = GetMarkerFromPrn(file, mrk.Id);
                        marker.PrnSource = file;
                        finalMarkers.Add(marker);
                        labels.Add(marker.Age.ToString("0.00E+0"));
                        //labels.Add(
                        //    Config.Tools.ExtractNumberFromString(
                        //        file.Substring(file.LastIndexOf(@"\", StringComparison.Ordinal) + 1)).ToString());
                    }

                    var pttWnd = new PTtWindow(finalMarkers, labels);
                    pttWnd.Show();
                }
                else
                {
                    var mrk = GetMarkersByPositionAndType(selectedFile, _selectedPoints);

                    foreach (var mr in mrk)
                    {
                        mr.PrnSource = selectedFile;
                    }

                    var allMarkers = new List<List<Marker>>();

                    foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString))
                    {
                        if (file == selectedFile)
                        {
                            allMarkers.Add(mrk);
                            continue;
                        }

                        

                        var markerSet = GetMarkersFromPrn(file, mrk.Select(x => x.Id).ToList());

                        foreach (var mr in markerSet)
                        {
                            mr.PrnSource = file;
                        }

                        allMarkers.Add(markerSet);
                        //labels.Add(
                        //    Config.Tools.ExtractNumberFromString(
                        //        file.Substring(file.LastIndexOf(@"\", StringComparison.Ordinal) + 1)).ToString());
                    }

                    var labels = allMarkers.Select(sets => sets[0].Age.ToString("0.00E+0")).ToList();

                    var markersDic = CategorizeMarkers(allMarkers);

                    var pttWnd = new PTtWindow(markersDic, labels);
                    pttWnd.Show();
                }
            }

        }

        /// <summary>
        /// Пересортировать маркеры из файловой категоризации в индексовую (Switch file-indexes categorization to index-files categorization)
        /// </summary>
        /// <param name="allMarkersList">Лист, состоящий из листов маркеров с одного файла</param>
        /// <returns>Словарь, где ключ - индекс, значение - лист маркеров с этим индексом из всех файлов</returns>
        private Dictionary<uint, List<Marker>> CategorizeMarkers(List<List<Marker>> allMarkersList)
        {
            var markersDic = allMarkersList[0].ToDictionary(marker => marker.Id, marker => new List<Marker>());
            foreach (var markerid in markersDic.Keys)
            {
                foreach (var markers in allMarkersList)
                {
                    var targetMarker = markers.FirstOrDefault(x => x.Id == markerid);
                    markersDic[markerid].Add(targetMarker);
                }
            }
            return markersDic;
        }

        
        /// <summary>
        /// Получить маркер по индексу из prn-файла
        /// </summary>
        /// <param name="filePath">Путь к prn-файлу</param>
        /// <param name="ind">Индекс маркера</param>
        /// <returns>Маркер</returns>
        private Marker GetMarkerFromPrn(string filePath, uint ind)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var intArr = new byte[4];
            var int64Arr = new byte[8];
            var longArr = new byte[45];
            var doubleArr = new byte[72];
            var byteArr = new byte[1];
            var floatArr = new byte[4];

            var A = new byte[4];
            fs.Read(A, 0, 4);

            fs.Read(longArr, 0, 40);
            var xnumx = BitConverter.ToInt64(longArr, 0);
            var ynumy = BitConverter.ToInt64(longArr, 8);
            var mnumx = BitConverter.ToInt64(longArr, 16);
            var mnumy = BitConverter.ToInt64(longArr, 24);
            var marknum = BitConverter.ToInt64(longArr, 32);

            if (ind > marknum) return null;

            fs.Read(doubleArr, 0, 72);
            var xsize = BitConverter.ToDouble(doubleArr, 0);
            var ysize = BitConverter.ToDouble(doubleArr, 8);
            var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
            var gxkoef = BitConverter.ToDouble(doubleArr, 56);
            var gykoef = BitConverter.ToDouble(doubleArr, 64);

            fs.Read(intArr, 0, 4);
            var rocknum = BitConverter.ToInt32(intArr, 0);
            fs.Read(int64Arr, 0, 8);
            var bondnum = BitConverter.ToInt64(int64Arr, 0);
            fs.Read(intArr, 0, 4);
            var n1 = BitConverter.ToInt32(intArr, 0);
            fs.Read(doubleArr, 0, 8);
            var timesum = BitConverter.ToDouble(doubleArr,0);

            // -------

            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);

            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4);
                gx[i] = BitConverter.ToSingle(floatArr, 0);
            }

            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }
            //-------

            var nodenum1 = xnumx * ynumy;
            var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind*37;
            fs.Position = curpos1;

            var parBytes = new byte[36];
            fs.Read(parBytes, 0, 36);

            var rid = new byte[1];
            fs.Read(rid, 0, 1);

            var resultMarker = new Marker
            {
                XPosition = BitConverter.ToSingle(parBytes, 0),
                YPosition = BitConverter.ToSingle(parBytes, 4),
                Temperature = BitConverter.ToSingle(parBytes, 8) - 273,
                Density = BitConverter.ToSingle(parBytes, 12),
                WaterCons = BitConverter.ToSingle(parBytes, 16),
                UndefinedPar1 = BitConverter.ToSingle(parBytes, 20),
                UndefinedPar2 = BitConverter.ToSingle(parBytes, 24),
                Viscosity = BitConverter.ToSingle(parBytes, 28),
                Deformation = BitConverter.ToSingle(parBytes, 32),
                RockId = rid[0],
                Id = ind, 
                Age = timesum
            };

            //     p1--------p3
            //     |         |
            //     p2--------p4

            var p1x = LeftIndex(gx, resultMarker.XPosition);
            var p1y = LeftIndex(gy, resultMarker.YPosition);
            var p2x = p1x;
            var p2y = p1y + 1;
            var p3x = p1x + 1;
            var p3y = p1y;
            var p4x = p1x + 1;
            var p4y = p1y + 1;

            double pressure;

            if (p1x < 0 || p1y < 0)
            {
                pressure = 0;
                resultMarker.Pressure = pressure;
                return resultMarker;
            }

            fs.Position = curpos0 + 120*ynumy*p1x + 120*p1y;
            fs.Read(floatArr, 0, 4);
            var v1 = BitConverter.ToSingle(floatArr, 0);

            fs.Position = curpos0 + 120 * ynumy * p2x + 120 * p2y;
            fs.Read(floatArr, 0, 4);
            var v2 = BitConverter.ToSingle(floatArr, 0);

            fs.Position = curpos0 + 120 * ynumy * p3x + 120 * p3y;
            fs.Read(floatArr, 0, 4);
            var v3 = BitConverter.ToSingle(floatArr, 0);

            fs.Position = curpos0 + 120 * ynumy * p4x + 120 * p4y;
            fs.Read(floatArr, 0, 4);
            var v4 = BitConverter.ToSingle(floatArr, 0);

            var a1x = gx[p1x];
            var a1y = gy[p1y];
            var a4x = gx[p4x];
            var a4y = gy[p4y];

            var interpolation = new InterpolationRectangle(new ModPoint(a1x, a1y), new ModPoint(a4x, a4y), v1, v2, v3, v4, new ModPoint(resultMarker.XPosition, resultMarker.YPosition));
            pressure = interpolation.InterpolatedValue;

            var pr = new float[ynumy, xnumx];
            //var buff = new byte[116];

            //for (int i = 0; i < xnumx; i++)
            //{
            //    for (int j = 0; j < ynumy; j++)
            //    {
            //        fs.Read(floatArr, 0, 4);
            //        var prCur = BitConverter.ToSingle(floatArr, 0);
            //        fs.Position += 116;
            //        //fs.Read(buff, 0, 116);
            //        pr[j, i] = prCur;
            //    }
            //}

            resultMarker.Pressure = pressure;

            fs.Close();

            return resultMarker;
        }


        private List<Marker> GetMarkersFromPrn(string filePath, List< uint> inds)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var intArr = new byte[4];
            var int64Arr = new byte[8];
            var longArr = new byte[45];
            var doubleArr = new byte[72];
            var byteArr = new byte[1];
            var floatArr = new byte[4];

            var A = new byte[4];
            fs.Read(A, 0, 4);

            fs.Read(longArr, 0, 40);
            var xnumx = BitConverter.ToInt64(longArr, 0);
            var ynumy = BitConverter.ToInt64(longArr, 8);
            var mnumx = BitConverter.ToInt64(longArr, 16);
            var mnumy = BitConverter.ToInt64(longArr, 24);
            var marknum = BitConverter.ToInt64(longArr, 32);

           // if (ind > marknum) return null; todo вернуть

            fs.Read(doubleArr, 0, 72);
            var xsize = BitConverter.ToDouble(doubleArr, 0);
            var ysize = BitConverter.ToDouble(doubleArr, 8);
            var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
            var gxkoef = BitConverter.ToDouble(doubleArr, 56);
            var gykoef = BitConverter.ToDouble(doubleArr, 64);

            fs.Read(intArr, 0, 4);
            var rocknum = BitConverter.ToInt32(intArr, 0);
            fs.Read(int64Arr, 0, 8);
            var bondnum = BitConverter.ToInt64(int64Arr, 0);
            fs.Read(intArr, 0, 4);
            var n1 = BitConverter.ToInt32(intArr, 0);
            fs.Read(doubleArr, 0, 8);
            var timesum = BitConverter.ToDouble(doubleArr, 0);


            // -------

            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4);

            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4);
                gx[i] = BitConverter.ToSingle(floatArr, 0);
            }

            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }
            //-------

            var result = new List<Marker>();

            foreach (var ind in inds)
            {
                var nodenum1 = xnumx * ynumy;
                var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8) + ind * 37;
                fs.Position = curpos1;

                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36);

                var rid = new byte[1];
                fs.Read(rid, 0, 1);

                var resultMarker = new Marker
                {
                    XPosition = BitConverter.ToSingle(parBytes, 0),
                    YPosition = BitConverter.ToSingle(parBytes, 4),
                    Temperature = BitConverter.ToSingle(parBytes, 8) - 273,
                    Density = BitConverter.ToSingle(parBytes, 12),
                    WaterCons = BitConverter.ToSingle(parBytes, 16),
                    UndefinedPar1 = BitConverter.ToSingle(parBytes, 20),
                    UndefinedPar2 = BitConverter.ToSingle(parBytes, 24),
                    Viscosity = BitConverter.ToSingle(parBytes, 28),
                    Deformation = BitConverter.ToSingle(parBytes, 32),
                    RockId = rid[0],
                    Id = ind,
                    Age = timesum
                };

                //     p1--------p3
                //     |         |
                //     p2--------p4

                var p1X = LeftIndex(gx, resultMarker.XPosition);
                var p1Y = LeftIndex(gy, resultMarker.YPosition);
                var p2X = p1X;
                var p2Y = p1Y + 1;
                var p3X = p1X + 1;
                var p3Y = p1Y;
                var p4X = p1X + 1;
                var p4Y = p1Y + 1;

                double pressure;

                if (p1X < 0 || p1Y < 0)
                {
                    pressure = 0;
                    resultMarker.Pressure = pressure;
                    result.Add(resultMarker);
                    continue;
                }

                fs.Position = curpos0 + 120 * ynumy * p1X + 120 * p1Y;
                fs.Read(floatArr, 0, 4);
                var v1 = BitConverter.ToSingle(floatArr, 0);

                fs.Position = curpos0 + 120 * ynumy * p2X + 120 * p2Y;
                fs.Read(floatArr, 0, 4);
                var v2 = BitConverter.ToSingle(floatArr, 0);

                fs.Position = curpos0 + 120 * ynumy * p3X + 120 * p3Y;
                fs.Read(floatArr, 0, 4);
                var v3 = BitConverter.ToSingle(floatArr, 0);

                fs.Position = curpos0 + 120 * ynumy * p4X + 120 * p4Y;
                fs.Read(floatArr, 0, 4);
                var v4 = BitConverter.ToSingle(floatArr, 0);

                var a1X = gx[p1X];
                var a1Y = gy[p1Y];
                var a4X = gx[p4X];
                var a4Y = gy[p4Y];

                var interpolation = new InterpolationRectangle(new ModPoint(a1X, a1Y), new ModPoint(a4X, a4Y), v1, v2, v3, v4, new ModPoint(resultMarker.XPosition, resultMarker.YPosition));
                pressure = interpolation.InterpolatedValue;

                var pr = new float[ynumy, xnumx];
                

                resultMarker.Pressure = pressure;

                resultMarker.PrnSource = filePath;

                result.Add(resultMarker);
            }

            fs.Close();

            return result;
        }


        private Marker GetMarkersByPositionAndType(string filePath, ModPoint point)
        {
            var curList = new List<ModPoint> {point};
            return GetMarkersByPositionAndType(filePath, curList).Last();
        }

        private List<Marker> GetMarkersByPositionAndType(string filePath, List<ModPoint> points)
        {

            var fs = new FileStream(filePath, FileMode.Open); // открываем файловый поток (нашей prn-ки)

            // массивы байтов для дальнейшего преобразования в нужные типы
            var intArr = new byte[4]; // массив для преобразования байтов в целое число (int)
            var int64Arr = new byte[8]; // массив для преобразования байтов в целое число расширенного диапазона (long)
            var longArr = new byte[45]; // массив для хранения 45 байт
            var doubleArr = new byte[72]; // массив для хранения 72 байт (в одном месте их надо прочесть сразу)
            var byteArr = new byte[1]; 
            var floatArr = new byte[4]; // для преобразования в вещественное число
            // -------

            var A = new byte[4]; // зачем-то создал ещё один четырёхбайтовый массив (скорее всего, осталось из старых вариантов)
            
            fs.Read(A, 0, 4); // читаем с потока 4 байта (скорее всего эту и строку выше можно было заменить простым изменением байтовой позиции с 0 до 4

            fs.Read(longArr, 0, 40); // считываем теперь 40 байт и запихиваем их в буферный массив longArr
            var xnumx = BitConverter.ToInt64(longArr, 0); // первые 8 байт из этого массива - кол-во узлов по x. преобразуем их в long и записываем в переменную xnumx (не знаю зачем 8 байт, когда 4 было бы достаточно, но так устроен prn)
            var ynumy = BitConverter.ToInt64(longArr, 8); // следующие 8 байт - кол-во узлов по y. Преобразуем в long и в переменную ynumy
            var mnumx = BitConverter.ToInt64(longArr, 16); // следующие 8 байт для mnumx (если често не знаю что за mnumx и mnumy. Обычно они 8 и 4 соответственно. У меня и в матлабовском скрипте жэти переменные нигде не используются)
            var mnumy = BitConverter.ToInt64(longArr, 24); // следующие 8 байт для mnumy
            var marknum = BitConverter.ToInt64(longArr, 32); // следующие 8 байт - кол-во маркеров
            // здесь отдельно обращаю внимание, что все эти байты в 5 верхних строках берутся с массива, а не считываются с потока (с потока мы уже считали 40 байт и теперь распределяем их по переменным)

            fs.Read(doubleArr, 0, 72); // теперь считываем с поток 72 байта и записываем их в буферный массив doubleArr
            var xsize = BitConverter.ToDouble(doubleArr, 0); // первые 8 байт - ширина модели
            var ysize = BitConverter.ToDouble(doubleArr, 8); // следующие 8 байт - высота модели
            var pinit = new double[5]; // создаем массив типа double из 5 элементов
            for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i); // следующие 40 байт идут в массив pinit (по 8 байт на элемент) 
            var gxkoef = BitConverter.ToDouble(doubleArr, 56); // следующие 8 байт - ускорение свободного падения по x
            var gykoef = BitConverter.ToDouble(doubleArr, 64); // следующие 8 байт - ускорение свободного падения по y

            fs.Read(intArr, 0, 4); // продолжаем считывать с потока. теперь считываем 4 байта и записываем в массив для преобразования в int (intArr)
            var rocknum = BitConverter.ToInt32(intArr, 0); // преобразуем эти 4 байта в int и записываем в переменную rocknum. Это кол-во типов пород (если вы новых в init'е не добавляли, то их 39)
            fs.Read(int64Arr, 0, 8); // считываем с потока 8 байт 
            var bondnum = BitConverter.ToInt64(int64Arr, 0); // преобразуем в long и записываем в переменную bondnum (не знаю что это такое, обычно оно 13600, но оно понадобится для смещения байтовой позиции дальше)
            fs.Read(intArr, 0, 4); // считываем с потока 4 байта
            var n1 = BitConverter.ToInt32(intArr, 0); // записываем их в целочисленную переменную n1 (опять же, не знаю что это такое и зачем, нигде не используется. Здесь только чтобы перескочить 4 байта)
            fs.Read(doubleArr, 0, 8); //считываем 8 байт с потока
            var timesum = BitConverter.ToDouble(doubleArr, 0); // преобразуем в double и в переменную timesum. Это текущее время модели (в годах)


            //----------------
            //теперь перескакиваем вот на такую байтовую позицию:
            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4); 
            fs.Position = curpos0 + (4 * 22 + 8 * 4) * xnumx * ynumy;

            // далее создаем одномерный массив gx (это массив расстояний на узлах по x)
            var gx = new float[xnumx];
            for (int i = 0; i < xnumx; i++)
            {
                fs.Read(floatArr, 0, 4); // в цикле считываем каждые 4 байта...
                gx[i] = BitConverter.ToSingle(floatArr, 0); // преобразуем в float и заполняем массив gx
            }

            // то же самое с массивом gy, который содержит глубины на узлах по y
            var gy = new float[ynumy];
            for (int i = 0; i < ynumy; i++)
            {
                fs.Read(floatArr, 0, 4);
                gy[i] = BitConverter.ToSingle(floatArr, 0);
            }

            //-------------
            // теперь снова перескакиваем, уже на такую байтовую позицию:
            var nodenum1 = xnumx * ynumy;
            var curpos1 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8);
            fs.Position = curpos1;
            //вот с этой позиции начинается перебор непостредственно маркеров

            var selectedPts = points.ToDictionary(pt => pt, pt => new List<Marker>());

            double tolerance = 1000;
                int test = 0;
                int test2 = 0;

            // создаем цикл по всем маркерам
            for (int i = 0; i < marknum; i++)
            {
                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36); // на каждом шаге цикла читаем по 36 байт и записываем их в буферный массив parBytes

                var xcoord = BitConverter.ToSingle(parBytes, 0); //первые 4 байта - x-компонента
                var ycoord = BitConverter.ToSingle(parBytes, 4); //следующие 4 байта - y-компонента
                var tk = BitConverter.ToSingle(parBytes, 8) - 273; //следующие 4 байта - температура
                var dens = BitConverter.ToSingle(parBytes, 12); //след. 4 байта - плотность
                var watCons = BitConverter.ToSingle(parBytes, 16); //след. 4 байта - содержание воды 
                var und1 = BitConverter.ToSingle(parBytes, 20); //след. 4 байта - не знаю
                var und2 = BitConverter.ToSingle(parBytes, 24); //след. 4 байта - не знаю
                var viscosity = BitConverter.ToSingle(parBytes, 28); //след. 4 байта, возмонжно, - вязкость 
                var deformation = BitConverter.ToSingle(parBytes, 32); //след. 4 байта - возможно, относительная деформация 
                
                var rid = new byte[1]; 
                fs.Read(rid, 0, 1); // далее, считываем 1 байт
                var rockid = rid[0]; // это Id породы

                //var pressure = GetPressureByPosition(pr, gx, gy, xcoord, ycoord);

                
                foreach (var pt in points)
                {

                    //if (double.IsNaN(xcoord)) test++; else test2++;

                    //if (rockid == pt.Type && Math.Abs(xcoord - pt.X*1000) < tolerance && Math.Abs(ycoord - pt.Y*1000) < tolerance)
                    if ( Math.Abs(xcoord - pt.X * 1000) < tolerance && Math.Abs(ycoord - pt.Y * 1000) < tolerance)
                    {


                        //     p1--------p3
                        //     |         |
                        //     p2--------p4

                        var p1x = LeftIndex(gx, xcoord);
                        var p1y = LeftIndex(gy, ycoord);
                        var p2x = p1x;
                        var p2y = p1y + 1;
                        var p3x = p1x + 1;
                        var p3y = p1y;
                        var p4x = p1x + 1;
                        var p4y = p1y + 1;

                        double pressure;

                        

                        if (p1x < 0 || p1y < 0)
                        {
                            pressure = 0;
                        }
                        else
                        {
                            var prevPosition = fs.Position;
                            fs.Position = curpos0 + 120 * ynumy * p1x + 120 * p1y;
                            fs.Read(floatArr, 0, 4);
                            var v1 = BitConverter.ToSingle(floatArr, 0);

                            fs.Position = curpos0 + 120 * ynumy * p2x + 120 * p2y;
                            fs.Read(floatArr, 0, 4);
                            var v2 = BitConverter.ToSingle(floatArr, 0);

                            fs.Position = curpos0 + 120 * ynumy * p3x + 120 * p3y;
                            fs.Read(floatArr, 0, 4);
                            var v3 = BitConverter.ToSingle(floatArr, 0);

                            fs.Position = curpos0 + 120 * ynumy * p4x + 120 * p4y;
                            fs.Read(floatArr, 0, 4);
                            var v4 = BitConverter.ToSingle(floatArr, 0);

                            var a1x = gx[p1x];
                            var a1y = gy[p1y];
                            var a4x = gx[p4x];
                            var a4y = gy[p4y];

                            var interpolation = new InterpolationRectangle(new ModPoint(a1x, a1y), new ModPoint(a4x, a4y), v1, v2, v3, v4, new ModPoint(xcoord, ycoord));
                            pressure = interpolation.InterpolatedValue;
                            fs.Position = prevPosition;
                        }
                        

                        selectedPts[pt].Add(new Marker
                        {
                            XPosition = xcoord, 
                            YPosition = ycoord, 
                            Temperature = tk, 
                            Density = dens, 
                            WaterCons = watCons, 
                            UndefinedPar1 = und1, 
                            UndefinedPar2 = und2, 
                            Viscosity = viscosity, 
                            Deformation = deformation, 
                            Id = (uint)i, 
                            RockId = rockid, 
                            Pressure = pressure, 
                            Age = timesum
                        });
                    }
                }
            }

            fs.Close();

            var result = new List<Marker>();

            foreach (var mp in selectedPts.Keys)
            {
                var orderedList = selectedPts[mp].OrderBy( x => Math.Sqrt(Math.Pow((x.XPosition - mp.X*1000f), 2) + Math.Pow((x.YPosition- mp.Y*1000f ), 2))).ToList();
                //if (selectedPts[mp].Count > 0) result.Add(selectedPts[mp].First());
                if (orderedList.Count > 0) result.Add(orderedList.First());
            }

            return result;

        }

        //private double GetPressureByPosition(float[,] pr, float[] gx, float[] gy, float xcoord, float ycoord)
        //{

        //    //     p1--------p3
        //    //     |         |
        //    //     p2--------p4

        //    var p1x = LeftIndex(gx, xcoord);
        //    var p1y = LeftIndex(gy, ycoord);
        //    var p2x = p1x;
        //    var p2y = p1y + 1;
        //    var p3x = p1x + 1;
        //    var p3y = p1y;
        //    var p4x = p1x + 1;
        //    var p4y = p1y + 1;

        //    double press;

        //    if (p1x < 0 || p1y < 0)
        //    {
        //        press = 0;
        //    }
        //    else
        //    {
        //        var a1x = gx[p1x];
        //        var a1y = gy[p1y];
        //        var a4x = gx[p4x];
        //        var a4y = gy[p4y];

        //        var interpolation = new InterpolationRectangle(new ModPoint(a1x, a1y), new ModPoint(a4x, a4y), pr[p1y, p1x], pr[p2y, p2x], pr[p3y, p3x], pr[p4y, p4x], new ModPoint(xcoord, ycoord));
        //        press = interpolation.InterpolatedValue;
        //    }

        //    return press;
        //}

        private int LeftIndex(float[] arr, float coord)
        {
            for (int i = 0; i < arr.Length-1; i++)
            {
                if (arr[i] <= coord && arr[i + 1] > coord) return i;
            }
            return -1;
        }

        private void ReadPrn(string filePath)
        {
            var fs = new FileStream(filePath, FileMode.Open);

            var intArr = new byte[4];
            var int64Arr = new byte[8];
            var longArr = new byte[45];
            var doubleArr = new byte[72];
            var byteArr = new byte[1];

            var A = new byte[4];
            fs.Read(A, 0, 4);

            fs.Read(longArr, 0, 40);
            var xnumx = BitConverter.ToInt64(longArr, 0);
            var ynumy = BitConverter.ToInt64(longArr, 8);
            var mnumx = BitConverter.ToInt64(longArr, 16);
            var mnumy = BitConverter.ToInt64(longArr, 24);
            var marknum = BitConverter.ToInt64(longArr, 32);

            fs.Read(doubleArr, 0, 72);
            var xsize = BitConverter.ToDouble(doubleArr, 0);
            var ysize = BitConverter.ToDouble(doubleArr, 8);
            var pinit = new double[5]; for (int i = 0; i < 5; i++) pinit[i] = BitConverter.ToDouble(doubleArr, 16 + 8 * i);
            var gxkoef = BitConverter.ToDouble(doubleArr, 56);
            var gykoef = BitConverter.ToDouble(doubleArr, 64);

            fs.Read(intArr, 0, 4);
            var rocknum = BitConverter.ToInt32(intArr, 0);
            fs.Read(int64Arr, 0, 8);
            var bondnum = BitConverter.ToInt64(int64Arr, 0);

            var nodenum1 = xnumx * ynumy;
            var curpos0 = 4 + 2 * 4 + 16 * 8 + rocknum * (8 * 24 + 4) + 15 * 8 * nodenum1 + 4 * (xnumx + ynumy) + (bondnum - 1) * (16 + 3 * 8);
            fs.Position = curpos0;

            for (int i = 0; i < marknum; i++)
            {
                var parBytes = new byte[36];
                fs.Read(parBytes, 0, 36);

                var xcoord = BitConverter.ToSingle(parBytes, 0);
                var ycoord = BitConverter.ToSingle(parBytes, 4);
                var tk = BitConverter.ToSingle(parBytes, 8);
                var dens = BitConverter.ToSingle(parBytes, 12);

                var rid = new byte[1];
                fs.Read(rid, 0, 1);
                var rockid = rid[0];
                //1 - x-компонента (4 байта)
                //2 - y-компонента (4 байта)
                //3 - температура (4 байта)
                //4 - плотность (4 байта)
                //5 - содержание воды (4 байта)
                //6 - хз (4 байта)
                //7 - хз (4 байта)
                //8 - скорее всего - вязкость (4 байта)
                //9 - скорее всего - относительная деформация (4 байта)
                //10 - тип породы (1 байт)
            }

            fs.Close();

        }

        private void PrnFilesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFile = PrnFilesListBox.SelectedItem.ToString();
            LoadTxtButton.IsEnabled = true;
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (IndexRb.IsChecked == true)
            {
                var selPt = (uint) MarkersList.SelectedItem;
                _selectedIndexes.Remove(selPt);
                MarkersList.Items.Refresh();
            }
            else
            {
                var selPt = MarkersList.SelectedItem as ModPoint;
                _selectedPoints.Remove(selPt);
                MarkersList.Items.Refresh();
            }
            
        }

        private void IndexRb_OnChecked(object sender, RoutedEventArgs e)
        {
            IndexInputStackPanel.Visibility = Visibility.Visible;
            MarkersList.ItemsSource = _selectedIndexes;
            MarkersList.Items.Refresh();
        }

        private void IndexRb_OnUnchecked(object sender, RoutedEventArgs e)
        {
            IndexInputStackPanel.Visibility = Visibility.Collapsed;
            MarkersList.ItemsSource = _selectedPoints;
            MarkersList.Items.Refresh();
        }

        private void MarkerIndexTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Config.Tools.OnlyNumeric(e.Text);
        }

        private void MarkerIndexInputButton_Click(object sender, RoutedEventArgs e)
        {
            if ( String.IsNullOrWhiteSpace(MarkerIndexTextBox.Text) ) return;
            var curInd = Config.Tools.ParseOrDefaultInt(MarkerIndexTextBox.Text, -1);
            if (curInd < 0)
            {
                MessageBox.Show("Некорректный индекс!");
                return;
            }

            var uintInd = Convert.ToUInt32(curInd);

            if (_selectedIndexes.Contains(uintInd))
            {
                MessageBox.Show("Данный индекс уже содержиться в списке!");
                return;
            }

            _selectedIndexes.Add(uintInd);
            MarkersList.Items.Refresh();
            MarkerIndexTextBox.Text = "";
        }

        private void SearchRocksButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            var rockIndexes = ReadRockIndexesFromBox(TargetRocksIndexesBox);
            if (rockIndexes == null || rockIndexes.Count == 0) return;
            
            var minX = Math.Min(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var maxX = Math.Max(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var minY = Math.Min(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));
            var maxY = Math.Max(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));
            var tarea = new CoordIntRectangle(minX, minY, maxX, maxY);

            var outList = new List<string>();

            var rocksCt = rockIndexes.ToDictionary(x=>x, y=>0);

            foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString))
            {
                var pointsToInvestigate = PrnWorker.GetMarkersByRocksIdInRange(file, rockIndexes,
                tarea); //все маркеры заданной области и с заданными ID пород

                if (pointsToInvestigate.Count > 0)
                {
                    outList.Add(file);

                    foreach(var rind in rockIndexes)
                    {
                        var ct = pointsToInvestigate.Count(x => x.RockId == rind);
                        outList.Add(string.Format("{0}: {1}", rind, ct));
                        if (ct > rocksCt[rind]) rocksCt[rind] = ct;
                    }

                    foreach(var pt in pointsToInvestigate)
                    {
                        outList.Add(string.Format("{0}\t{1}\t{2}\t{3}",pt.RockId, pt.Id, pt.XPosition, pt.YPosition));
                    }
                }
                
            }

            if (outList.Count == 0)
            {
                MessageBox.Show("Не было найдено ни одного маркера с заданными индексами :(");
                return;
            }

            var sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            //File.WriteAllLines(sfd.FileName, outList);

            var ctlist = new List<string>() { selectedFile } ;
            foreach( var ri in rocksCt.Keys ) {
                ctlist.Add(string.Format("{0}: {1} ({2:0.00000000}%)", ri, rocksCt[ri], Convert.ToDouble(rocksCt[ri])/tarea.kostyl));
            }
            ctlist.Add("--");

           // File.AppendAllLines(@"H:\modelling_results\vsz\Hot_orogens\T_150\granits.txt", ctlist);

        }

        private void MarkerIndexTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MarkerIndexInputButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            }
        }

        private void SearchMarkersButton_Click(object sender, RoutedEventArgs e)
        {

            if (!ValidateInput()) return;
            var rockIndexes = ReadRockIndexesFromBox(RockIndexesBox);
            if (rockIndexes == null || rockIndexes.Count == 0) return;
            
            var minSubdDepth = Config.Tools.ParseOrDefaultDouble(MinDepthBox.Text) * 1000;
            var maxExumDepth = Config.Tools.ParseOrDefaultDouble(MaxDepthBox.Text) * 1000;

            var minX = Math.Min(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var maxX = Math.Max(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var minY = Math.Min(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));
            var maxY = Math.Max(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));

            var pointsToInvestigate = PrnWorker.GetMarkersByRocksIdInRange(selectedFile, rockIndexes,
                new CoordIntRectangle(minX, minY, maxX, maxY)); //все маркеры заданной области и с заданными ID пород

            var ptIndexes = pointsToInvestigate.Select(x => x.Id).ToList();

            // key - filename, value - markers
            var markersSet = new Dictionary<string,  Dictionary<uint,Marker> >();
            
            foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString))
            {
                //if (file == selectedFile)
                //{
                //    markersSet.Add(file, pointsToInvestigate);
                //    continue;
                //}
                var curFileMarkers = PrnWorker.GetMarkersFromPrnByIndexesDict(file, ptIndexes, false);
                if (curFileMarkers.Count > 0) markersSet.Add(file, curFileMarkers);
            }
            

            // key - markerID, value - marker
            var contenderMarkers = new Dictionary<uint, Marker>(); //сюда накапливаем маркеры, достигшие нужной глубины
            //key - filename, value - age
            var ages = new Dictionary<string, double>(); //список имеющихся возрастов

            // отбор субдуцированных на заданную глубину маркеров 
            foreach (var setKey in markersSet.Keys)
            {
                ages.Add( setKey, markersSet[setKey].First().Value.Age);
                foreach (var marker in markersSet[setKey].Values)
                {
                    if (marker.YPosition >= minSubdDepth && !contenderMarkers.ContainsKey(marker.Id)) contenderMarkers.Add(marker.Id, marker); 
                }
            }

            var firstResultString = string.Format("Кол-во маркеров с заданными индексами в выбранной области:{0}\nКол-во маркеров достигших заданной глубины погружения: {1}", pointsToInvestigate.Count, contenderMarkers.Count );
            var secondResultString = "";

            // поиск эксгумированных пород
            bool removeContender = true;
            if (contenderMarkers.Count > 0 && maxExumDepth > 0)
            {

                foreach (var markerId in contenderMarkers.Keys.ToList())
                {
                    removeContender = true;
                    foreach (var filemame in markersSet.Keys)
                    {
                        if (ages[filemame] < contenderMarkers[markerId].Age) continue; //если набор "младше" текущего маркера - пропускаем ход. Нас интересуют только те, что старше текущего (т.е. всплывшие)
                        //removeContender = !markersSet[filemame].Any(x => x.Id == markerId && x.YPosition <= maxExumDepth);

                        var ctMrk = markersSet[filemame][markerId];
                        if (ctMrk.YPosition <= maxExumDepth)
                        {
                            removeContender = false;
                            break;
                        }

                        //var exhumedMarker = markersSet[filemame].FirstOrDefault(
                        //    x =>
                        //        x.Id == markerId &&
                        //        x.YPosition <= maxExumDepth);

                        //if (exhumedMarker != null)
                        //{
                        //    //Trace.WriteLine(exhumedMarker.YPosition);
                        //    removeContender = false;
                        //    break;
                        //} 
                    }
                    if (removeContender) contenderMarkers.Remove(markerId);
                }

                secondResultString = string.Format("Кол-во маркеров всплывших до заданной глубины: {0}", contenderMarkers.Count);
            }

            if (contenderMarkers.Count == 0)
            {
                MessageBox.Show("Маркеров с заданными параметрами не обнаружено");
                return;
            }

            MessageBoxResult result = MessageBox.Show(string.Format("{0}\n{1}\nХотите сохранить результаты в файл?", firstResultString, secondResultString), "Результат", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var sfd = new SaveFileDialog();
                sfd.Filter = "Текстовый файл | *.txt";
                sfd.FileName = string.Format("rocks_{0}_subd{1}_exhum{2}", string.Join("-", rockIndexes), minSubdDepth/1000f, maxExumDepth/1000f); // selectedFile
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var filestream = new StreamWriter(sfd.FileName))
                    {
                        filestream.WriteLine("#ID\tRockID");
                        foreach (var markId in contenderMarkers.Keys)
                        {
                            var mrk = contenderMarkers[markId];
                            filestream.WriteLine("{0}\t{1}", markId, mrk.RockId);
                        }
                    }
                }
            }

            var markersToPlot = new Dictionary<uint, List<Marker> >();
            var plotMarkersCount = Config.Tools.ParseOrDefaultInt(PlotMarkersCountBox.Text, -1);

            if (plotMarkersCount < 0 || plotMarkersCount > 10 )
            {
                MessageBox.Show("Вы ввели недопустимое или слишком большое кол-во отображаемых маркеров. Их должно быть не больше 10. Меняем текущее значение на 10");
                plotMarkersCount = 10;
            } 

            if (plotMarkersCount == 0) return;

            var selMrkCount = contenderMarkers.Count; //кол-во отобранных маркеров
            var step = (selMrkCount / plotMarkersCount > 1) ? selMrkCount / plotMarkersCount : 1;

            var contendersIndexes = contenderMarkers.Keys.ToList();
            var indexesToPlot = new List<uint>();

            for (int i = 0 + step / 2; i < selMrkCount; i += step)
            {
                var mInd = contendersIndexes[i];
                indexesToPlot.Add(mInd);
                //markersToPlot.Add(mInd, new List<Marker>());
                //foreach (var fl in markersSet.Keys)
                //{
                //    markersToPlot[mInd].Add(markersSet[fl][mInd]);
                //}
            }

            foreach (var file in markersSet.Keys)
            {
                var cursStepMarkers = PrnWorker.GetMarkersFromPrnByIndexes(file, indexesToPlot, true);
                foreach (var mrk in cursStepMarkers)
                {
                    if (!markersToPlot.Keys.Contains(mrk.Id))
                    {
                        markersToPlot.Add(mrk.Id, new List<Marker> {mrk});
                    }
                    else
                    {
                        markersToPlot[mrk.Id].Add(mrk);
                    }
                }
            }

            //сортируем каждый набор маркеров по возрасту
            foreach (var mrkId in markersToPlot.Keys.ToList())
            {
                markersToPlot[mrkId] = markersToPlot[mrkId].OrderBy(x => x.Age).ToList();
            }

            var pttWnd = new PTtWindow(markersToPlot);
            pttWnd.Show();
        }


        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            switch (SearchOptionsTabControl.SelectedIndex)
            {
                case 0:
                    TipLabel.Content = "Кликами мыши по изображению выберите точки, которые хотите отследить";
                    _forArea = false;
                    break;
                case 1:
                    TipLabel.Content = "Выделите мышью прямоугольную область, в пределах которой будет происходить поиск маркеров";
                    _forArea = true;
                    break;
                case 2:
                    TipLabel.Content = "Выделите мышью прямоугольную область, в пределах которой будет происходить поиск маркеров";
                    _forArea = true;
                    break;
                case 3:
                    TipLabel.Content = "Выделите мышью прямоугольную область, в пределах которой будет происходить поиск маркеров";
                    _forArea = true;
                    break;
                default:
                    TipLabel.Content = "";
                    break;
            }

        }

        private void PTtBuilderWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (GeologyConfig.Instace.MetamorphicFacies == null || GeologyConfig.Instace.MetamorphicFacies.Count == 0) return;
            MetamorphicFaciesListBox.ItemsSource = GeologyConfig.Instace.MetamorphicFacies;
        }

        private bool ValidateInput()
        {
            if (files == null || files.Count == 0)
            {
                MessageBox.Show("Вы не загрузили файлы!");
                return false;
            }
            if (plotView.Model == null)
            {
                MessageBox.Show("Вы не загрузили txt!");
                return false;
            }
            if (_currentRectangle.Tag == null)
            {
                MessageBox.Show("Вы не выбрали область поиска!");
                return false;
            }

            return true;
        }

        private List<int> ReadRockIndexesFromBox(TextBox tb)
        {
            var rockIndexes = new List<int>();
            var errInds = new List<string>();

            foreach (var rInd in tb.Text.Split(new[] { ' ', '|', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var ind = Config.Tools.ParseOrDefaultInt(rInd, -1);
                if (ind < 0) errInds.Add(string.Format("\"{0}\"", rInd)); else rockIndexes.Add(ind);
            }

            if (errInds.Count > 0)
            {
                MessageBox.Show(string.Format("Символы {0} не были распознаны как индексы!", string.Join(", ", errInds)));
                return null;
            }

            return rockIndexes;

        }

        private void MetaFaciesSearchMarkersButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;
            var rockIndexes = ReadRockIndexesFromBox(MetaRockIndexesBox);
            if (rockIndexes == null || rockIndexes.Count == 0) return;

            if (MetamorphicFaciesListBox.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите интересующие фации метаморфизма!");
            }

            var minX = Math.Min(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var maxX = Math.Max(Convert.ToInt32(_currentRectangle.MinimumX), Convert.ToInt32(_currentRectangle.MaximumX));
            var minY = Math.Min(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));
            var maxY = Math.Max(Convert.ToInt32(_currentRectangle.MinimumY), Convert.ToInt32(_currentRectangle.MaximumY));

            var pointsToInvestigate = PrnWorker.GetMarkersByRocksIdInRange(selectedFile, rockIndexes,
                new CoordIntRectangle(minX, minY, maxX, maxY)); //все маркеры заданной области и с заданными ID пород

            var ptIndexes = pointsToInvestigate.Select(x => x.Id).ToList();

            // key - filename, value - markers
            var markersSet = new Dictionary<string, Dictionary<uint, Marker>>();

            foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString))
            {
                var curFileMarkers = PrnWorker.GetMarkersFromPrnByIndexesDict(file, ptIndexes, true);
                if (curFileMarkers.Count > 0) markersSet.Add(file, curFileMarkers);
            }

            var targetFacies = (from object facie in MetamorphicFaciesListBox.SelectedItems select facie as MetamorphicFacie).ToList();
            var resultMarkersIndexes = targetFacies.ToDictionary(tfacie => tfacie, tfacie => new List<Marker>()); //key - фация, value - список индексов маркеров

            //----

            foreach (var mrkInd in ptIndexes)
            {
                //todo проверить как повлияет на производительность проверка на существование индекса
                var mrkSeria = markersSet.Keys.Select(prnFile => markersSet[prnFile][mrkInd]).ToList(); //список версий текущего маркера из разных файлов
                var peakMrk = mrkSeria[0];
                foreach (var mrk in mrkSeria)
                {
                    if (mrk.Temperature > peakMrk.Temperature) peakMrk = mrk;
                }
                var curFacie = GeologyConfig.Instace.GetFacie(new PTPoint(peakMrk.Pressure, peakMrk.Temperature));

                if (targetFacies.Contains(curFacie)) resultMarkersIndexes[curFacie].Add(peakMrk);
            }

            var resultString = resultMarkersIndexes.Aggregate("", (current, res) => current + string.Format("{0}: {1} маркеров\n", res.Key.Name, res.Value.Count));

            MessageBoxResult result = MessageBox.Show(string.Format("{0}\nХотите сохранить результаты в файл?", resultString), "Результат", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var sfd = new SaveFileDialog();
                sfd.Filter = "Текстовый файл | *.txt";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {

                    var fullInfo = (FullFileFacieCheckBox.IsChecked == true);

                    using (var filestream = new StreamWriter(sfd.FileName))
                    {

                        if (fullInfo)
                        {
                            filestream.WriteLine("#Facie - Markers Count");
                            filestream.WriteLine("#ID");
                            filestream.WriteLine("#X (m)\tY (m)\tPressure (Pa)\tTemperature (C)\tAge (Ma)\tFacie\tPrnFile");
                            filestream.WriteLine("#Prn-files count: {0}", markersSet.Keys.Count);
                            filestream.WriteLine("#---");

                            foreach (var res in resultMarkersIndexes)
                            {
                                filestream.WriteLine("~{0} - {1}{2}", res.Key.Name, res.Value.Count, res.Key.Color.HasValue ? string.Format("|{0},{1},{2}", res.Key.Color.Value.R, res.Key.Color.Value.G, res.Key.Color.Value.B) : "" );
                                foreach (var mrk in res.Value)
                                {
                                    filestream.WriteLine(mrk.Id);
                                    foreach (var file in markersSet.Keys)
                                    {
                                        var curM = markersSet[file][mrk.Id];
                                        var curFc = GeologyConfig.Instace.GetFacie(new PTPoint(curM.Pressure, curM.Temperature));

                                        filestream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", 
                                            curM.XPosition.ToString("0"), 
                                            curM.YPosition.ToString("0"), 
                                            curM.Pressure.ToString("0"), 
                                            curM.Temperature.ToString("0"), 
                                            curM.Age.ToString("E2"),
                                            (curFc == null) ? "Not_Defined" : curFc.Name, 
                                            file );
                                    }
                                }
                                filestream.WriteLine(@"--***--");
                            }

                        }
                        else
                        {
                            filestream.WriteLine("#Facie - Markers Count");
                            filestream.WriteLine("#ID\tX (m)\tY (m)\tPressure (Pa)\tTemperature (C)\tAge (Ma)\tFacie");
                            filestream.WriteLine("#---");
                            foreach (var res in resultMarkersIndexes)
                            {
                                filestream.WriteLine("{0} - {1}", res.Key.Name, res.Value.Count);
                                foreach (var mrk in res.Value)
                                {
                                    filestream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", mrk.Id, mrk.XPosition.ToString("0"), mrk.YPosition.ToString("0"), mrk.Pressure.ToString("0"), mrk.Temperature.ToString("0"), mrk.Age.ToString("E2"), res.Key.Name);
                                }
                            }
                        }

                    }
                }
            }

            var markersToPlot = new Dictionary<uint, List<Marker>>();
            
            var markersPerFacie = Config.Tools.ParseOrDefaultInt(PlotMarkersFacieCountBox.Text, -1);

            if (markersPerFacie == -1 || markersPerFacie > 5)
            {
                MessageBox.Show("Вы ввели недопустимое число маркеров на фацию в отображении графика. Меняем его на 3");
                markersPerFacie = 3;
            }

            if (markersPerFacie == 0) return;

            //отбираем выводимые маркеры
            foreach (var facie in resultMarkersIndexes.Keys)
            {
                var step = (resultMarkersIndexes[facie].Count > markersPerFacie)
                    ? resultMarkersIndexes[facie].Count/markersPerFacie
                    : 1;
                //и добавляем в словарь
                for (int i = 0 + step/2 ; i < resultMarkersIndexes[facie].Count; i += step)
                {
                    markersToPlot.Add(resultMarkersIndexes[facie][i].Id, new List<Marker>());
                }
            }

            //формируем словарь для построения PTt-тренда
            foreach (var prnFile in markersSet.Keys)
            {
                foreach (var mrkInd in markersToPlot.Keys)
                {
                    markersToPlot[mrkInd].Add(markersSet[prnFile][mrkInd]);
                }
            }

            //сортируем каждый маркер по возрасту
            foreach (var mrkId in markersToPlot.Keys.ToList())
            {
                markersToPlot[mrkId] = markersToPlot[mrkId].OrderBy(x => x.Age).ToList();
            }

            // выводим график
            var pptWnd = new PTtWindow(markersToPlot);
            pptWnd.Show();
        }
    }
}
