using System;
using System.Collections.Generic;
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
using I2VISTools.InitClasses;
using I2VISTools.Subclasses;
using MathNet.Numerics.Differentiation;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Renci.SshNet.Security;
using Cursors = System.Windows.Input.Cursors;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineAnnotation = OxyPlot.Annotations.LineAnnotation;
using LineSeries = OxyPlot.Series.LineSeries;
using MessageBox = System.Windows.MessageBox;
using PointAnnotation = OxyPlot.Annotations.PointAnnotation;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;


namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for GeothermWindow.xaml
    /// </summary>
    public partial class GeothermWindow : Window
    {

        private PlotModel model;
        private LinearAxis axeT;
        private LinearAxis axeH;

        private LineSeries mantleSeries;
        private LineSeries _continentalSeries;
        private LineSeries _continentalCrustSeries;

        private FunctionSeries oceanicSerie;
        private Func<double, double> oceanicFunc;

        private DataPoint _dragPoint = DataPoint.Undefined;
        private DataPoint _startPt1;
        private DataPoint _startPt2;

        private LineAnnotation _continentalLithosphere;

        private int tm = 1388;
        private double t = 4e7;

        private double Hc = 50000;
        private double Tc = 500;

        private const double MAX_DEPTH = 400000;
        private const double MIN_MANTLE_T = 1200;
        private const double MAX_MANTLE_T = 1900;

        // точка условного пересечения адиабаты и океанической геотермы
        private DataPoint PtOcEnd;

        public GeothermWindow()
        {
            InitializeComponent();

            model = new PlotModel();

            axeH = new LinearAxis
            {
                Title = "Температура (°C)",
                Position = AxisPosition.Top,
                //StartPosition = 1,
                //EndPosition = 0,
                Key = "xAxis",
                Maximum = 1500,
                AbsoluteMaximum = 2200,
                AbsoluteMinimum = -5,
                MajorGridlineStyle = LineStyle.Solid
               // MinorGridlineStyle = LineStyle.Dash
            };
            
            axeT = new LinearAxis
            {
                Title = "Глубина (м)",
                Position = AxisPosition.Left,
                Key = "yAxis",
                StartPosition = 1,
                EndPosition = 0,
                Maximum = MAX_DEPTH,
                AbsoluteMaximum = MAX_DEPTH,
                AbsoluteMinimum = -10000,
                MajorGridlineStyle = LineStyle.Solid
                //MinorGridlineStyle = LineStyle.Dash
            };

            model.LegendPosition = LegendPosition.BottomLeft;

            model.Axes.Add(axeT);
            model.Axes.Add(axeH);

            mantleSeries = new LineSeries();
            mantleSeries.Points.Add(new DataPoint(1320, 0));
            mantleSeries.Points.Add(new DataPoint(1493, MAX_DEPTH));
            mantleSeries.Title = "Мантийная адиабата";
            mantleSeries.Color = OxyColors.Purple;
            mantleSeries.StrokeThickness = 3;
            

            _continentalCrustSeries = new LineSeries();
            _continentalCrustSeries.Points.Add(new DataPoint(0, 0));
            _continentalCrustSeries.Points.Add(new DataPoint(Tc, Hc));
            _continentalCrustSeries.Color = OxyColors.Gray;
            _continentalCrustSeries.StrokeThickness = 3;

            _continentalSeries = new LineSeries();
            _continentalSeries.Points.Add(new DataPoint(Tc,Hc));
            _continentalSeries.Points.Add(new DataPoint(1300, 160000));
            _continentalSeries.Title = "Континентальная геотерма";
            _continentalSeries.Color = _continentalCrustSeries.Color;
            _continentalSeries.StrokeThickness = 3;

            #region события перетаскивания мантийной адиабаты

            mantleSeries.MouseDown += (sender, args) =>
            {
                if (args.ChangedButton == OxyMouseButton.Left && args.IsControlDown)
                {
                    _dragPoint = mantleSeries.InverseTransform(args.Position);
                    _startPt1 = mantleSeries.Points[0];
                    _startPt2 = mantleSeries.Points[1];
                    GeothermsPlotView.Cursor = Cursors.Hand;
                    args.Handled = true;
                }
                 
            };

            mantleSeries.MouseMove += (sender, args) =>
            {
                
                if (_dragPoint.IsDefined() && args.IsControlDown)
                {
                    var curPos = mantleSeries.InverseTransform(args.Position);

                    var m0 = _startPt1.X + (curPos.X - _dragPoint.X);

                    if (m0 >= MIN_MANTLE_T && m0 <= MAX_MANTLE_T)
                    {
                        mantleSeries.Points[0] = new DataPoint(m0, mantleSeries.Points[0].Y);
                        mantleSeries.Points[1] = new DataPoint(_startPt2.X + (curPos.X - _dragPoint.X), mantleSeries.Points[1].Y);

                        model.InvalidatePlot(false);
                    }

                    args.Handled = true; 
                    return;
                }
                GeothermsPlotView.Cursor = Cursors.Arrow;
                args.Handled = true;    

            };

            mantleSeries.MouseUp += (sender, args) =>
            {
                _dragPoint = DataPoint.Undefined;
                args.Handled = true;
                GeothermsPlotView.Cursor = Cursors.Arrow;

                //var tm = mantleSeries.Points[1].X - 105;
                //oceanicFunc = y => MathNet.Numerics.SpecialFunctions.Erf((y / (2 * Math.Sqrt(1e-6 * 4e7 * 365 * 24 * 60 * 60)))) * tm;
                //Func<double, double> yFunc = d => d;

                //model.Series.Remove(oceanicSerie);
                //oceanicSerie = new FunctionSeries(oceanicFunc, yFunc, 0, 400000, 1000, "Океаническая кривая");
                //model.Series.Insert(0, oceanicSerie);
                //model.InvalidatePlot(false);

                AdjustGeothermToMantle();
                ContinueContinentToAdiabate();
            };

            #endregion

            #region События перетаскивания континентальной геотермы
            
            _continentalSeries.MouseDown += (sender, args) =>
            {
                if (args.IsControlDown)
                {
                    _dragPoint = _continentalSeries.InverseTransform(args.Position);
                    _startPt2 = _continentalSeries.Points[1];
                    args.Handled = true;
                }
            };

            _continentalSeries.MouseMove += (sender, args) =>
            {
                if (_dragPoint.IsDefined() && args.IsControlDown)
                {
                    if (_continentalSeries.Points.Count > 2) _continentalSeries.Points.RemoveAt(2);

                    var curPos = mantleSeries.InverseTransform(args.Position);

                    _continentalSeries.Points[1] = new DataPoint(_continentalSeries.Points[1].X, _startPt2.Y + (curPos.Y - _dragPoint.Y));

                    model.InvalidatePlot(false);

                    args.Handled = true;
                    return;
                }
                GeothermsPlotView.Cursor = Cursors.Arrow;
                args.Handled = true;    
            };

            _continentalSeries.MouseUp += (sender, args) =>
            {
                _dragPoint = DataPoint.Undefined;
                ContinueContinentToAdiabate();
                GeothermsPlotView.Cursor = Cursors.Arrow;
                args.Handled = true;
            };

            #endregion

            GeothermsPlotView.Model = model;

        }


        private void GeothermsPlotView_OnLoaded(object sender, RoutedEventArgs e)
        {

           // oceanicFunc = y => MathNet.Numerics.SpecialFunctions.Erf((y/(2*Math.Sqrt(1e-6*t*365*24*60*60))))*tm;

           //// oceanicFunc = y => MathNet.Numerics.SpecialFunctions.Erfc(y);

           // Func<double, double> yFunc = d => d;
           // //oceanicSerie = new FunctionSeries(oceanicFunc, 0, 300000, 1000);
           // oceanicSerie = new FunctionSeries(oceanicFunc, yFunc, 0, MAX_DEPTH, 1000, "Океаническая кривая (" + (t/1e6).ToString("0.#") + " Ma)" );
           // oceanicSerie.Color = OxyColor.FromRgb(0,255,0);
           // oceanicSerie.StrokeThickness = 3;
           // //oceanicSerie.XAxisKey = "xAxis";
           // //oceanicSerie.YAxisKey = "yAxis";
            oceanicSerie = new FunctionSeries();
            AdjustGeothermToMantle();

           // model.Series.Add(oceanicSerie);
            model.Series.Add(mantleSeries);
            model.Series.Add(_continentalCrustSeries);
            model.Series.Add(_continentalSeries);
            

            var lithTermBoundary = new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = 1300
                
            };

            _continentalLithosphere = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = 160000
            };
            
            model.Annotations.Add(lithTermBoundary);
            model.Annotations.Add(_continentalLithosphere);

            ContinueContinentToAdiabate();

            //model.InvalidatePlot(false);
        }

        /// <summary>
        /// Определить расстояние от функции f2 до функции f1 в точке x по оси y
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private double FunctDistance(Func<double, double> f1, Func<double,double> f2, double x )
        {
            return Math.Abs(f2(x) - f1(x)) ;
        }

        

        private List<double> FunctDistances(Func<double, double> f1, Func<double, double> f2, double x1, double x2, double dx)
        {
            if (x2 < x1) return null;

            var res = new List<double>();

            for (double i = x1; i < x2; i+=dx)
            {
                res.Add(FunctDistance(f1, f2, i)); 
            }
            return res;
        }

        private Func<double, double> GetLinearFuncByTwoPoints(DataPoint p1, DataPoint p2)
        {
            var k = (p1.Y - p2.Y) / (p1.X - p2.X);
            var b = p1.Y - k * p1.X;

            return x => k * x + b;
        } 

        private void ApplyAgeButton_Click(object sender, RoutedEventArgs e)
        {
            var boxvalue = Config.Tools.ParseOrDefaultInt(AgeBox.Text);
            var thickness = Config.Tools.ParseOrDefaultDouble(CrustThicknessBox.Text);
            var tMoho = Config.Tools.ParseOrDefaultDouble(MohoBox.Text);

            if (tMoho <= 0 || tMoho > 1000)
            {
                MessageBox.Show("Температура подошвы коры некорректная!");
                MohoBox.Text = "600";
                return;
            }

            if (thickness <= 0 || thickness > 100)
            {
                MessageBox.Show("Кора мощностью " + thickness + "км ? Не может быть такого!");
                CrustThicknessBox.Text = "40";
                return;
            }

            if (boxvalue <= 0)
            {
                MessageBox.Show("Возраст не может быть нулевым!");
                AgeBox.Text = (t / 1e6).ToString("0.#");
                return;
            }
            if (boxvalue > 200)
            {
                MessageBox.Show("Слишком большой возраст. Такого не бывает!");
                AgeBox.Text = (t/1e6).ToString("0.#");
                return;
            }


            Hc = thickness*1000;
            Tc = tMoho;

            _continentalCrustSeries.Points[1] = new DataPoint(Tc, Hc);

            _continentalSeries.Points[0] = new DataPoint(Tc, Hc);
            model.InvalidatePlot(false);

            t = boxvalue*1e6;

            AdjustGeothermToMantle();
            ContinueContinentToAdiabate();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Config.Tools.OnlyNumeric(e.Text);
        }

        private void AdjustGeothermToMantle()
        {
            var x1 = mantleSeries.Points[0].Y;
            var x2 = mantleSeries.Points[1].Y;
            var y1 = mantleSeries.Points[0].X;
            var y2 = mantleSeries.Points[1].X;

            var k = (y1 - y2) / (x1 - x2);
            var b = y1 - k * x1;

            Func<double, double> adiabateFunc = x => k * x + b;

            for (double tt = y1; tt <= y2; tt++)
            {
                var curTm = tt;
                oceanicFunc = y => MathNet.Numerics.SpecialFunctions.Erf((y / (2 * Math.Sqrt(1e-6 * t * 365 * 24 * 60 * 60)))) * curTm;

                var distances = FunctDistances(oceanicFunc, adiabateFunc, 0, MAX_DEPTH, 1000);
                var minDist = distances.Min();
                if (minDist <= 1)
                {

                    //находим точку условного пересечения (самую близкую к адиабате)
                    var H_oceanEnd = distances.IndexOf(minDist) * 1000;

                    PtOcEnd = new DataPoint(oceanicFunc(H_oceanEnd), H_oceanEnd);

                    tm = Convert.ToInt32(tt);
                    Func<double, double> yFunc = d => d;
                    model.Series.Remove(oceanicSerie);
                    oceanicSerie = new FunctionSeries(oceanicFunc, yFunc, 0, MAX_DEPTH, 1000, "Океаническая геотерма (" + (t / 1e6).ToString("0.#") + " млн лет)");
                    oceanicSerie.Color = OxyColor.FromRgb(0, 255, 0);
                    oceanicSerie.StrokeThickness = 3;
                    model.Series.Insert(0, oceanicSerie);
                    model.InvalidatePlot(false);
                    break;
                }
            }
        }


        private DataPoint ContAdiabateIntersection(double hlc, double tlc)
        {
            if (tlc == 0) return DataPoint.Undefined;

            var tg0 = _continentalSeries.Points[0].X;
            var hg0 = _continentalSeries.Points[0].Y;

            var ta1 = mantleSeries.Points[0].X;
            var ta2 = mantleSeries.Points[1].X;

            //var x = ((MAX_DEPTH/(ta2 - ta1))*ta1)/(MAX_DEPTH/(ta2 - ta1) - (hlc - hg0) / (tlc - tg0) );
            //var y = (hlc - hg0) / (tlc - tg0) * x;

            var k1 = MAX_DEPTH/(ta2 - ta1);
            var k2 = (hlc - hg0)/(tlc - tg0);

            var x = (ta1*k1 + hlc - k2*tlc)/(k1 - k2);
            var y = k1*(x - ta1);

            return new DataPoint(x,y);
        }

        private void ContinueContinentToAdiabate()
        {
            var hlc = _continentalSeries.Points[1].Y;
            var tlc = _continentalSeries.Points[1].X;

            if (_continentalSeries.Points.Count > 2) _continentalSeries.Points.RemoveAt(2);

            _continentalSeries.Points.Add(ContAdiabateIntersection(hlc, tlc));
            _continentalLithosphere.Y = hlc;

            model.InvalidatePlot(false);
        }

        private void OpenGeothermFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Температурные файлы модели (.txt)|*.txt|Экспортированные пользователем файлы|*.txt*"
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            if (ofd.FilterIndex == 1)
            {

                var tFile = ofd.FileName;
                if (tFile == null) return;

                var indOfT = tFile.Substring(0, tFile.Length - 4).LastIndexOf("t", StringComparison.Ordinal);
                if (indOfT == -1)
                {
                    MessageBox.Show("Некорректное название файла!");
                    return;
                }

                var cFile = tFile;
                cFile = cFile.Remove(indOfT, 1);
                cFile = cFile.Insert(indOfT, "c");

                if (!File.Exists(cFile))
                {
                    MessageBox.Show(
                        "К файлу температуры не найден соответствующий файл строения! Он должен располагаться в той же папке.");
                    return;
                }

                var gfw = new GeothermFileWindow(tFile, cFile);
                if (gfw.ShowDialog() == true)
                {
                    foreach (var serie in gfw.ThermSeries)
                    {
                        model.Series.Add(serie);
                        serie.IsVisible = (ShowUserGeotherms.IsChecked == true);
                    }    
                    model.InvalidatePlot(false);
                }
        }

            if (ofd.FilterIndex == 2)
            {
                
            }
            

        }

        private void ShowGeotherms_OnChecked(object sender, RoutedEventArgs e)
        {
            if (oceanicSerie != null) oceanicSerie.IsVisible = true;
            if (_continentalCrustSeries != null) _continentalCrustSeries.IsVisible = true;
            if (_continentalSeries != null) _continentalSeries.IsVisible = true;
            if (mantleSeries != null) mantleSeries.IsVisible = true;

            if (model != null) model.InvalidatePlot(false);
        }

        private void ShowGeotherms_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (oceanicSerie != null) oceanicSerie.IsVisible = false;
            if (_continentalCrustSeries != null) _continentalCrustSeries.IsVisible = false;
            if (_continentalSeries != null) _continentalSeries.IsVisible = false;
            if (mantleSeries != null) mantleSeries.IsVisible = false;

            if (model != null) model.InvalidatePlot(false);
        }

        private void ShowUserGeotherms_OnChecked(object sender, RoutedEventArgs e)
        {
            if (model == null) return;

            foreach (var serie in model.Series)
            {
                if (serie.Title != null && serie.Title.Contains("UserData")) serie.IsVisible = true;
            }

            model.InvalidatePlot(false);
        }

        private void ShowUserGeotherms_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (model == null) return;

            foreach (var serie in model.Series)
            {
                if (serie.Title != null && serie.Title.Contains("UserData")) serie.IsVisible = false;
            }

            model.InvalidatePlot(false);
        }

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

        private void ExportPicButton_OnClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Рисунок PNG|*.png" };

            if (sfd.ShowDialog() == false) return;

            var fileName = sfd.FileName;

            ExportPlotToPng(model, fileName);
        }

        private void ApproximateButton_OnClick(object sender, RoutedEventArgs e)
        {
            int deltaT = Config.Tools.ParseOrDefaultInt(DeltaBox.Text);
            ApproximateOcenicSerie(deltaT);
            


        }

        private LineSeries approxSerie;

        // точка глубины конт. коры
        private DataPoint staticPoint = new DataPoint();

        private void ApproximateOcenicSerie(int deltaT)
        {

            if (model.Series.Contains(approxSerie)) model.Series.Remove(approxSerie);

            var stPos = 0;
            var endPos = 10000;
            var step = 10000;

            approxSerie = new LineSeries
            {
                MarkerSize = 5,
                MarkerFill = OxyColors.Black,
                MarkerType = MarkerType.Circle
            };

            SubscribeEventsToLineSerie(approxSerie);

            approxSerie.Points.Add(new DataPoint(oceanicSerie.Points[0].X, oceanicSerie.Points[0].Y));

            while (endPos < MAX_DEPTH)
            {
                var startPoint = new DataPoint(stPos, oceanicFunc(stPos));
                var endPoint = new DataPoint(endPos, oceanicFunc(endPos));

                var fc = GetLinearFuncByTwoPoints(startPoint, endPoint);

                while (FunctDistances(fc, oceanicFunc, stPos, endPos, 1000).Max() < deltaT && endPos < MAX_DEPTH)
                {
                    endPos += step;
                    endPoint = new DataPoint(endPos, oceanicFunc(endPos));
                    fc = GetLinearFuncByTwoPoints(startPoint, endPoint);
                }

                stPos = endPos - step;

                approxSerie.Points.Add(new DataPoint(oceanicFunc(stPos), stPos));
                endPos += step;

            }

            //approxSerie.Points[approxSerie.Points.Count - 1] = new DataPoint(oceanicSerie.Points.Last().X, oceanicSerie.Points.Last().Y);

            staticPoint.Y = Hc;
            staticPoint.X = oceanicFunc(Hc);

            approxSerie.Points.RemoveAll(x => Math.Abs(x.Y - staticPoint.Y) < 5000); //удаляем все точки, которые очень близко к статической

            var tarPt = approxSerie.Points.FirstOrDefault(x => x.Y > Hc);
            var tarInd = approxSerie.Points.IndexOf(tarPt);
            if (tarInd != -1) approxSerie.Points.Insert(tarInd, staticPoint);


            approxSerie.Points.RemoveAll(x => x.Y >= PtOcEnd.Y);
            approxSerie.Points.Add(new DataPoint(PtOcEnd.X, PtOcEnd.Y)); 

            model.Series.Add(approxSerie);
            model.InvalidatePlot(false);
        }

        private void SubscribeEventsToLineSerie(LineSeries seria)
        {
            int indexOfPointToMove = -1;
            seria.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == OxyMouseButton.Right)
                {
                    if (e.IsControlDown)
                    {
                        int indexOfNearestPoint = (int)Math.Round(e.HitTestResult.Index);

                        if (indexOfNearestPoint == seria.Points.IndexOf(staticPoint))
                        {
                            e.Handled = true;
                            return;
                        }
                        
                        var nearestPoint = seria.Transform(seria.Points[indexOfNearestPoint]);

                        if ((nearestPoint - e.Position).Length < 10)
                        {
                            seria.Points.RemoveAt(indexOfNearestPoint); 
                            model.InvalidatePlot(false);
                        }

                    }
                }

                if (e.ChangedButton == OxyMouseButton.Left)
                {

                    int indexOfNearestPoint = (int)Math.Round(e.HitTestResult.Index);
                    var nearestPoint = seria.Transform(seria.Points[indexOfNearestPoint]);

                    if (e.IsControlDown)
                    {
                        if ((nearestPoint - e.Position).Length < 10)
                        {
                            indexOfPointToMove = indexOfNearestPoint;
                        }
                        else
                        {
                            int i = (int)e.HitTestResult.Index + 1;
                            seria.Points.Insert(i, seria.InverseTransform(e.Position));
                            indexOfPointToMove = i;
                        }
                    }

                    seria.LineStyle = LineStyle.DashDot;

                    model.InvalidatePlot(false);

                    e.Handled = true;
                }
            };

            seria.MouseMove += (s, e) =>
            {

                if (indexOfPointToMove == seria.Points.IndexOf(staticPoint))
                {
                    e.Handled = true;
                    return;
                }

                if (indexOfPointToMove >= 0)
                {
                    //seria.Points[indexOfPointToMove] = new DataPoint(seria.InverseTransform(e.Position).X, seria.InverseTransform(e.Position).Y); 
                    seria.Points[indexOfPointToMove] = new DataPoint(oceanicFunc(seria.InverseTransform(e.Position).Y), seria.InverseTransform(e.Position).Y); 
                    model.InvalidatePlot(false);
                    e.Handled = true;
                }
            };

            seria.MouseUp += (s, e) =>
            {

                indexOfPointToMove = -1;
                seria.LineStyle = LineStyle.Solid;
                model.InvalidatePlot(false);
                e.Handled = true;
            };
        }

        private void ClearApproxButton_Click(object sender, RoutedEventArgs e)
        {
            if (model.Series.Contains(approxSerie))
            {
                model.Series.Remove(approxSerie);
                model.InvalidatePlot(false);
            }

        }

        private void ImplyButton_Click(object sender, RoutedEventArgs e)
        {
            //todo расхардкодить глубину океана и высоту воздуха (расчёт по маркерам или иниту или сделать опциональным)
            var Hocean = 4500; 
            var Hair = 8000;

            if (approxSerie == null || approxSerie.Points.Count == 0)
            {
                MessageBox.Show("Похоже, вы ещё не сделали аппроксимацию. Нечего выводить.");
                return;
            }

            var leftStrings = LeftRangeBox.Text.Split(new [] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            var rightStrings = RightRangeBox.Text.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (leftStrings.Count() != 2 || rightStrings.Count() != 2)
            {
                MessageBox.Show("Неверный формат введеных данных в полях интервала окраины!");
                return;
            }

            var lx1 = Config.Tools.ParseOrDefaultDouble(leftStrings[0], -1) * 1000;
            var lx2 = Config.Tools.ParseOrDefaultDouble(leftStrings[1], -1) * 1000;
            var rx1 = Config.Tools.ParseOrDefaultDouble(rightStrings[0], -1) * 1000;
            var rx2 = Config.Tools.ParseOrDefaultDouble(rightStrings[1], -1) * 1000;

            if (lx1 < 0 || lx2 < 0 || rx1 < 0 || rx2 < 0)
            {
                MessageBox.Show("Произошла ошибка при поытке считать интервалы окраин.");
                return;
            }

            List<Geotherm> geotherms = new List<Geotherm>();

            // уравнения геотермы континентальной корочки
            var crustEq =
                GetLinearFuncByTwoPoints(
                    new DataPoint(_continentalCrustSeries.Points[0].Y, _continentalCrustSeries.Points[0].X), // x и y меняем местами, поскольку отображение графика нестандартное (ось аргумента слева, а функции сверху)
                    new DataPoint(_continentalCrustSeries.Points[1].Y, _continentalCrustSeries.Points[1].X));
            
            // уравнения геотермы континентальной литосферы
            var contLitEq =
                GetLinearFuncByTwoPoints(
                    new DataPoint(_continentalSeries.Points[0].Y, _continentalSeries.Points[0].X), 
                    new DataPoint(_continentalSeries.Points[1].Y, _continentalSeries.Points[1].X));

            // уравнение мантийной адиабаты
            var adiabateEq = GetLinearFuncByTwoPoints(
                    new DataPoint(mantleSeries.Points[0].Y, mantleSeries.Points[0].X),
                    new DataPoint(mantleSeries.Points[1].Y, mantleSeries.Points[1].X));

            // конечная точка аппроксимации
            var hlc = _continentalSeries.Points[1].Y;
            var tlc = _continentalSeries.Points[1].X;
            var conPt = ContAdiabateIntersection(hlc, tlc);
            var endPt = (conPt.Y > PtOcEnd.Y) ? conPt : PtOcEnd;

            //todo добавлять точку на изломе конт геотермы

            if (conPt.Y < PtOcEnd.Y)
            {
                var tarPt = approxSerie.Points.FirstOrDefault(x => x.Y > conPt.Y);
                var ind = approxSerie.Points.IndexOf(tarPt);
                if (ind != -1) approxSerie.Points.Insert(ind, new DataPoint(oceanicFunc(conPt.Y), conPt.Y)); 
            }

            // формирование температурных полей слева
            for (int i = 0; i < approxSerie.Points.Count - 1; i++)
            {
                var gt = new Geotherm
                {
                    Apex0 = new ModPoint(lx1, Math.Round(approxSerie.Points[i].Y)),
                    Apex1 = new ModPoint(lx1, Math.Round(approxSerie.Points[i + 1].Y)),
                    Apex2 = new ModPoint(lx2, Math.Round(approxSerie.Points[i].Y)),
                    Apex3 = new ModPoint(lx2, Math.Round(approxSerie.Points[i + 1].Y))
                };

                var contTFunc = (gt.Apex3.Y <= Hc) ? crustEq : contLitEq;
                if (gt.Apex3.Y > conPt.Y) contTFunc = adiabateEq;


                gt.T0 = Math.Round(contTFunc(gt.Apex0.Y)) + 273;
                gt.T1 = Math.Round(contTFunc(gt.Apex1.Y)) + 273;
                gt.T2 = Math.Round(approxSerie.Points[i].X) + 273;
                gt.T3 = Math.Round(approxSerie.Points[i + 1].X) + 273;

                gt.Apex0.Y += Hair;
                gt.Apex1.Y += Hair;
                gt.Apex2.Y += Hair + Hocean;
                gt.Apex3.Y += Hair + Hocean;

                geotherms.Add(gt);
            }

            // формирование температурных полей справа
            for (int i = 0; i < approxSerie.Points.Count - 1; i++)
            {
                var gt = new Geotherm
                {
                    Apex0 = new ModPoint(rx1, Math.Round(approxSerie.Points[i].Y) ),
                    Apex1 = new ModPoint(rx1, Math.Round(approxSerie.Points[i + 1].Y) ),
                    Apex2 = new ModPoint(rx2, Math.Round(approxSerie.Points[i].Y) ),
                    Apex3 = new ModPoint(rx2, Math.Round(approxSerie.Points[i + 1].Y) )
                };

                var contTFunc = (gt.Apex3.Y <= Hc) ? crustEq : contLitEq;
                if (gt.Apex3.Y > conPt.Y) contTFunc = adiabateEq;

                gt.T0 = Math.Round(approxSerie.Points[i].X) + 273;
                gt.T1 = Math.Round(approxSerie.Points[i + 1].X) + 273;
                gt.T2 = Math.Round(contTFunc(gt.Apex0.Y)) + 273;
                gt.T3 = Math.Round(contTFunc(gt.Apex1.Y)) + 273;

                gt.Apex0.Y += Hair + Hocean;
                gt.Apex1.Y += Hair + Hocean;
                gt.Apex2.Y += Hair; 
                gt.Apex3.Y += Hair;

                geotherms.Add(gt);
            }

            var totString = geotherms.Aggregate("", (current, geoTherm) => current + (String.Format("{0}       m{1}  m{2}  m{3}  m{4}  m{5}  m{6}  m{7}  m{8}   {9}   {10}   {11}   {12}\n", geoTherm.GeothermType, geoTherm.Apex0.X, geoTherm.Apex0.Y, geoTherm.Apex1.X, geoTherm.Apex1.Y, geoTherm.Apex2.X, geoTherm.Apex2.Y, geoTherm.Apex3.X, geoTherm.Apex3.Y, geoTherm.T0.ToString("0.#"), geoTherm.T1.ToString("0.#"), geoTherm.T2.ToString("0.#"), geoTherm.T3.ToString("0.#")).Replace(",", ".")));

            var tw = new TextWindow(totString);
            tw.Show();
        }

    }
}
