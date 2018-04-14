using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
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
using OxyPlot.Wpf;
using Xceed.Wpf.Toolkit;
using AreaSeries = OxyPlot.Series.AreaSeries;
using CheckBox = System.Windows.Controls.CheckBox;
using HeatMapSeries = OxyPlot.Series.HeatMapSeries;
using Image = System.Drawing.Image;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LinearColorAxis = OxyPlot.Axes.LinearColorAxis;
using LineAnnotation = OxyPlot.Annotations.LineAnnotation;
using LineSeries = OxyPlot.Series.LineSeries;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using PixelFormat = System.Windows.Media.PixelFormat;
using Point = System.Drawing.Point;
using PointAnnotation = OxyPlot.Annotations.PointAnnotation;
using Rectangle = System.Drawing.Rectangle;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using ScatterSeries = OxyPlot.Series.ScatterSeries;
using TextAnnotation = OxyPlot.Annotations.TextAnnotation;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for PTtWindow.xaml
    /// </summary>
    public partial class PTtWindow : Window
    {

        private PlotModel pttModel, pathModel;
        private List<string> _labels;

        private Dictionary<uint, List<Marker>> MarkersCollection; 

        public string PrnFilesPath { get; set; }

        public PTtWindow(List<Marker> markerList, List<string> labels = null)
        {
            InitializeComponent();

            MarkersCollection = new Dictionary<uint, List<Marker>> {{markerList[0].Id, markerList}};

            #region ptt rend

            var pmin = markerList.Min(x => x.Pressure);
            var pmax = markerList.Max(x => x.Pressure);

            var pAxe = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = pmin - (pmax-pmin)*0.1,
                Maximum = pmax + (pmax - pmin)*0.1,
                UseSuperExponentialFormat = true,
                Title = "Давление, Па",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                FontSize = 16
            };

            var tmin = markerList.Min(x => x.Temperature);
            var tmax = markerList.Max(x => x.Temperature);

            var tAxe = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = tmin - (tmax - tmin)*0.1,
                Maximum = tmax + (tmax - tmin)*0.1,
                Title = "Температура, °C",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                FontSize = 16
            };

            var pttSeria = new LineSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                Smooth = true,
                StrokeThickness = 3,
                Title = markerList[0].Id.ToString(),
                Tag = markerList[0].Id
            };

            pttModel = new PlotModel();
            pttModel.Axes.Add(tAxe);
            pttModel.Axes.Add(pAxe);
            pttModel.Title = "PTt тренд";

            var k = 1.7e9/1800;
            var b = 2.1e9 - 100*k;

            var uhpmLine = new LineAnnotation
            {
                Slope = k,
                Intercept = b,
                MinimumX = 100,
                MaximumX = 1900,
                MinimumY = 2.1e9,
                MaximumY = 3.8e9,
                LineStyle = LineStyle.LongDash
            };

            k = 3.3e9/1400;
            b = 0.3e9 - 500*k;

            var epmLine = new LineAnnotation
            {
                Slope = k,
                Intercept = b,
                MinimumX = 500,
                MaximumX = 1900,
                MinimumY = 0.3e9,
                MaximumY = 3.6e9,
                LineStyle = LineStyle.LongDash
            };

            pttModel.Annotations.Add(uhpmLine);
            pttModel.Annotations.Add(epmLine);

            foreach (var markerState in markerList)
            {
                pttSeria.Points.Add(new DataPoint(markerState.Temperature, markerState.Pressure) );
            }

            pttModel.Series.Add(pttSeria);

            PtTView.Model = pttModel;

            #endregion 

            #region path trend

            var xmin = markerList.Min(x => x.XPosition);
            var xmax = markerList.Max(x => x.XPosition);

            var xAxe = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = xmin - (xmax-xmin)*0.05,
                Maximum = xmax + (xmax - xmin) * 0.05,
                Title = "X, км",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                LabelFormatter = AxisLabelKmFormat,
                FontSize = 16
            };

            var ymin = markerList.Min(x => x.YPosition);
            var ymax = markerList.Max(x => x.YPosition);

            var yAxe = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = ymin - (ymax - ymin)*0.05,
                Maximum = ymax + (ymax - ymin)*0.05,
                StartPosition = 1,
                EndPosition = 0,
                Title = "Y, км",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                LabelFormatter = AxisLabelKmFormat,
                FontSize = 16
            };

            List<OxyColor> colors = GraphConfig.Instace.ColorMap;
            colors.Add(OxyColors.Transparent);
            colors.Add(OxyColors.White);

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

            

            var pathSeria = new LineSeries
            {
                MarkerType = MarkerType.Diamond,
                MarkerSize = 5,
                Smooth = true,
                Title = markerList[0].Id.ToString(),
                Tag = markerList[0].Id
            };

            pathModel = new PlotModel();
            pathModel.Axes.Add(xAxe);
            pathModel.Axes.Add(yAxe);
            pathModel.Axes.Add(cAxe);
            pathModel.Title = "Траектория движения маркера";


            pathModel.Series.Add(pathSeria);

            foreach (var markerState in markerList)
            {
                pathSeria.Points.Add(new DataPoint(markerState.XPosition, markerState.YPosition));
            }

            PathView.Model = pathModel;
            #endregion

            #region подписи
            if (labels != null && labels.Count >= markerList.Count)
            {
                for (int i = 0; i < pttSeria.Points.Count; i++)
                {
                    pathModel.Annotations.Add(new PointAnnotation()
                    {
                        X = pathSeria.Points[i].X,
                        Y = pathSeria.Points[i].Y,
                        Text = labels[i],
                        Fill = pathSeria.Color
                    });
                    pttModel.Annotations.Add(new PointAnnotation()
                    {
                        X = pttSeria.Points[i].X,
                        Y = pttSeria.Points[i].Y,
                        Text = labels[i],
                        Fill = pathSeria.Color
                    });
                }
                PathView.InvalidatePlot(false);
                PtTView.InvalidatePlot(false);
            }
            //else
            //{
            //    for (int i = 0; i < pttSeria.Points.Count; i++)
            //    {
            //        pathModel.Annotations.Add(new PointAnnotation()
            //        {
            //            X = pathSeria.Points[i].X,
            //            Y = pathSeria.Points[i].Y,
            //            Text = markerList[i].Age.ToString("E2"),
            //            Fill = pathSeria.Color
            //        });
            //        pttModel.Annotations.Add(new PointAnnotation()
            //        {
            //            X = pttSeria.Points[i].X,
            //            Y = pttSeria.Points[i].Y,
            //            Text = markerList[i].Age.ToString("E2"),
            //            Fill = pathSeria.Color
            //        });
            //    }
            //    PathView.InvalidatePlot(false);
            //    PtTView.InvalidatePlot(false);
            //}
            #endregion

        }

        List<PointAnnotation> _labelAnnotations = new List<PointAnnotation>();

        public PTtWindow(Dictionary<uint, List<Marker>> markersSet, List<string> labels = null)
        {
            InitializeComponent();

            MarkersCollection = markersSet;

            ShowSeriesBox.Visibility = Visibility.Visible;
            AgesListBox.Visibility = Visibility.Visible;
            AgeLabel.Visibility = Visibility.Visible;

            #region ptt rend

            var pmin = markersSet.First().Value.Min(x => x.Pressure);
            var pmax = markersSet.First().Value.Max(x => x.Pressure);

            var pAxe = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = pmin - (pmax - pmin)*0.1,
                Maximum = pmax + (pmax - pmin)*0.1,
                UseSuperExponentialFormat = true,
                Title = "Давление, Па",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                FontSize = 16
            };

            var tmin = markersSet.First().Value.Min(x => x.Temperature);
            var tmax = markersSet.First().Value.Max(x => x.Temperature);

            var tAxe = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = tmin - (tmax - tmin) * 0.1,
                Maximum = tmax + (tmax - tmin) * 0.1,
                Title = "Температура, °C",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                FontSize = 16
            };

            

            pttModel = new PlotModel();
            pttModel.Axes.Add(tAxe);
            pttModel.Axes.Add(pAxe);
            pttModel.Title = "PTt тренд";

            var k = 1.7e9 / 1800;
            var b = 2.1e9 - 100 * k;

            var uhpmLine = new LineAnnotation
            {
                Slope = k,
                Intercept = b,
                MinimumX = 100,
                MaximumX = 1900,
                MinimumY = 2.1e9,
                MaximumY = 3.8e9,
                LineStyle = LineStyle.LongDash
            };

            k = 3.3e9 / 1400;
            b = 0.3e9 - 500 * k;

            var epmLine = new LineAnnotation
            {
                Slope = k,
                Intercept = b,
                MinimumX = 500,
                MaximumX = 1900,
                MinimumY = 0.3e9,
                MaximumY = 3.6e9,
                LineStyle = LineStyle.LongDash
            };

            pttModel.Annotations.Add(uhpmLine);
            pttModel.Annotations.Add(epmLine);

            foreach (var curSet in markersSet.Values)
            {
                var pttSeria = new LineSeries
                {
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    Smooth = true,
                    StrokeThickness = 3,
                    Title = curSet[0].Id + String.Format(" ({0})", curSet[0].RockId),
                    Tag = curSet[0].Id
                };

                foreach (var markerState in curSet)
                {
                    pttSeria.Points.Add(new DataPoint(markerState.Temperature, markerState.Pressure) );
                }

                if (labels != null && labels.Count >= curSet.Count)
                {
                    for (int i = 0; i < curSet.Count; i++)
                    {
                        var ann = new PointAnnotation
                        {
                            X = pttSeria.Points[i].X,
                            Y = pttSeria.Points[i].Y,
                            Text = (i+1).ToString(),
                            Fill = pttSeria.Color,
                            Tag = pttSeria
                        };
                        
                        pttModel.Annotations.Add(ann);
                        _labelAnnotations.Add(ann);
                    }
                }

                pttModel.Series.Add(pttSeria);
            }


            PtTView.Model = pttModel;

            #endregion

            #region path trend

            var xmin = markersSet.First().Value.Min(x => x.XPosition);
            var xmax = markersSet.First().Value.Max(x => x.XPosition);

            var xAxe = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Minimum = xmin - (xmax - xmin) * 0.05,
                Maximum = xmax + (xmax - xmin) * 0.05,
                Title = "X, км",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                LabelFormatter = AxisLabelKmFormat,
                FontSize = 16
            };

            var ymin = markersSet.First().Value.Min(x => x.YPosition);
            var ymax = markersSet.First().Value.Max(x => x.YPosition);

            var yAxe = new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = ymin - (ymax - ymin) * 0.05,
                Maximum = ymax + (ymax - ymin) * 0.05,
                StartPosition = 1,
                EndPosition = 0,
                Title = "Y, км",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dash,
                LabelFormatter = AxisLabelKmFormat,
                FontSize = 16
            };

            List<OxyColor> colors = GraphConfig.Instace.ColorMap.ToList();
            colors.Add(OxyColors.Transparent);
            colors.Add(OxyColors.White);

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

            pathModel = new PlotModel();
            pathModel.Axes.Add(xAxe);
            pathModel.Axes.Add(yAxe);
            pathModel.Axes.Add(cAxe);
            pathModel.Title = "Траектория движения маркера";

            int mti = 0;

            var mtList = new List<MarkerType>() { MarkerType.Circle, MarkerType.Diamond, MarkerType.Square, MarkerType.Triangle };

            foreach (var curSet in markersSet.Values)
            {
                var pathSeria = new LineSeries
                {
                    MarkerType = mtList[mti],
                    MarkerSize = 5,
                    Smooth = true,
                    Title = curSet[0].Id + String.Format(" ({0})", curSet[0].RockId),
                    Tag = curSet[0].Id
                };

                

                foreach (var markerState in curSet)
                {
                    pathSeria.Points.Add(new DataPoint(markerState.XPosition, markerState.YPosition));
                }

                pathModel.Series.Add(pathSeria);
                //pttModel.Series.Add(pathSeria);

                if (labels != null && labels.Count >= curSet.Count)
                {
                    for (int i = 0; i < curSet.Count; i++)
                    {
                        var ann = new PointAnnotation
                        {
                            X = pathSeria.Points[i].X,
                            Y = pathSeria.Points[i].Y,
                            Text = (i+1).ToString(),
                            Fill = pathSeria.Color,
                            Tag = pathSeria
                        };
                        pathModel.Annotations.Add(ann);
                        _labelAnnotations.Add(ann);
                    }
                }

                mti++;
                if (mti >= mtList.Count) mti = 0;
            }

            
            

            PathView.Model = pathModel;
            #endregion

            
            #region формирование чекбоксов отображения графиков

            foreach (var markerId in markersSet.Keys)
            {
                var cb = new CheckBox
                {
                    Content = markerId,
                    Tag = markerId,
                    IsChecked = true
                };

                cb.Checked += (sender, args) =>
                {
                    var selectedPtt = pttModel.Series.FirstOrDefault(x => ((uint) x.Tag) == (uint) cb.Tag);
                    if (selectedPtt != null)
                    {
                        selectedPtt.IsVisible = true;
                        pttModel.InvalidatePlot(false);
                    }
                    var selectedPath = pathModel.Series.FirstOrDefault(x => x.Tag != null && ((uint)x.Tag) == (uint)cb.Tag);
                    if (selectedPath != null)
                    {
                        selectedPath.IsVisible = true;
                        pathModel.InvalidatePlot(false);
                    }

                    foreach (var ann in _labelAnnotations)
                    {
                        if (ann.Tag == selectedPath) pathModel.Annotations.Add(ann);
                        if (ann.Tag == selectedPtt) pttModel.Annotations.Add(ann);
                    }

                };

                cb.Unchecked += (sender, args) =>
                {
                    var selectedPtt = pttModel.Series.FirstOrDefault(x => ((uint)x.Tag) == (uint)cb.Tag);
                    if (selectedPtt != null)
                    {
                        selectedPtt.IsVisible = false;
                        pttModel.InvalidatePlot(false);
                    }
                    var selectedPath = pathModel.Series.FirstOrDefault(x => x.Tag != null && ((uint)x.Tag) == (uint)cb.Tag);
                    if (selectedPath != null)
                    {
                        selectedPath.IsVisible = false;
                        pathModel.InvalidatePlot(false);
                    }

                    foreach (var ann in pttModel.Annotations.ToList())
                    {
                        if (ann.Tag == selectedPtt) pttModel.Annotations.Remove(ann);
                    }

                    foreach (var ann in pathModel.Annotations.ToList())
                    {
                        if (ann.Tag == selectedPath) pathModel.Annotations.Remove(ann);
                    }

                };

                ShowSeriesBox.Items.Add(cb);
            }
            #endregion

            if (labels != null && labels.Count > 0)
            {
                for (int i = 1; i <= labels.Count; i++) AgesListBox.Items.Add(i + ") " + labels[i-1]);
            }

        }

        private string AxisLabelKmFormat(double v)
        {
            return (v/1000).ToString("0.##");
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Рисунок PNG|*.png" };

            if (sfd.ShowDialog() == false) return;

            var fileName = sfd.FileName;

            ExportPlotToPng(pttModel, fileName);
        }

        private void PathPngExportItem_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Рисунок PNG|*.png" };
            if (sfd.ShowDialog() == false) return;
            var fileName = sfd.FileName;

            ExportPlotToPng(pathModel, fileName, 1000, 500);
        }

        private void AllPngExportItem_OnClick(object sender, RoutedEventArgs e)
        {

            var sfd = new SaveFileDialog { Filter = "Рисунок PNG|*.png" };
            if (sfd.ShowDialog() == false) return;
            var fileName = sfd.FileName;

            var pngExporter = new PngExporter();

            var pttBmpSrc = pngExporter.ExportToBitmap(pttModel);
            var pttBmp = GetBitmap(pttBmpSrc);

            var pathBmpSrc = pngExporter.ExportToBitmap(pathModel);
            var pathBmp = GetBitmap(pathBmpSrc);

            var resultImage = MergeTwoImages(pttBmp, pathBmp);

            resultImage.Save(fileName);

        }

        private void TxtExportItem_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Текстовый файл|*.txt" };
            if (sfd.ShowDialog() == false) return;
            var fileName = sfd.FileName;

            TxtExport(fileName);
        }


        private void PngAndTxtExportItem_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Текстовый файл|*.txt" };
            if (sfd.ShowDialog() == false) return;
            var fileName = sfd.FileName;

            TxtExport(fileName);

            fileName = Path.ChangeExtension(fileName, "png");

            var pngExporter = new PngExporter();
            var pttBmpSrc = pngExporter.ExportToBitmap(pttModel);
            var pttBmp = GetBitmap(pttBmpSrc);
            var pathBmpSrc = pngExporter.ExportToBitmap(pathModel);
            var pathBmp = GetBitmap(pathBmpSrc);
            var resultImage = MergeTwoImages(pttBmp, pathBmp);
            resultImage.Save(fileName);

        }


        private void PathAndCompPngExportItem_OnClick(object sender, RoutedEventArgs e)
        {

            var mofd = new OpenFileDialog
            {
                Filter = "Файлы вещественной струкутры|*.txt",
                Multiselect = true,
                Title = "Выберите файлы вещественной структуры"
            };

            if (mofd.ShowDialog() == true)
            {
                var fbd = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true,
                    SelectedPath =
                        mofd.FileNames[0].Substring(0, mofd.FileNames[0].LastIndexOf("\\", StringComparison.Ordinal) + 1)
                };


                DialogResult result = fbd.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK) return;


                var seriesToDraw = pathModel.Series.Where(x => x is LineSeries && x.IsVisible && x.Tag != null).Select(x => Convert.ToUInt32(x.Tag)).ToList();

                var oldTitle = pathModel.Title;
                
                foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
                {
                    ls.IsVisible = false;
                }
                foreach (var ann in pathModel.Annotations.ToList())
                {
                    pathModel.Annotations.Remove(ann);
                }


                foreach (var fn in mofd.FileNames)
                {
                    var ordNum = Config.Tools.ExtractNumberFromString(fn.Split(new [] {'\\'}, StringSplitOptions.RemoveEmptyEntries).Last());

                    foreach (var markInd in seriesToDraw)
                    {
                        
                        foreach (var mrk in MarkersCollection[markInd])
                        {
                            if (ordNum == Config.Tools.ExtractNumberFromString(mrk.PrnSource.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()))
                            {

                                pathModel.Title = "Время = " + (mrk.Age / 1e6).ToString("0.00") + " млн лет; Маркеры: " + seriesToDraw.Aggregate("", (current, mInd) => current + (mInd + ", "));
                                pathModel.Title = pathModel.Title.Substring(0, pathModel.Title.Length - 2);

                                InsertHeatMapToPathModel(fn, false);

                                var tempSeria = new LineSeries();
                                tempSeria.Points.Add(new DataPoint(mrk.XPosition, mrk.YPosition));
                                tempSeria.Tag = "shit";

                                var parSeria =
                                    pathModel.Series.FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == markInd.ToString()) as
                                        LineSeries;
                                if (parSeria != null)
                                {
                                    tempSeria.MarkerType = parSeria.MarkerType;
                                    tempSeria.MarkerSize = parSeria.MarkerSize;
                                    tempSeria.MarkerFill = parSeria.MarkerFill;
                                }

                                pathModel.Series.Add(tempSeria);

                            }
                        }

                    }

                    var fileName = fn.Split(new [] {'\\'}, StringSplitOptions.RemoveEmptyEntries).Last();
                    fileName = fileName.Substring(0, fileName.Length - 4)+"_markers";
                    
                    fileName = seriesToDraw.Aggregate(fileName, (current, mInd) => current + ("_" + mInd));

                    fileName += ".png";

                    var picWidth = Convert.ToInt32((pathModel.Axes[0].ActualMaximum - pathModel.Axes[0].ActualMinimum) / 1000d) + 60;
                    var picHeight = 60 +
                                    Convert.ToInt32((pathModel.Axes[1].ActualMaximum - pathModel.Axes[1].ActualMinimum)/
                                                    1000d);

                    if (picWidth < 1300)
                    {
                        var ratio = Convert.ToDouble(picWidth)/Convert.ToDouble(picHeight);
                        picWidth = 1300;
                        picHeight = Convert.ToInt32(1300d/ratio);
                    }

                    ExportPlotToPng(pathModel, fbd.SelectedPath + "\\" + fileName, picWidth, picHeight);

                    var remRange = pathModel.Series.Where(x => x.Tag != null && x.Tag.ToString() == "shit").ToList();

                    foreach (var rem in remRange)
                    {
                        pathModel.Series.Remove(rem);
                    }

                }

                foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
                {
                    ls.IsVisible = true;
                }

                pathModel.Title = oldTitle;
                pathModel.InvalidatePlot(false);

                MessageBox.Show(@"Рисунки успешно сохранены (вроде как)");
            }

            
            


        }

        #region вспомогательные функции

        private void ExportPlotToPng(PlotModel model, string path, int width = 0, int height = 0)
        {
            using (var stream = File.Create(path))
            {
                var pngExporter = new PngExporter();

                if (width > 0 && height > 0)
                {
                    pngExporter.Width = width;
                    pngExporter.Height = height;
                }

                pngExporter.Export(model, stream);
            }
        }

        private void TxtExport(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {

                file.WriteLine("ID маркера");
                file.WriteLine("Время_(лет)\tX_(м)\tY_(м)\tДавление_(Па)\tТемператуа_(C)\tНомер_породы");
                file.WriteLine("");
                foreach (var markerIndex in MarkersCollection.Keys)
                {
                    file.WriteLine(markerIndex);
                    foreach (var currentMarker in MarkersCollection[markerIndex])
                    {
                        file.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", currentMarker.Age.ToString("##.###"), currentMarker.XPosition, currentMarker.YPosition.ToString("0.00"), currentMarker.Pressure.ToString("0"), currentMarker.Temperature, currentMarker.RockId);
                    }
                    file.WriteLine("");
                }
            }
        }


        private Bitmap GetBitmap(BitmapSource source)
        {
            Bitmap bmp = new Bitmap(
              source.PixelWidth,
              source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(
              new Rectangle(Point.Empty, bmp.Size),
              ImageLockMode.WriteOnly,
              System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            source.CopyPixels(
              Int32Rect.Empty,
              data.Scan0,
              data.Height * data.Stride,
              data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        public Bitmap MergeTwoImages(Image firstImage, Image secondImage)
        {
            if (firstImage == null)
            {
                throw new ArgumentNullException("firstImage");
            }

            if (secondImage == null)
            {
                throw new ArgumentNullException("secondImage");
            }

            int outputImageWidth = firstImage.Width > secondImage.Width ? firstImage.Width : secondImage.Width;

            int outputImageHeight = firstImage.Height + secondImage.Height + 1;

            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.DrawImage(firstImage, new Rectangle(new Point(), firstImage.Size),
                    new Rectangle(new Point(), firstImage.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(secondImage, new Rectangle(new Point(0, firstImage.Height + 1), secondImage.Size),
                    new Rectangle(new Point(), secondImage.Size), GraphicsUnit.Pixel);
            }

            return outputImage;
        }
        #endregion

        private void MaterialCompositionItem_OnClick(object sender, RoutedEventArgs e)
        {
            var mofd = new OpenFileDialog
            {
                Filter = "Файлы вещественной струкутры|*.txt",
                Multiselect = false,
                Title = "Выберите файл вещественной структуры"
            };


            if (!String.IsNullOrEmpty(PrnFilesPath)) mofd.InitialDirectory = PrnFilesPath;

            if (mofd.ShowDialog() == true)
            {
                InsertHeatMapToPathModel(mofd.FileName);
            }
        }

        private void InsertHeatMapToPathModel(string curFile, bool temperature = false)
        {
            var arr = I2VISOutputReader.GetRocksArrayFromTxt(curFile);

            var stX = 0;
            var endX = arr.GetLength(1);
            var stZ = 0;
            var endZ = arr.GetLength(0);

            var hs = new HeatMapSeries
            {
                X0 = stX * 1000,
                X1 = Config.Tools.ParseOrDefaultInt(XSizeBox.Text, 4000) * 1000,
                Y0 = stZ * 1000,
                Y1 = Config.Tools.ParseOrDefaultInt(YSizeBox.Text, 400) * 1000,
                Data = new double[endX - stX, endZ - stZ]
            };



            int ii = 0;
            for (int i = stZ; i < endZ; i++)
            {
                int jj = 0;
                for (int j = stX; j < endX; j++)
                {
                    hs.Data[jj, ii] = arr[i, j];
                    jj++;
                }
                ii++;
            }

                while (pathModel.Series.FirstOrDefault(x => x is HeatMapSeries) != null)
                {
                    pathModel.Series.Remove(pathModel.Series.FirstOrDefault(x => x is HeatMapSeries));
                }
                
            
                

            pathModel.Series.Insert(0, hs);

            #region отрисовка температуры
            if (temperature)
            {
                var cInd = curFile.LastIndexOf("c", StringComparison.Ordinal);
                if (cInd == -1) return;
                var tfilename = curFile.Remove(cInd, 1).Insert(cInd, "t");

                if (File.Exists(tfilename))
                {

                    var tarr = I2VISOutputReader.GetTemperatureArrayFromTxt(tfilename);

                    var t_stX = 0;
                    var t_endX = tarr.GetLength(1);
                    var t_stZ = 0;
                    var t_endZ = tarr.GetLength(0);

                    if ((tarr.GetLength(0) != arr.GetLength(0) ||
                        tarr.GetLength(1) != arr.GetLength(1)) && InterpolateTemperatureCb.IsChecked == true)
                    {
                        tarr = Config.Tools.InterpolateArray(tarr, arr.GetLength(0), arr.GetLength(1));
                    } 

                    var colorTMap = new double[tarr.GetLength(1), tarr.GetLength(0)];
                    for (int i = 0; i < colorTMap.GetLength(0); i++)
                    {
                        for (int j = 0; j < colorTMap.GetLength(1); j++)
                        {
                            colorTMap[i, j] = ((LinearColorAxis)pathModel.Axes.FirstOrDefault(x => x is LinearColorAxis))
                                            .Palette.Colors.Count - 2;
                        }
                    }


                    double tc, tc_prev_h, tc_prev_v;
                    Dictionary<double, double> isoterms = Config.GraphConfig.Instace.Isoterms.ToDictionary<double, double, double>(isoterm => isoterm, isoterm => 0);

                    for (int i = 0; i < tarr.GetLength(0); i++)
                    {
                        for (int j = 0; j < tarr.GetLength(1); j++)
                        {
                            tc = tarr[i, j];
                            tc_prev_v = (i > 0) ? tarr[i - 1, j] : tc;
                            tc_prev_h = (j > 0) ? tarr[i, j - 1] : tc;


                            foreach (double isoterm in isoterms.Keys.ToList())
                            {
                                if ((isoterm > tc_prev_v && isoterm <= tc) ||
                                     (isoterm < tc_prev_v && isoterm >= tc) ||
                                     (isoterm > tc_prev_h && isoterm <= tc) ||
                                     (isoterm < tc_prev_h && isoterm >= tc))
                                {

                                    colorTMap[j, i] =
                                        ((LinearColorAxis)pathModel.Axes.FirstOrDefault(x => x is LinearColorAxis))
                                            .Palette.Colors.Count - 1;

                                    if (j ==
                                        Convert.ToInt32((pathModel.Axes[0].ActualMinimum * (double)tarr.GetLength(1))/
                                                        (Config.Tools.ParseOrDefaultInt(XSizeBox.Text, 4000)*1000d)))
                                    {
                                        var isotermAnn = new PointAnnotation
                                        {
                                            X = pathModel.Axes[0].ActualMinimum + 15000,
                                            Y = ((Config.Tools.ParseOrDefaultInt(YSizeBox.Text, 400)*1000d)/
                                                 tarr.GetLength(0))*i ,
                                            Text = isoterm.ToString(),
                                            Tag = "isotermAnn",
                                            Layer = AnnotationLayer.AboveSeries,
                                            TextColor = OxyColors.White,
                                            Fill = OxyColors.Transparent,
                                            TextMargin = -5,
                                            
                                        };

                                        pathModel.Annotations.Add(isotermAnn);

                                    }

                                }

                            }

                        }
                    }



                    var ths = new HeatMapSeries
                    {
                        X0 = stX * 1000,
                        X1 = Config.Tools.ParseOrDefaultInt(XSizeBox.Text, 4000) * 1000,
                        Y0 = stZ * 1000,
                        Y1 = Config.Tools.ParseOrDefaultInt(YSizeBox.Text, 400) * 1000,
                        Data = colorTMap
                    };

                    

                    pathModel.Series.Insert(1, ths);


                }
            }
            #endregion


            pathModel.InvalidatePlot(false);
        }


        

        private void TransparancySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }

        private void FitScaleButton_Click(object sender, RoutedEventArgs e)
        {
            if (pathModel == null || pathModel.Axes == null || pathModel.Axes.Count < 2) return;

            var axeX = pathModel.Axes[0] as LinearAxis;
            var axeY = pathModel.Axes[1] as LinearAxis;

            axeX.Zoom(axeY.Scale);

            pathModel.InvalidatePlot(false);
        }

        private void PTtWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            TrajTypeBox.ItemsSource = Enum.GetValues(typeof(LineStyle)).Cast<LineStyle>();
            /*
            TrandsColorList.ItemsSource = MarkersCollection.Keys;
            TrandsColorList.Items.Refresh();
             */

            foreach (var tKey in MarkersCollection.Keys)
            {
                TrandsColorList.Items.Add(tKey);
            }
         }

        private void TrajTypeBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pathModel == null) return;

            foreach (var ls in pathModel.Series.Where(x=>x is LineSeries))
            {
                ((LineSeries) ls).LineStyle = (LineStyle)TrajTypeBox.SelectedItem ;
            }

            pathModel.InvalidatePlot(false);
        }

        private void TrajTypeBox_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (pathModel == null) return;
            var cAxe = pathModel.Axes.FirstOrDefault(x => x is LinearColorAxis) as LinearColorAxis;
            if (cAxe == null) return;

            var val = Convert.ToByte(TransparancySlider.Value);

            for (int i = 0; i < cAxe.Palette.Colors.Count; i++)
            {
                var cl = cAxe.Palette.Colors[i];
                cAxe.Palette.Colors[i] = OxyColor.FromArgb(val, cl.R, cl.G, cl.B);
            }

            var hs = pathModel.Series.FirstOrDefault(x => x is HeatMapSeries) as HeatMapSeries;
            if (hs != null) hs.Invalidate();
            pathModel.InvalidatePlot(false);
        }

        private void MarkerSizeSlider_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (pathModel == null) return;

            var val = Convert.ToInt32(MarkerSizeSlider.Value);

            foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
            {
                ((LineSeries)ls).MarkerSize = val;
            }

            pathModel.InvalidatePlot(false);
        }


        private void SHowGridCb_OnChecked(object sender, RoutedEventArgs e)
        {
            if (pathModel == null) return;

            foreach (var axe in pathModel.Axes.Where(x=>x is LinearAxis))
            {
                axe.MajorGridlineStyle = LineStyle.Solid;
                axe.MinorGridlineStyle = LineStyle.Dash;
            }

            pathModel.InvalidatePlot(false);
        }

        private void SHowGridCb_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (pathModel == null) return;

            foreach (var axe in pathModel.Axes.Where(x => x is LinearAxis))
            {
                axe.MajorGridlineStyle = LineStyle.None;
                axe.MinorGridlineStyle = LineStyle.None;
            }

            pathModel.InvalidatePlot(false);
        }

        private void TrandsColorList_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

            var selItem = TrandsColorList.SelectedItem;
            if (selItem is ColorPicker) return;


            ColorPicker cPicker = new ColorPicker();

            cPicker.SelectedColorChanged += (sen, ev) =>
            {
                var cl = OxyColor.FromArgb(255, cPicker.SelectedColor.Value.R, cPicker.SelectedColor.Value.G,
                    cPicker.SelectedColor.Value.B);

                var lsPath = pathModel.Series.FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == selItem.ToString()) as LineSeries;
                if (lsPath != null)
                {
                    lsPath.Color = cl;
                    lsPath.MarkerFill = cl;
                    pathModel.InvalidatePlot(false);
                }
                

                var pttPath = pttModel.Series.FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == selItem.ToString()) as LineSeries;
                if (pttPath != null)
                {
                    pttPath.Color = cl;
                    pttPath.MarkerFill = cl;
                    pttModel.InvalidatePlot(false);
                }
                

                TrandsColorList.Items.Remove(cPicker);
            };

            TrandsColorList.Items.Insert(TrandsColorList.SelectedIndex + 1, cPicker);

            

            //Grid.SetRow(cPicker, 1);
            //Grid.SetColumn(cPicker, 2);
            //MainGrid.Children.Add(cPicker);

        }

        private void MarkerThicknessSlider_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (pathModel == null) return;

            var val = Convert.ToInt32(MarkerThicknessSlider.Value);

            foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
            {
                ((LineSeries)ls).StrokeThickness = val;
            }

            pathModel.InvalidatePlot(false);
        }


        private void FullTxtItem_OnClick(object sender, RoutedEventArgs e)
        {

            var mofd = new OpenFileDialog
            {
                Filter = "Файлы вещественной струкутры|*.txt",
                Multiselect = false,
                Title = "Выберите файл вещественной структуры"
            };

           


            if (!String.IsNullOrEmpty(PrnFilesPath)) mofd.InitialDirectory = PrnFilesPath;

            if (mofd.ShowDialog() == true)
            {
                InsertHeatMapToPathModel(mofd.FileName);

                var cInd = mofd.FileName.LastIndexOf("c", StringComparison.Ordinal);
                if (cInd == -1) return;
                var tfilename = mofd.FileName.Remove(cInd, 1).Insert(cInd, "t");

                if (!File.Exists(tfilename))
                {
                    MessageBox.Show("К данному файлу вещественной структуры не найден температурный! Температурный файл должен находиться в той же папке.");
                    return;
                }

                //var tarray = Config.Tools.Transpose(I2VISOutputReader.GetTemperatureArrayFromTxt(tfilename));
                

                ////todo опционализировать
                //double x0 = 0;
                //double x1 = 400000;
                //double y0 = 0;
                //double y1 = 4000000;


                ////Func<double, double, double> peaks = (x, y) => x*x + y*y;
                //var xx = ArrayBuilder.CreateVector(x0, x1, tarray.GetLength(1));
                //var yy = ArrayBuilder.CreateVector(y0, y1, tarray.GetLength(0));
                ////var peaksData = ArrayBuilder.Evaluate(peaks, xx, yy);

                //var cs = new ContourSeries
                //{
                //    Color = OxyColors.White,
                //    LabelBackground = OxyColors.Transparent,
                //    ColumnCoordinates = yy,
                //    RowCoordinates = xx,
                //    Data = tarray,
                //    ContourLevels = GraphConfig.Instace.Isoterms.ToArray()
                //};

                //pathModel.Series.Add(cs);

                var stream = GraphTools.StreamFromTemperature(tfilename);
                var oi = new OxyImage(stream.ToArray());
                var imageAnn = new ImageAnnotation
                {
                    ImageSource = oi,
                    X = new PlotLength(Config.Tools.ParseOrDefaultInt(XSizeBox.Text, 4000) * 500, PlotLengthUnit.Data),
                    Y = new PlotLength(Config.Tools.ParseOrDefaultInt(YSizeBox.Text, 400) * 500, PlotLengthUnit.Data),
                    Width = new PlotLength(Config.Tools.ParseOrDefaultInt(XSizeBox.Text, 4000) * 1000, PlotLengthUnit.Data),
                    Height = new PlotLength(Config.Tools.ParseOrDefaultInt(YSizeBox.Text, 400) * 1000, PlotLengthUnit.Data),
                    Interpolate = true,
                    Layer = AnnotationLayer.AboveSeries
                };

                pathModel.Annotations.Add(imageAnn);

            } 

            

        }

        private void PathTempAndCompPngExportItem_OnClick(object sender, RoutedEventArgs e)
        {
            var mofd = new OpenFileDialog
            {
                Filter = "Файлы вещественной струкутры|*.txt",
                Multiselect = true,
                Title = "Выберите файлы вещественной структуры"
            };

            if (mofd.ShowDialog() == true)
            {
                var fbd = new FolderBrowserDialog
                {
                    ShowNewFolderButton = true,
                    SelectedPath =
                        mofd.FileNames[0].Substring(0, mofd.FileNames[0].LastIndexOf("\\", StringComparison.Ordinal) + 1)
                };


                DialogResult result = fbd.ShowDialog();
                if (result != System.Windows.Forms.DialogResult.OK) return;


                var seriesToDraw = pathModel.Series.Where(x => x is LineSeries && x.IsVisible && x.Tag != null).Select(x => Convert.ToUInt32(x.Tag)).ToList();

                var oldTitle = pathModel.Title;

                foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
                {
                    ls.IsVisible = false;
                }
                foreach (var ann in pathModel.Annotations.ToList())
                {
                    pathModel.Annotations.Remove(ann);
                }


                foreach (var fn in mofd.FileNames)
                {
                    var ordNum = Config.Tools.ExtractNumberFromString(fn.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last());

                    foreach (var markInd in seriesToDraw)
                    {

                        foreach (var mrk in MarkersCollection[markInd])
                        {
                            if (ordNum == Config.Tools.ExtractNumberFromString(mrk.PrnSource.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last()))
                            {

                                pathModel.Title = "Время = " + (mrk.Age / 1e6).ToString("0.00") + " млн лет; Маркеры: " + seriesToDraw.Aggregate("", (current, mInd) => current + (mInd + ", "));
                                pathModel.Title = pathModel.Title.Substring(0, pathModel.Title.Length - 2);

                                InsertHeatMapToPathModel(fn, true);

                                var tempSeria = new LineSeries();
                                tempSeria.Points.Add(new DataPoint(mrk.XPosition, mrk.YPosition));
                                tempSeria.Tag = "shit";

                                var parSeria =
                                    pathModel.Series.FirstOrDefault(x => x.Tag != null && x.Tag.ToString() == markInd.ToString()) as
                                        LineSeries;
                                if (parSeria != null)
                                {
                                    tempSeria.MarkerType = parSeria.MarkerType;
                                    tempSeria.MarkerSize = parSeria.MarkerSize;
                                    tempSeria.MarkerFill = parSeria.MarkerFill;
                                }
                                
                                pathModel.Series.Add(tempSeria);

                            }
                        }

                    }

                    var fileName = fn.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();
                    fileName = fileName.Substring(0, fileName.Length - 4) + "_markers";

                    fileName = seriesToDraw.Aggregate(fileName, (current, mInd) => current + ("_" + mInd));

                    fileName += ".png";

                    var picWidth = Convert.ToInt32((pathModel.Axes[0].ActualMaximum - pathModel.Axes[0].ActualMinimum) / 1000d) + 60;
                    var picHeight = 60 +
                                    Convert.ToInt32((pathModel.Axes[1].ActualMaximum - pathModel.Axes[1].ActualMinimum) /
                                                    1000d);

                    if (picWidth < 1300)
                    {
                        var ratio = Convert.ToDouble(picWidth) / Convert.ToDouble(picHeight);
                        picWidth = 1300;
                        picHeight = Convert.ToInt32(1300d / ratio);
                    }

                    ExportPlotToPng(pathModel, fbd.SelectedPath + "\\" + fileName, picWidth, picHeight);

                    var remRange = pathModel.Series.Where(x => x.Tag != null && x.Tag.ToString() == "shit").ToList();

                    foreach (var rem in remRange)
                    {
                        pathModel.Series.Remove(rem);
                    }

                    var annRemRange = pathModel.Annotations.Where(x => x.Tag != null && x.Tag.ToString() == "isotermAnn").ToList();
                    foreach (var annRem in annRemRange)
                    {
                        pathModel.Annotations.Remove(annRem);
                    }

                }

                foreach (var ls in pathModel.Series.Where(x => x is LineSeries))
                {
                    ls.IsVisible = true;
                }

                pathModel.Title = oldTitle;
                pathModel.InvalidatePlot(false);

                MessageBox.Show(@"Рисунки успешно сохранены (вроде как)");
            }

        }

        private void ShowPhases_OnChecked(object sender, RoutedEventArgs e)
        {
            if (pttModel.Series.Any(x => x.Tag.ToString() == "phase"))
            {
                foreach (var seria in pttModel.Series)
                {
                    if (seria.Tag.ToString() == "phase") seria.IsVisible = true;
                }
                pttModel.InvalidatePlot(false);
                return;
            }

            foreach (var phase in GeologyConfig.Instace.Phases)
            {
                var phaseSeria = new LineSeries
                {
                    Tag = "phase",
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 2,
                    Smooth = true
                };

                if (phase.Color.HasValue)
                {
                    phaseSeria.Color = OxyColor.FromArgb(phase.Color.Value.A, phase.Color.Value.R, phase.Color.Value.G,
                        phase.Color.Value.B);
                }
                else
                {
                    phaseSeria.Color = OxyColors.Black;
                }

                foreach (var phPoint in phase.Points)
                {
                    phaseSeria.Points.Add(new DataPoint(phPoint.T, phPoint.P));
                }
                pttModel.Series.Insert(0, phaseSeria);
            }

            pttModel.InvalidatePlot(false);
        }

        private void ShowPhases_OnUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var seria in pttModel.Series)
            {
                if (seria.Tag.ToString() == "phase") seria.IsVisible = false;
            }
            pttModel.InvalidatePlot(false);
        }

        private void SHowFacies_OnChecked(object sender, RoutedEventArgs e)
        {

            if (pttModel.Series.Any(x => x.Tag.ToString() == "facie"))
            {
                foreach (var seria in pttModel.Series)
                {
                    if (seria.Tag.ToString() == "facie") seria.IsVisible = true;
                }
                pttModel.InvalidatePlot(false);
                return;
            }

            var rnd = new Random();
            foreach (var facie in GeologyConfig.Instace.MetamorphicFacies)
            {
                var facieSeria = new AreaSeries
                {
                    Tag = "facie",
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = 8
                };

                if (facie.Color.HasValue)
                {
                    facieSeria.Fill = OxyColor.FromArgb(facie.Color.Value.A, facie.Color.Value.R, facie.Color.Value.G,
                        facie.Color.Value.B);
                }
                else
                {
                    var colBts = new byte[3];
                    rnd.NextBytes(colBts);
                    facieSeria.Fill = OxyColor.FromArgb(122, colBts[0], colBts[1], colBts[2] );
                }

                facieSeria.Color = facieSeria.Fill;
                facieSeria.Color2 = facieSeria.Fill;
                
                foreach (var fPoint in facie.Points)
                {
                    facieSeria.Points.Add(new DataPoint(fPoint.T, fPoint.P));
                }
                pttModel.Series.Insert(0, facieSeria);
                //pttModel.Series.Add(facieSeria);
            }

            pttModel.InvalidatePlot(false);
           

            /*
            pttModel.MouseUp += (o, args) =>
            {
                var pt = pttModel.Axes[0].InverseTransform(args.Position.X, args.Position.Y, pttModel.Axes[1]);

                var facie = GeologyConfig.Instace.GetFacie(new PTPoint(pt.Y, pt.X));

                var msg = (facie == null) ? "None" : facie.Name;
                MessageBox.Show(msg);

            };
            */
        }

        private void SHowFacies_OnUnchecked(object sender, RoutedEventArgs e)
        {
            foreach (var seria in pttModel.Series)
            {
                if (seria.Tag.ToString() == "facie") seria.IsVisible = false;
            }
            pttModel.InvalidatePlot(false);
        }
    }
}
