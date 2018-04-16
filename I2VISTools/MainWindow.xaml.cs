using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using I2VISTools.Config;
using I2VISTools.InitClasses;
using I2VISTools.ModelClasses;
using I2VISTools.ModelConfigClasses;
using I2VISTools.Subclasses;
using I2VISTools.Tools;
using I2VISTools.Windows;
using Microsoft.Windows.Controls.Ribbon;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using FlowDirection = System.Windows.FlowDirection;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace I2VISTools
{
    delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        #region Private Fields and Properies
        
        private DataTable squeueTable;
        private bool isOffline = false;

        private InitConfig _currentConfig;

        private Dictionary<string, AreaSeries> areas;

        private PlotModel _graphModel;
        private AreaSeries _selectedArea;
        private LineSeries _selectedContour;
        private List<int> _selectedSideIndexes;
        private PointAnnotation _selectedSide = new PointAnnotation();

        private LineSeries _selectedTermoBox;

        private LineSeries _ruleX;
        private LineSeries _ruleY;

        private HIstoryView _history = new HIstoryView();
        //private HistoryLog _bufferLog;

        private bool _isNewBoxAdding = false;
        private bool _readyForDrag = false;
        private DataPoint _dragPt;
        private List<ModPoint> _startDragRectPosition;

        private LineSeries _newBoxSeria;

        private bool isPrn = false;
        private List<string> files;
        private FolderBrowserDialog fbd;

        List<DrawingImage> _images = new List<DrawingImage>();
        List<string> _imagesnames = new List<string>();

        BackgroundWorker worker = new BackgroundWorker();

        private string overlayFile { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            squeueTable = new DataTable();
            squeueTable.Columns.Add("JobId");
            squeueTable.Columns.Add("Partition");
            squeueTable.Columns.Add("User");
            squeueTable.Columns.Add("Status");
            squeueTable.Columns.Add("Time");
            squeueTable.Columns.Add("NodeList");

            SqueueDataGrid.ItemsSource = squeueTable.DefaultView;
            //Insert code required on object creation below this point.

            SetDefaultConf();
            SetDefaultGraphConf();

            ClusterPartsBox.Items.Add("Все");
            ClusterPartsBox.Items.Add("Мои задачи");
            ClusterPartsBox.Items.Add("regular4");
            ClusterPartsBox.Items.Add("regular6");
            ClusterPartsBox.Items.Add("hdd4");
            ClusterPartsBox.Items.Add("hdd6");
            ClusterPartsBox.Items.Add("gpu");
            ClusterPartsBox.Items.Add("test");
            ClusterPartsBox.Items.Add("gputest");
            ClusterPartsBox.SelectedIndex = 1;

            if (!isOffline) ((App)Application.Current).SSHManager = new SshManager(Config.Config.Instace);

            GeologyConfig.Instace.LoadFacies();
            GeologyConfig.Instace.LoadPhases();

        }


        private void UpdateSqueue()
        {
            squeueTable.Clear();

            var username = Config.Config.Instace.UserLogin;

            string stringPart = (ClusterPartsBox.SelectedIndex == 0) ? "" : " -p " + ClusterPartsBox.SelectedValue;
            if (ClusterPartsBox.SelectedIndex == 1) stringPart = " -u " + username;

            var cmds = new List<string> { "module add slurm", "squeue" + stringPart };

            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);
            if (string.IsNullOrEmpty(result)) return;

            result = result.Substring(result.IndexOf('\n'), result.Length - result.IndexOf('\n'));

            var resultSubStrings = result.Split('\n');

            foreach (var substring in resultSubStrings)
            {
                if (String.IsNullOrWhiteSpace(substring)) continue;
                var squeueParams = substring.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string jobId, partition, user, status, time, nodelist;

                jobId = squeueParams[0];
                partition = squeueParams[1];
                user = squeueParams[3];
                status = squeueParams[4];
                time = squeueParams[5];
                nodelist = squeueParams[7];

                squeueTable.Rows.Add(jobId, partition, user, status, time, nodelist);
            }
        }

        private void SqueueButton_OnClick(object sender, RoutedEventArgs e)
        {
            UpdateSqueue();
        }


        private void Ribbon_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayControl.SelectedIndex = Ribbon.SelectedIndex;
        }

        private void OpenInitButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog();
                if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                var fName = ofd.FileName;

                _currentConfig = new InitConfig();
                ReadInitFromFile(fName);

                GeometryHideBox.IsChecked = true;
                EnableRbControls();

                RocksPropertiesButton.IsEnabled = true;
                if (!isOffline) CommitInitButton.IsEnabled = true;
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка чтения файла: " + ex.Message);
            }

            RocksPropertiesButton.IsEnabled = true;
        }

        private void ReadInitFromFile(string fName)
        {
            using (StreamReader sr = new StreamReader(fName))
            {
                String line = sr.ReadLine().Replace(" ", String.Empty);

                var gridPar = new CalcGrid();

                if (line.StartsWith("/"))
                {
                    //TODO конец файла также учесть
                    while (String.IsNullOrWhiteSpace(line) || line.StartsWith("/"))
                    {
                        line = sr.ReadLine().Replace(" ", String.Empty);
                    }
                }

                // TODO сделать, чтобы пробелы, пропуски в любом месте игнорились
                gridPar.Xnumx = GetIntFromLine(line);
                gridPar.Ynumy = GetIntFromLine(sr.ReadLine());
                gridPar.Mnumx = GetIntFromLine(sr.ReadLine());

                line = sr.ReadLine();
                var sublines = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (sublines.Count() > 2)
                {
                    gridPar.Mnumy = GetIntFromLine(sublines[0]);
                    gridPar.Mnumy_left = GetIntFromLine(sublines[1]);
                    gridPar.Mnumy_rigth = GetIntFromLine(sublines[2]);
                }
                else
                {
                    gridPar.Mnumy = GetIntFromLine(line);
                }

                gridPar.Xsize = GetIntFromLine(sr.ReadLine());
                gridPar.Ysize = GetIntFromLine(sr.ReadLine());
                gridPar.Pinit = GetDoubleFromLine(sr.ReadLine());
                gridPar.GxKoef = GetDoubleFromLine(sr.ReadLine());
                gridPar.GyKoef = GetDoubleFromLine(sr.ReadLine());
                gridPar.TimeSum = GetIntFromLine(sr.ReadLine());

                line = sr.ReadLine();
                sublines = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (sublines.Count() > 2)
                {
                    gridPar.NonStab = GetIntFromLine(sublines[0]);
                    gridPar.dx = GetDoubleFromLine(sublines[1]);
                    gridPar.dy = GetDoubleFromLine(sublines[2]);
                }
                else
                {
                    gridPar.NonStab = GetIntFromLine(line);
                }

                _currentConfig.Grid = gridPar;

                
                _currentConfig.MarkerTypesFileName = Config.Tools.ParseOrDefaultInt(NextActualLine(sr));

                sublines = NextActualLine(sr).Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (sublines.Count() > 1)
                {
                    _currentConfig.OutputPrnFile = sublines[0];
                    _currentConfig.OutputType = sublines[1][0];
                }
                else
                {
                    _currentConfig.OutputPrnFile = sublines[0];
                    _currentConfig.OutputType = 'b';
                }
                #region считывание свойств пород
                var rocksStrings = new List<string>();
                while (!line.Contains("~"))
                {
                    line = sr.ReadLine();
                    if (String.IsNullOrWhiteSpace(line) || line.Contains("~")) continue;
                    rocksStrings.Add(line);
                }

                //NUM_______________NU(Pa^MM*s)___DE(J)_________DV(J/bar)_____SS(Pa)________MM(Power)_____LL(KOEF)____RO(kg/M^3)____bRo(1/K)______aRo(1/kbar)___CP(J/kg)______Kt(Wt/(m*K))__Ht(Wt/kg)
                for (int i = 0; i < rocksStrings.Count; i++)
                {
                    if (!rocksStrings[i].StartsWith("/"))
                    {
                        var props = rocksStrings[i].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        if (props.Count() != 25) throw new Exception("Ошибка в структуре блока свойств горных пород!");

                        var rock = new RockDescription
                        {
                            RockNum = GetIntFromLine(props[0]),
                            n0 = GetNumberFromString(props[1]),
                            n1 = GetNumberFromString(props[2]),
                            s0 = GetNumberFromString(props[3]),
                            s1 = GetNumberFromString(props[4]),
                            Nu = GetNumberFromString(props[5]),
                            dh = GetNumberFromString(props[6]),
                            Dv = GetNumberFromString(props[7]),
                            Ss = GetNumberFromString(props[8]),
                            Mm = GetNumberFromString(props[9]),
                            Ll = GetNumberFromString(props[10]),
                            a0 = GetNumberFromString(props[11]),
                            a1 = GetNumberFromString(props[12]),
                            b0 = GetNumberFromString(props[13]),
                            b1 = GetNumberFromString(props[14]),
                            e0 = GetNumberFromString(props[15]),
                            e1 = GetNumberFromString(props[16]),
                            Ro = GetNumberFromString(props[17]),
                            bRo = GetNumberFromString(props[18]),
                            aRo = GetNumberFromString(props[19]),
                            Cp = GetNumberFromString(props[20]),
                            Kt = GetNumberFromString(props[21]),
                            kf = GetNumberFromString(props[22]),
                            kp = GetNumberFromString(props[23]),
                            Ht = GetNumberFromString(props[24])
                        };

                        if (i > 0 && rocksStrings[i - 1].StartsWith("/"))
                        {
                            rock.RockName = rocksStrings[i - 1].Remove(0, 1);
                        }
                        _currentConfig.Rocks.Add(rock);
                    }
                }
                #endregion

                line = sr.ReadLine();
                while (!line.Contains("~"))
                {
                    line = sr.ReadLine();
                    _currentConfig.kostyl.Add(line);
                    // TODO пока пропустим блок с граничечными условиями
                }
                _currentConfig.kostyl.RemoveAt(_currentConfig.kostyl.Count - 1);
                line = sr.ReadLine();

                int rbc = 0;
                // геометрия пород
                while (!line.Contains("~"))
                {
                    line = NextActualLine(sr);
                    if (line.Contains("~")) continue;
                    var boxPars = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (boxPars.Count() != 10) throw new Exception("Ошибка в описании геометрии горных пород!");

                    var newRockBox = new RockBox
                    {
                        RockType = Config.Tools.ParseOrDefaultInt(boxPars[0]),
                        RockId = (boxPars[1][0] == 'i') ? Config.Tools.ParseOrDefaultInt(boxPars[1].Substring(1, boxPars[1].Length - 1)) * -1 : Config.Tools.ParseOrDefaultInt(boxPars[1]),
                        Apex0 =
                            new ModPoint(_currentConfig.GetValueFromString(boxPars[2]),
                                _currentConfig.GetValueFromString(boxPars[3], false)),
                        Apex1 =
                            new ModPoint(_currentConfig.GetValueFromString(boxPars[4]),
                                _currentConfig.GetValueFromString(boxPars[5], false)),
                        Apex2 =
                            new ModPoint(_currentConfig.GetValueFromString(boxPars[6]),
                                _currentConfig.GetValueFromString(boxPars[7], false)),
                        Apex3 =
                            new ModPoint(_currentConfig.GetValueFromString(boxPars[8]),
                                _currentConfig.GetValueFromString(boxPars[9], false)),
                        Name = "RockBox" + rbc
                    };

                    _currentConfig.RockBoxes.Add(newRockBox);
                    rbc++;
                }

                line = sr.ReadLine();

                var geoThermsCt = 0;
                // ГЕОТЕРМЫ
                while (!line.Contains("~"))
                {
                    line = NextActualLine(sr);
                    if (line.Contains("~")) continue;
                    var geoPars = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (geoPars.Count() < 13) throw new Exception("Ошибка в описании геотерм!");

                    var newGeotherm = new Geotherm
                    {
                        GeothermType = Config.Tools.ParseOrDefaultInt(geoPars[0]),
                        Apex0 =
                            new ModPoint(_currentConfig.GetValueFromString(geoPars[1]),
                                _currentConfig.GetValueFromString(geoPars[2], false)),
                        Apex1 =
                            new ModPoint(_currentConfig.GetValueFromString(geoPars[3]),
                                _currentConfig.GetValueFromString(geoPars[4], false)),
                        Apex2 =
                            new ModPoint(_currentConfig.GetValueFromString(geoPars[5]),
                                _currentConfig.GetValueFromString(geoPars[6], false)),
                        Apex3 =
                            new ModPoint(_currentConfig.GetValueFromString(geoPars[7]),
                                _currentConfig.GetValueFromString(geoPars[8], false)),
                        T0 = GetNumberFromString(geoPars[9]),
                        T1 = GetNumberFromString(geoPars[10]),
                        T2 = GetNumberFromString(geoPars[11]),
                        T3 = GetNumberFromString(geoPars[12])
                    };

                    if (geoPars.Count() > 13)
                    {
                        newGeotherm.LeftOceanicAge = GetNumberFromString(geoPars[13]);
                        newGeotherm.RightOceanicAge = GetNumberFromString(geoPars[14]);
                        newGeotherm.ThermalDiffusivity = GetNumberFromString(geoPars[15]);
                    }

                    newGeotherm.Name = "Geotherm" + geoThermsCt;
                    geoThermsCt++;

                    _currentConfig.Geotherms.Add(newGeotherm);
                }

                GeometryDataGrid.ItemsSource = _currentConfig.RockBoxes;
                GeothermsDataGrid.ItemsSource = _currentConfig.Geotherms;

                _graphModel = new PlotModel();
                var linearAxis1 = new LinearAxis();
                linearAxis1.Position = AxisPosition.Bottom;
                linearAxis1.Minimum = 0;
                linearAxis1.Maximum = _currentConfig.Grid.Xsize;
                linearAxis1.AbsoluteMinimum = -5000000;
                linearAxis1.AbsoluteMaximum = 9000000;
                _graphModel.Axes.Add(linearAxis1);
                var linearAxis2 = new LinearAxis();
                linearAxis2.Minimum = 0;
                linearAxis2.Maximum = _currentConfig.Grid.Ysize;
                linearAxis2.StartPosition = 1;
                linearAxis2.EndPosition = 0;
                linearAxis2.AbsoluteMinimum = -200000;
                linearAxis2.AbsoluteMaximum = 1000000;
                _graphModel.Axes.Add(linearAxis2);

                _graphModel.Background = OxyColors.WhiteSmoke;


                var upperBound = new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = 0,
                    MaximumX = _currentConfig.Grid.Ysize,
                    MinimumX = 0,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.DashDotDot
                };

                var bottomBound = new LineAnnotation
                {
                    Type = LineAnnotationType.Horizontal,
                    Y = _currentConfig.Grid.Ysize,
                    MaximumX = _currentConfig.Grid.Xsize,
                    MinimumX = 0,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.DashDotDot
                };

                var leftBound = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = 0,
                    MaximumY = _currentConfig.Grid.Ysize,
                    MinimumY = 0,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.DashDotDot
                };

                var rightBound = new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = _currentConfig.Grid.Xsize,
                    MaximumY = _currentConfig.Grid.Ysize,
                    MinimumY = 0,
                    Color = OxyColors.White,
                    LineStyle = LineStyle.DashDotDot
                };


                _graphModel.Annotations.Add(upperBound);
                _graphModel.Annotations.Add(bottomBound);
                _graphModel.Annotations.Add(leftBound);
                _graphModel.Annotations.Add(rightBound);

                var rockColors = GraphConfig.Instace.ColorMap;

                areas = new Dictionary<string, AreaSeries>();

                foreach (var rockBox in _currentConfig.RockBoxes)
                {
                    var ptSeries = new AreaSeries {Tag = rockBox.Name + "area"};
                    areas.Add(rockBox.Name, ptSeries);

                    var apex0 = new DataPoint(rockBox.Apex0.X, rockBox.Apex0.Y);
                    var apex1 = new DataPoint(rockBox.Apex1.X, rockBox.Apex1.Y);
                    var apex2 = new DataPoint(rockBox.Apex2.X, rockBox.Apex2.Y);
                    var apex3 = new DataPoint(rockBox.Apex3.X, rockBox.Apex3.Y);

                    ptSeries.Points.Add(apex0);
                    ptSeries.Points.Add(apex2);
                    ptSeries.Points2.Add(apex1);
                    ptSeries.Points2.Add(apex3);

                    ptSeries.Fill = rockColors[Math.Abs(rockBox.RockId)];
                    ptSeries.Color = OxyColors.Transparent;
                    _graphModel.Series.Add(ptSeries);
                }

                foreach (var rockBox in _currentConfig.RockBoxes)
                {
                    var points = new LineSeries
                    {
                        Tag = rockBox.Name,
                        MarkerFill = rockColors[Math.Abs(rockBox.RockId)],
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 4,
                        Color = OxyColors.Black
                    };
                    points.Points.Add(new DataPoint(rockBox.Apex0.X, rockBox.Apex0.Y));
                    points.Points.Add(new DataPoint(rockBox.Apex1.X, rockBox.Apex1.Y));
                    points.Points.Add(new DataPoint(rockBox.Apex3.X, rockBox.Apex3.Y));
                    points.Points.Add(new DataPoint(rockBox.Apex2.X, rockBox.Apex2.Y));
                    points.Points.Add(new DataPoint(rockBox.Apex0.X, rockBox.Apex0.Y));
                    
                    AttachMovingEvents(points);
                    AttachChangeEvents(rockBox);
                    
                    points.IsVisible = (NodesVisibilityBox.IsChecked == true);

                    _graphModel.Series.Add(points);
                }

                foreach (var termoBox in _currentConfig.Geotherms)
                {
                    var points = new LineSeries
                    {
                        Tag = termoBox.Name,
                        MarkerFill = OxyColors.Orange,
                        MarkerType = MarkerType.Square,
                        MarkerSize = 4,
                        Color = OxyColors.Orange
                    };
                    points.Points.Add(new DataPoint(termoBox.Apex0.X, termoBox.Apex0.Y));
                    points.Points.Add(new DataPoint(termoBox.Apex1.X, termoBox.Apex1.Y));
                    points.Points.Add(new DataPoint(termoBox.Apex3.X, termoBox.Apex3.Y));
                    points.Points.Add(new DataPoint(termoBox.Apex2.X, termoBox.Apex2.Y));
                    points.Points.Add(new DataPoint(termoBox.Apex0.X, termoBox.Apex0.Y));


                    var t0 = new PointAnnotation
                    {
                        X = termoBox.Apex0.X,
                        Y = termoBox.Apex0.Y,
                        Text = termoBox.T0.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = termoBox.Name + "T0"
                    };

                    var t1 = new PointAnnotation
                    {
                        X = termoBox.Apex1.X,
                        Y = termoBox.Apex1.Y,
                        Text = termoBox.T1.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = termoBox.Name + "T1"
                    };

                    var t2 = new PointAnnotation
                    {
                        X = termoBox.Apex2.X,
                        Y = termoBox.Apex2.Y,
                        Text = termoBox.T2.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = termoBox.Name + "T2"
                    };

                    var t3 = new PointAnnotation
                    {
                        X = termoBox.Apex3.X,
                        Y = termoBox.Apex3.Y,
                        Text = termoBox.T3.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = termoBox.Name + "T3"
                    };

                    _graphModel.Annotations.Add(t0);
                    _graphModel.Annotations.Add(t1);
                    _graphModel.Annotations.Add(t2);
                    _graphModel.Annotations.Add(t3);

                    AttachMovingEvents(points);

                    points.IsVisible = (GeothermsBox.IsChecked == true);
                    foreach (var annotation in _graphModel.Annotations)
                    {
                        annotation.TextColor = (GeothermsBox.IsChecked == true) ? OxyColors.Red : OxyColors.Transparent;
                    }

                    AttachChangeEvents(termoBox);
                    _graphModel.Series.Add(points);
                }

                _graphModel.MouseDown += (s, ev) =>
                {

                    if (ev.ChangedButton == OxyMouseButton.Left)
                    {

                        if (!_isNewBoxAdding)
                        {
                            if (areas.Count <= 0) return;
                            var currPoint = areas.First().Value.InverseTransform(ev.Position);
                            var belongedSeries = new List<AreaSeries>();

                            foreach (var areaseria in areas.Keys)
                            {
                                areas[areaseria].Color = OxyColors.Transparent;
                                var polygon = new List<DataPoint>
                                {
                                    areas[areaseria].Points[0],
                                    areas[areaseria].Points2[0],
                                    areas[areaseria].Points2[1],
                                    areas[areaseria].Points[1]
                                };
                                if (IsPointInPolygon(polygon, currPoint)) belongedSeries.Add(areas[areaseria]);
                            }

                            if (belongedSeries.Count > 0)
                            {
                                belongedSeries.Last().Color = OxyColors.Red;
                                belongedSeries.Last().StrokeThickness = 5;
                                _selectedArea = belongedSeries.Last();
                                var contourTag = _selectedArea.Tag.ToString();
                                _selectedContour =
                                    _graphModel.Series.FirstOrDefault(
                                        x => x.Tag.ToString() == contourTag.Substring(0, contourTag.Length - 4)) as LineSeries ;


                                if (_selectedContour != null)
                                {
                                    _selectedSideIndexes = GetIndexesOfNearestSidePoints(_selectedContour, ev.Position, 25);

                                    if (_selectedSideIndexes != null)
                                    {

                                        _selectedSide.X = _selectedContour.Points[_selectedSideIndexes[0]].X + (_selectedContour.Points[_selectedSideIndexes[1]].X -
                                                           _selectedContour.Points[_selectedSideIndexes[0]].X)/2d;
                                        _selectedSide.Y = _selectedContour.Points[_selectedSideIndexes[0]].Y +(_selectedContour.Points[_selectedSideIndexes[1]].Y -
                                                           _selectedContour.Points[_selectedSideIndexes[0]].Y) / 2d;
                                        _selectedSide.Size = 6;
                                        _selectedSide.Fill = OxyColors.Red;
                                        _selectedSide.Shape = MarkerType.Square;

                                        if (!_graphModel.Annotations.Contains(_selectedSide))
                                            _graphModel.Annotations.Add(_selectedSide);

                                    }
                                    else
                                    {
                                        _selectedSide.Fill = OxyColors.Transparent;
                                    }
                                    
                                    _graphModel.InvalidatePlot(false);

                                }
                                 

                                var rb = GetSelectedRockBox();
                                var ind = _currentConfig.RockBoxes.IndexOf(rb);
                                GeometryDataGrid.SelectedIndex = ind;

                                _graphModel.InvalidatePlot(false);

                                
                                //if (_selectedContour != null && ev.IsControlDown)
                                //{
                                //    _readyForDrag = true;
                                //    _dragPt = _selectedContour.InverseTransform(ev.Position);

                                //    _startDragRectPosition = new List<ModPoint>
                                //    {
                                //        new ModPoint(_selectedContour.Points[0].X, _selectedContour.Points[0].Y),
                                //        new ModPoint(_selectedContour.Points[1].X, _selectedContour.Points[1].Y),
                                //        new ModPoint(_selectedContour.Points[3].X, _selectedContour.Points[3].Y),
                                //        new ModPoint(_selectedContour.Points[2].X, _selectedContour.Points[2].Y)
                                //    };
                                //}
                            }
                        }
                        else
                        {
                            var actualPos = areas.First().Value.InverseTransform(ev.Position);
                            
                            if (_newBoxSeria == null)
                            {
                                _newBoxSeria = new LineSeries();
                                _newBoxSeria.MarkerFill = OxyColors.Green;
                                _newBoxSeria.MarkerType = MarkerType.Circle;
                                _newBoxSeria.MarkerSize = 4;
                                _newBoxSeria.Color = OxyColors.Green;
                            }

                            var pt = new DataPoint
                            {
                                X = actualPos.X,
                                Y = actualPos.Y
                            };

                            _newBoxSeria.Points.Add(pt);
                            if (!_graphModel.Series.Contains(_newBoxSeria)) _graphModel.Series.Add(_newBoxSeria);

                            if (_newBoxSeria.Points.Count >= 4)
                            {
                                var lastPt = new DataPoint
                                {
                                    X = _newBoxSeria.Points[0].X,
                                    Y = _newBoxSeria.Points[0].Y
                                };
                                _newBoxSeria.Points.Add(lastPt);
                                //_graphModel.Series.Add(_newBoxSeria);
                                //_newBoxSeria = null;
                                _isNewBoxAdding = false;

                                var menu = new ContextMenu();
                                menu.Closed += (sender, args) =>
                                {
                                    if (_newBoxSeria != null)
                                    {
                                        _newBoxSeria.IsVisible = false;
                                        _newBoxSeria = null;
                                        _graphModel.InvalidatePlot(false);
                                    }
                                };

                                foreach (var rock in _currentConfig.Rocks)
                                {
                                    var item = new MenuItem {Header = rock.RockNum};
                                    item.Click += (sender, args) =>
                                    {
                                        if (_newBoxSeria == null) return;
                                        _newBoxSeria.Points[0] = new DataPoint(_newBoxSeria.Points[0].X - _newBoxSeria.Points[0].X % 100, _newBoxSeria.Points[0].Y - _newBoxSeria.Points[0].Y % 100);
                                        _newBoxSeria.Points[1] = new DataPoint(_newBoxSeria.Points[1].X - _newBoxSeria.Points[1].X % 100, _newBoxSeria.Points[1].Y - _newBoxSeria.Points[1].Y % 100);
                                        _newBoxSeria.Points[2] = new DataPoint(_newBoxSeria.Points[2].X - _newBoxSeria.Points[2].X % 100, _newBoxSeria.Points[2].Y - _newBoxSeria.Points[2].Y % 100);
                                        _newBoxSeria.Points[3] = new DataPoint(_newBoxSeria.Points[3].X - _newBoxSeria.Points[3].X % 100, _newBoxSeria.Points[3].Y - _newBoxSeria.Points[3].Y % 100);

                                        var rb = new RockBox();
                                        rb.RockId = Convert.ToInt32(item.Header);
                                        rb.Name = "RockBox" +
                                            _currentConfig.RockBoxes.Select(
                                                x => Config.Tools.ExtractNumberFromString(x.Name)).ToList().Max() + 1;
                                        rb.RockType = 0;

                                        rb.Apex0 = new ModPoint( _newBoxSeria.Points[0].X, _newBoxSeria.Points[0].Y );
                                        rb.Apex1 = new ModPoint(_newBoxSeria.Points[1].X, _newBoxSeria.Points[1].Y);
                                        rb.Apex2 = new ModPoint(_newBoxSeria.Points[3].X, _newBoxSeria.Points[3].Y);
                                        rb.Apex3 = new ModPoint(_newBoxSeria.Points[2].X, _newBoxSeria.Points[2].Y); 
                                        
                                        _currentConfig.RockBoxes.Add(rb);

                                        _graphModel.Series.Remove(_newBoxSeria);
                                        _newBoxSeria.Tag = rb.Name;
                                        _newBoxSeria.MarkerFill = rockColors[Math.Abs(rb.RockId)];
                                        _newBoxSeria.Color = OxyColors.Black;

                                        var areaSerie = new AreaSeries
                                        {
                                            Tag = rb.Name + "area",
                                            Fill = rockColors[Math.Abs(rb.RockId)]
                                        };
                                        areaSerie.Points.Add(new DataPoint(_newBoxSeria.Points[0].X, _newBoxSeria.Points[0].Y));
                                        areaSerie.Points.Add(new DataPoint(_newBoxSeria.Points[3].X, _newBoxSeria.Points[3].Y));
                                        areaSerie.Points2.Add(new DataPoint(_newBoxSeria.Points[1].X, _newBoxSeria.Points[1].Y));
                                        areaSerie.Points2.Add(new DataPoint(_newBoxSeria.Points[2].X, _newBoxSeria.Points[2].Y));
                                        
                                        areas.Add(rb.Name, areaSerie);

                                        var areaInd =
                                            _graphModel.Series.ToList().LastIndexOf(_graphModel.Series.LastOrDefault(x => x.Tag.ToString().Contains("area"))) + 1;

                                        _graphModel.Series.Insert(areaInd, areaSerie);

                                        var countourInd =
                                            _graphModel.Series.IndexOf(_graphModel.Series.FirstOrDefault(x => x.Tag.ToString().Contains("Geotherm")));

                                        _graphModel.Series.Insert(countourInd, _newBoxSeria);

                                        AttachMovingEvents(_newBoxSeria);
                                        AttachChangeEvents(rb);
                                        GeometryDataGrid.Items.Refresh();
                                        _graphModel.InvalidatePlot(false);

                                        _newBoxSeria = null;

                                    };

                                    menu.Items.Add(item);
                                }

                                menu.PlacementTarget = InitPlotView;
                                menu.IsOpen = true;

                                //_newBoxSeria.IsVisible = false;

                            }

                            _graphModel.InvalidatePlot(false);
                            
                            //MessageBox.Show(actualPos.ToString());
                        }

                    }

                };

                int dx=0, dy=0; //смещение прямоугольников при драге


                _graphModel.MouseUp += (s, ev) =>
                {

                    if (_readyForDrag)
                    {
                        var rb = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == _selectedContour.Tag.ToString());
                        if (rb == null) return;

                        var oldVals = SaveRbValues(rb);

                        rb.FreezeLogging = true;
                        rb.Apex0.X = _startDragRectPosition[0].X + dx;
                        rb.Apex0.Y = _startDragRectPosition[0].Y + dy;
                        rb.Apex1.X = _startDragRectPosition[1].X + dx;
                        rb.Apex1.Y = _startDragRectPosition[1].Y + dy;
                        rb.Apex2.X = _startDragRectPosition[2].X + dx;
                        rb.Apex2.Y = _startDragRectPosition[2].Y + dy;
                        rb.Apex3.X = _startDragRectPosition[3].X + dx;
                        rb.Apex3.Y = _startDragRectPosition[3].Y + dy;
                        rb.FreezeLogging = false;

                        var newVals = SaveRbValues(rb);

                        var hl = new HistoryLog
                        {
                            Undo = FormUndo(rb, oldVals),
                            Do = FormRedo(rb, newVals)
                        };
                        _history.Logs.Add(hl);

                    }

                    _readyForDrag = false;
                };

                _graphModel.MouseMove += (s, ev) =>
                {
                    //if (ev.IsControlDown && _readyForDrag)
                    //{
                    //    if (_selectedContour == null) return;

                    //    if (_selectedSide != null) _selectedSide.Fill = OxyColors.Transparent;

                    //    var curDragPt = _selectedContour.InverseTransform(ev.Position);
                    //    dx = (int) ((curDragPt.X - _dragPt.X)/1000)*1000;
                    //    dy = (int) ((curDragPt.Y - _dragPt.Y)/1000)*1000;

                    //    _selectedContour.Points[0] = new DataPoint(_startDragRectPosition[0].X + dx, _startDragRectPosition[0].Y + dy);
                    //    _selectedContour.Points[1] = new DataPoint(_startDragRectPosition[1].X + dx, _startDragRectPosition[1].Y + dy);
                    //    _selectedContour.Points[2] = new DataPoint(_startDragRectPosition[3].X + dx, _startDragRectPosition[3].Y + dy);
                    //    _selectedContour.Points[3] = new DataPoint(_startDragRectPosition[2].X + dx, _startDragRectPosition[2].Y + dy);
                    //    _selectedContour.Points[4] = new DataPoint(_startDragRectPosition[0].X + dx, _startDragRectPosition[0].Y + dy);


                    //    _graphModel.InvalidatePlot(false);
                    //}
                };

                _graphModel.KeyDown += (s, ev) =>
                { 
                    
                    var xStep = (int) _graphModel.Axes[0].ActualMinorStep;
                    var yStep = (int) _graphModel.Axes[1].ActualMinorStep;

                    if (_selectedSide != null) _selectedSide.Fill = OxyColors.Transparent;

                    if (_selectedArea != null)
                    {
                        var rb = GetSelectedRockBox();

                        if (ev.IsControlDown)
                        {

                            if (ev.Key == OxyKey.NumPad8)
                            {

                                 if (_selectedSideIndexes != null)
                                {
                                    var oldVals = SaveRbValues(rb);
                                    rb.FreezeLogging = true;
                                      foreach (var ind in _selectedSideIndexes)
                                    {
                                        rb.Apexes[ind].Y -= yStep;
                                    }
                                    rb.FreezeLogging = false;
                                    var newVals = SaveRbValues(rb);

                                    var hl = new HistoryLog
                                    {
                                        Undo = FormUndo(rb, oldVals),
                                        Do = FormRedo(rb, newVals)
                                    };
                                    _history.Logs.Add(hl);
                                }


                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad5)
                            {

                                if (_selectedSideIndexes != null)
                                {
                                    var oldVals = SaveRbValues(rb);
                                    rb.FreezeLogging = true;
                                    foreach (var ind in _selectedSideIndexes)
                                    {
                                        rb.Apexes[ind].Y += yStep;
                                    }
                                    rb.FreezeLogging = false;
                                    var newVals = SaveRbValues(rb);

                                    var hl = new HistoryLog
                                    {
                                        Undo = FormUndo(rb, oldVals),
                                        Do = FormRedo(rb, newVals)
                                    };
                                    _history.Logs.Add(hl);
                                }


                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad4)
                            {
                                if (_selectedSideIndexes != null)
                                {
                                    var oldVals = SaveRbValues(rb);
                                    rb.FreezeLogging = true;
                                    foreach (var ind in _selectedSideIndexes)
                                    {
                                        rb.Apexes[ind].X -= xStep;
                                    }
                                    rb.FreezeLogging = false;
                                    var newVals = SaveRbValues(rb);

                                    var hl = new HistoryLog
                                    {
                                        Undo = FormUndo(rb, oldVals),
                                        Do = FormRedo(rb, newVals)
                                    };
                                    _history.Logs.Add(hl);
                                }


                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad6)
                            {

                                if (_selectedSideIndexes != null)
                                {
                                    var oldVals = SaveRbValues(rb);
                                    rb.FreezeLogging = true;
                                    foreach (var ind in _selectedSideIndexes)
                                    {
                                        rb.Apexes[ind].X += xStep;
                                    }
                                    rb.FreezeLogging = false;
                                    var newVals = SaveRbValues(rb);

                                    var hl = new HistoryLog
                                    {
                                        Undo = FormUndo(rb, oldVals),
                                        Do = FormRedo(rb, newVals)
                                    };
                                    _history.Logs.Add(hl);
                                }

                            }

                            _graphModel.InvalidatePlot(false);

                        }
                        else
                        {

                            if (ev.Key == OxyKey.NumPad8)
                            {
                                var oldVals = SaveRbValues(rb);
                                rb.FreezeLogging = true;

                                rb.Apex0.Y -= yStep;
                                rb.Apex1.Y -= yStep;
                                rb.Apex2.Y -= yStep;
                                rb.Apex3.Y -= yStep;

                                rb.FreezeLogging = false;
                                var newVals = SaveRbValues(rb);

                                var hl = new HistoryLog
                                {
                                    Undo = FormUndo(rb, oldVals),
                                    Do = FormRedo(rb, newVals)
                                };
                                _history.Logs.Add(hl);


                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad5)
                            {

                                var oldVals = SaveRbValues(rb);
                                rb.FreezeLogging = true;

                                rb.Apex0.Y += yStep;
                                rb.Apex1.Y += yStep;
                                rb.Apex2.Y += yStep;
                                rb.Apex3.Y += yStep;

                                rb.FreezeLogging = false;
                                var newVals = SaveRbValues(rb);

                                var hl = new HistoryLog
                                {
                                    Undo = FormUndo(rb, oldVals),
                                    Do = FormRedo(rb, newVals)
                                };
                                _history.Logs.Add(hl);

                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad4)
                            {
                                var oldVals = SaveRbValues(rb);
                                rb.FreezeLogging = true;

                                rb.Apex0.X -= xStep;
                                rb.Apex1.X -= xStep;
                                rb.Apex2.X -= xStep;
                                rb.Apex3.X -= xStep;

                                rb.FreezeLogging = false;
                                var newVals = SaveRbValues(rb);

                                var hl = new HistoryLog
                                {
                                    Undo = FormUndo(rb, oldVals),
                                    Do = FormRedo(rb, newVals)
                                };
                                _history.Logs.Add(hl);

                                _graphModel.InvalidatePlot(false);
                            }
                            if (ev.Key == OxyKey.NumPad6)
                            {
                                var oldVals = SaveRbValues(rb);
                                rb.FreezeLogging = true;

                                rb.Apex0.X += xStep;
                                rb.Apex1.X += xStep;
                                rb.Apex2.X += xStep;
                                rb.Apex3.X += xStep;

                                rb.FreezeLogging = false;
                                var newVals = SaveRbValues(rb);

                                var hl = new HistoryLog
                                {
                                    Undo = FormUndo(rb, oldVals),
                                    Do = FormRedo(rb, newVals)
                                };
                                _history.Logs.Add(hl);

                                _graphModel.InvalidatePlot(false);
                            }

                        }
                    }

                };

                InitPlotView.Model = _graphModel;

            }
        }


        List<ModPoint> SaveRbValues(RockBox rb)
        {
            return rb.Apexes.Select(apex => new ModPoint(apex.X, apex.Y)).ToList();
        }

        Action FormUndo(RockBox rb, List<ModPoint> oldVals)
        {
            Action undo = () =>
            {
                rb.FreezeLogging = true;
                for (int i = 0; i < oldVals.Count; i++)
                {
                    rb.Apexes[i].X = oldVals[i].X;
                    rb.Apexes[i].Y = oldVals[i].Y;
                }
                rb.FreezeLogging = false;
            };
            
            return undo;
        }

        Action FormRedo(RockBox rb, List<ModPoint> oldVals)
        {
            Action undo = () =>
            {
                rb.FreezeLogging = true;
                for (int i = 0; i < oldVals.Count; i++)
                {
                    rb.Apexes[i].X = oldVals[i].X;
                    rb.Apexes[i].Y = oldVals[i].Y;
                }
                rb.FreezeLogging = false;
            };

            return undo;
        }

        private RockBox GetSelectedRockBox()
        {
            if (_currentConfig == null || _currentConfig.RockBoxes == null) return null;
            if (_selectedContour == null) return null;
            var rb =
                _currentConfig.RockBoxes.FirstOrDefault(
                    x => x.Name == _selectedContour.Tag.ToString());

            return rb;
        }

        private Geotherm GetSelectedGeotherm()
        {
            if (_currentConfig == null || _currentConfig.RockBoxes == null) return null;
            if (_selectedContour == null) return null;
            var gt =
                _currentConfig.Geotherms.FirstOrDefault(
                    x => x.Name == _selectedContour.Tag.ToString());

            return gt;
        }

        private void AttachChangeEvents(Geotherm termoBox)
        {
            termoBox.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "T0")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T0") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T0.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);
                    _graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "T1")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T1") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T1.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);
                    _graphModel.InvalidatePlot(false);
                }
                if (args.PropertyName == "T2")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T2") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T2.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);
                    _graphModel.InvalidatePlot(false);
                }
                if (args.PropertyName == "T3")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T3") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = curAnnot.X,
                        Y = curAnnot.Y,
                        Text = geoTherm.T3.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);
                    _graphModel.InvalidatePlot(false);
                }


                if (args.PropertyName == "Apex0")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(0);
                    ptSeries.Points.Insert(0, new DataPoint(geoTherm.Apex0.X, geoTherm.Apex0.Y));

                    ptSeries.Points.RemoveAt(4);
                    ptSeries.Points.Insert(4, new DataPoint(geoTherm.Apex0.X, geoTherm.Apex0.Y));

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T0") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex0.X,
                        Y = geoTherm.Apex0.Y,
                        Text = geoTherm.T0.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);


                    _graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex1")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(1);
                    ptSeries.Points.Insert(1, new DataPoint(geoTherm.Apex1.X, geoTherm.Apex1.Y));

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T1") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex1.X,
                        Y = geoTherm.Apex1.Y,
                        Text = geoTherm.T1.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);

                    _graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex2")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(3);
                    ptSeries.Points.Insert(3, new DataPoint(geoTherm.Apex2.X, geoTherm.Apex2.Y));

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T2") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex2.X,
                        Y = geoTherm.Apex2.Y,
                        Text = geoTherm.T2.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);

                    _graphModel.InvalidatePlot(false);
                }

                if (args.PropertyName == "Apex3")
                {
                    var geoTherm = sender as Geotherm;
                    if (geoTherm == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == geoTherm.Name) as LineSeries;
                    if (ptSeries == null) return;

                    ptSeries.Points.RemoveAt(2);
                    ptSeries.Points.Insert(2, new DataPoint(geoTherm.Apex3.X, geoTherm.Apex3.Y));

                    PointAnnotation curAnnot =
                        _graphModel.Annotations.FirstOrDefault(x => (string)x.Tag == geoTherm.Name + "T3") as PointAnnotation;
                    if (curAnnot == null) return;

                    var newAnot = new PointAnnotation
                    {
                        X = geoTherm.Apex3.X,
                        Y = geoTherm.Apex3.Y,
                        Text = geoTherm.T3.ToString(CultureInfo.CurrentCulture),
                        Fill = OxyColors.Transparent,
                        TextColor = OxyColors.Red,
                        Tag = curAnnot.Tag
                    };

                    _graphModel.Annotations.Remove(curAnnot);
                    _graphModel.Annotations.Add(newAnot);

                    _graphModel.InvalidatePlot(false);
                }

            };
        }

        private void AttachChangeEvents(RockBox rockBox)
        {
            rockBox.PropertyChanged += (sender, args) =>
            {

                if (args.PropertyName == "RockId")
                {
                    AreaSeries arSerie = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rockBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    
                    var rockInd = (rockBox.RockId >= 0) ? rockBox.RockId : rockBox.RockId*-1; 
                    var newRc = GraphConfig.Instace.RocksColor.FirstOrDefault(x => x.Index == rockInd);
                    if (newRc == null) return;
                    arSerie.Fill = newRc.Color;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rockBox.Name) as LineSeries;
                    if (ptSeries == null) return;
                    ptSeries.MarkerFill = newRc.Color;
                }

                if (args.PropertyName == "Apex0")
                {
                    var rBox = sender as RockBox; //todo по-моему это излишне, можно просто ссылаться на аргумент rockBox. Проверить!
                    if (rBox == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[0].X;
                        var y = ptSeries.Points[0].Y;

                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex0.X = x;
                            rBox.Apex0.Y = y;
                            rBox.FreezeLogging = false;
                        };
                        _history.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(0);
                    ptSeries.Points.Insert(0, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    ptSeries.Points.RemoveAt(4);
                    ptSeries.Points.Insert(4, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    AreaSeries arSerie = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points.RemoveAt(0);
                    arSerie.Points.Insert(0, new DataPoint(rBox.Apex0.X, rBox.Apex0.Y));

                    _graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "Apex1")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex1 = new ModPoint(ptSeries.Points[1].X, ptSeries.Points[1].Y);
                            rBox.FreezeLogging = false;
                        };
                        _history.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(1);
                    ptSeries.Points.Insert(1, new DataPoint(rBox.Apex1.X, rBox.Apex1.Y));


                    AreaSeries arSerie = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points2.RemoveAt(0);
                    arSerie.Points2.Insert(0, new DataPoint(rBox.Apex1.X, rBox.Apex1.Y));

                    _graphModel.InvalidatePlot(false);

                }
                if (args.PropertyName == "Apex2")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[3].X;
                        var y = ptSeries.Points[3].Y;

                        var hl = new HistoryLog();
                        hl.Undo = () =>
                        {
                            rBox.FreezeLogging = true;
                            rBox.Apex2.X = x;
                            rBox.Apex2.Y = y;
                            rBox.FreezeLogging = false;
                        };
                        _history.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(3);
                    ptSeries.Points.Insert(3, new DataPoint(rBox.Apex2.X, rBox.Apex2.Y));

                    AreaSeries arSerie = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points.RemoveAt(1);
                    arSerie.Points.Insert(1, new DataPoint(rBox.Apex2.X, rBox.Apex2.Y));

                    _graphModel.InvalidatePlot(false);

                    

                }
                if (args.PropertyName == "Apex3")
                {
                    var rBox = sender as RockBox;
                    if (rBox == null) return;

                    LineSeries ptSeries = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name) as LineSeries;
                    if (ptSeries == null) return;

                    if (!rBox.FreezeLogging)
                    {
                        var x = ptSeries.Points[2].X;
                        var y = ptSeries.Points[2].Y;

                        var hl = new HistoryLog
                        {
                            Undo = () =>
                            {
                                rBox.FreezeLogging = true;
                                rBox.Apex3.X = x;
                                rBox.Apex3.Y = y;
                                rBox.FreezeLogging = false;
                            }
                        };

                        _history.Add(hl);
                    }

                    ptSeries.Points.RemoveAt(2);
                    ptSeries.Points.Insert(2, new DataPoint(rBox.Apex3.X, rBox.Apex3.Y));

                    AreaSeries arSerie = _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == rBox.Name + "area") as AreaSeries;
                    if (arSerie == null) return;
                    arSerie.Points2.RemoveAt(1);
                    arSerie.Points2.Insert(1, new DataPoint(rBox.Apex3.X, rBox.Apex3.Y));

                    _graphModel.InvalidatePlot(false);

                    

                }

            };
        }

        private bool IsPointInPolygon(List<Point> polygon, Point point)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        private bool IsPointInPolygon(List<DataPoint> polygon, DataPoint point)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        private double GetDoubleFromLine(string line)
        {
            line = line.Replace(" ", String.Empty); 
            line = Regex.Match(line.Replace(",", "."), @"^[+-]?\d+\.?\d*").Value;

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                line = line.Replace(".", ",");
            }

            return Config.Tools.ParseOrDefaultDouble(line);
        }

        private double GetNumberFromString(string line)
        {
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                line = line.Replace(".", ",");
            }
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ".")
            {
                line = line.Replace(",", ".");
            }
            return Config.Tools.ParseOrDefaultDouble(line);
        }

        private int GetIntFromLine(string line)
        {
            line = line.Replace(" ", String.Empty); 
            line = Regex.Match(line, @"^[+-]?\d+\.?\d*").Value;

            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                line = line.Replace(".", ",");
            }

            return Config.Tools.ParseOrDefaultInt(line);
        }

        private string NextActualLine(StreamReader sr)
        {
            var line = sr.ReadLine();
            if (String.IsNullOrWhiteSpace(line) || line.StartsWith("/"))
            {
                while (String.IsNullOrWhiteSpace(line) || line.StartsWith("/"))
                {
                    line = sr.ReadLine();
                }
            }
            return line;
        }

        private void Button3_OnClick(object sender, RoutedEventArgs e)
        {

            double x0 = -3.1;
            double x1 = 3.1;
            double y0 = -3;
            double y1 = 3;
            Func<double, double, double> peaks = (x, y) => 3 * (1 - x) * (1 - x) * Math.Exp(-(x * x) - (y + 1) * (y + 1)) - 10 * (x / 5 - x * x * x - y * y * y * y * y) * Math.Exp(-x * x - y * y) - 1.0 / 3 * Math.Exp(-(x + 1) * (x + 1) - y * y);
            var xx = ArrayBuilder.CreateVector(x0, x1, 100);
            var yy = ArrayBuilder.CreateVector(y0, y1, 100);
            var peaksData = ArrayBuilder.Evaluate(peaks, xx, yy);

            
        }

        private void NodesVisibilityBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_graphModel == null) return;
            foreach (var seria in _graphModel.Series)
            {
                if (!(seria.Tag.ToString()).Contains("area") && !(seria.Tag.ToString()).Contains("Geotherm"))
                {
                    seria.IsVisible = true;
                    _graphModel.InvalidatePlot(false);
                }
            }
        }

        private void NodesVisibilityBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (_graphModel == null) return;
            foreach (var seria in _graphModel.Series)
            {
                if (!(seria.Tag.ToString()).Contains("area") && !(seria.Tag.ToString()).Contains("Geotherm"))
                {
                    seria.IsVisible = false;
                    _graphModel.InvalidatePlot(false);
                }
            }
        }

        private void GeometryHideBox_OnChecked(object sender, RoutedEventArgs e)
        {
            TablesViewer.Visibility = Visibility.Visible;
            HidableColumn.Width = new GridLength(1, GridUnitType.Star);
            //TablesGrid.Visibility = Visibility.Visible;
            //GeometryDataGrid.Visibility = Visibility.Visible;
        }

        private void GeometryHideBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            TablesViewer.Visibility = Visibility.Collapsed;
            HidableColumn.Width = new GridLength(1, GridUnitType.Auto);
            //TablesGrid.Visibility = Visibility.Collapsed;
            //GeometryDataGrid.Visibility = Visibility.Collapsed;
        }

        private void RemoveBoxButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArea != null)
            {
                _graphModel.Series.Remove(_selectedArea);
                
                var boxName = _selectedArea.Tag.ToString();
                boxName = boxName.Substring(0, boxName.Length - 4);

                areas.Remove(boxName);

                var lineSeria =
                   _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == boxName);
                if (lineSeria != null) _graphModel.Series.Remove(lineSeria);

                var rBox = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == boxName);
                if (rBox != null) _currentConfig.RockBoxes.Remove(rBox);
                GeometryDataGrid.Items.Refresh();

                _graphModel.InvalidatePlot(false);
                _selectedArea = null;
            }
        }

        private void LayerUpButton_Click(object sender, RoutedEventArgs e)
        {

            if (_selectedArea != null)
            {
                int oldIndex = _graphModel.Series.IndexOf(_selectedArea);
                if (oldIndex == areas.Count - 1) return;

                _graphModel.Series.Remove(_selectedArea);
                _graphModel.Series.Insert(oldIndex + 1, _selectedArea);

                var boxName = _selectedArea.Tag.ToString();
                boxName = boxName.Substring(0, boxName.Length - 4);

                var lineSeria =
                   _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == boxName);
                _graphModel.Series.Remove(lineSeria);
                _graphModel.Series.Insert(oldIndex + areas.Count + 1, lineSeria);

                var rBox = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == boxName);
                _currentConfig.RockBoxes.Remove(rBox);
                _currentConfig.RockBoxes.Insert(oldIndex + 1, rBox);

                GeometryDataGrid.Items.Refresh();
                _graphModel.InvalidatePlot(false);
            }
        }

        private void LayerDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArea != null)
            {
                int oldIndex = _graphModel.Series.IndexOf(_selectedArea);
                if (oldIndex == 0) return;

                _graphModel.Series.Remove(_selectedArea);
                _graphModel.Series.Insert(oldIndex - 1, _selectedArea);

                var boxName = _selectedArea.Tag.ToString();
                boxName = boxName.Substring(0, boxName.Length - 4);

                var lineSeria =
                   _graphModel.Series.FirstOrDefault(x => x.Tag.ToString() == boxName);
                _graphModel.Series.Remove(lineSeria);
                _graphModel.Series.Insert(oldIndex + areas.Count - 1, lineSeria);

                var rBox = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == boxName);
                _currentConfig.RockBoxes.Remove(rBox);
                _currentConfig.RockBoxes.Insert(oldIndex - 1, rBox);

                GeometryDataGrid.Items.Refresh();
                _graphModel.InvalidatePlot(false);
            }
        }

        private void InitUndoButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var currentUndo = _history.ShowNextUndo();
            //if (currentUndo == null) return;
            //currentUndo.Undo();

            _history.Undo();

            //_graphModel.InvalidatePlot(false);
        }

        private void InitRedoButton_OnClick(object sender, RoutedEventArgs e)
        {
            _history.Redo();
            //var currentRedo = _history.ShowNextRedo();
            //if (currentRedo == null) return;
            //currentRedo.Do();

            //_graphModel.InvalidatePlot(false);
        }

        private void GeothermsBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_graphModel == null) return;
            foreach (var seria in _graphModel.Series)
            {
                if ((seria.Tag.ToString()).Contains("Geotherm"))
                {
                    seria.IsVisible = true;
                    _graphModel.InvalidatePlot(false);
                }
            }

            foreach (var annotation in _graphModel.Annotations)
            {
                annotation.TextColor = OxyColors.Red;
            }

        }

        private void GeothermsBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (_graphModel == null) return;
            foreach (var seria in _graphModel.Series)
            {
                if ((seria.Tag.ToString()).Contains("Geotherm"))
                {
                    seria.IsVisible = false;
                    _graphModel.InvalidatePlot(false);
                }
            }

            foreach (var annotation in _graphModel.Annotations)
            {
                annotation.TextColor =  OxyColors.Transparent;
            }

            ClearTermoboxSelection();

        }

        private void ClearTermoboxSelection()
        {
            if (_selectedTermoBox != null)
            {
                _selectedTermoBox.Color = OxyColors.Orange;
                _selectedTermoBox = null;
            }
        }

        private void SaveAsInitButton_OnClick(object sender, RoutedEventArgs e)
        {
         
            var svDialog = new SaveFileDialog();
            
            svDialog.Filter = "Init-файл (*.t3c)|*.t3c";
            
            var res = svDialog.ShowDialog();

            if (res == false) return;
            

            var sfName = svDialog.FileName;

            SaveInitToFile(sfName);

        }

        private void PseudoSaveInitToFile(string sfName)
        {
            
            using (StreamWriter file = new StreamWriter(sfName))
            {

                using (StreamReader rFile = new StreamReader("init.t3c"))
                {

                    var line = "";
                    var tct = 0;
                    while (tct != 2)
                    {
                        line = rFile.ReadLine();
                        file.WriteLine(line);
                        if (line.Contains("~")) tct++;
                    }

                }

                file.WriteLine();

                file.WriteLine(@"/ROCKS_BOXES_DESCRIPTION");
                file.WriteLine(@"/0-2");
                file.WriteLine(@"/1-3");
                file.WriteLine(@"/x=X/xsize");
                file.WriteLine(@"/y=Y/ysize");
                file.WriteLine(@"/Type:");
                file.WriteLine(@"/0__simple_rectangle");
                file.WriteLine(@"/1__simple_additional_rectangle");
                file.WriteLine(@"/2__simple_rectangle_with_changing_of_markers_type");
                file.WriteLine(@"/3__simple_circular_pattern");
                file.WriteLine(@"/4__simple_circular_pattern_with_changing_of_markers_type");
                file.WriteLine(@"/Type__Rock__x0____________y0____________x1___________y1_____________x2_____________y2_______________x3_______________y3");

                foreach (var rockBox in _currentConfig.RockBoxes)
                {
                    var x0 = (Math.Abs(rockBox.Apex0.X) <= _currentConfig.Grid.Xsize && rockBox.Apex0.X > 0) ? "m" + rockBox.Apex0.X.ToString("0.0") : Math.Round((rockBox.Apex0.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y0 = (Math.Abs(rockBox.Apex0.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex0.Y > 0) ? "m" + rockBox.Apex0.Y.ToString("0.0") : Math.Round((rockBox.Apex0.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x1 = (Math.Abs(rockBox.Apex1.X) <= _currentConfig.Grid.Xsize && rockBox.Apex1.X > 0) ? "m" + rockBox.Apex1.X.ToString("0.0") : Math.Round((rockBox.Apex1.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y1 = (Math.Abs(rockBox.Apex1.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex1.Y > 0) ? "m" + rockBox.Apex1.Y.ToString("0.0") : Math.Round((rockBox.Apex1.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x2 = (Math.Abs(rockBox.Apex2.X) <= _currentConfig.Grid.Xsize && rockBox.Apex2.X > 0) ? "m" + rockBox.Apex2.X.ToString("0.0") : Math.Round((rockBox.Apex2.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y2 = (Math.Abs(rockBox.Apex2.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex2.Y > 0) ? "m" + rockBox.Apex2.Y.ToString("0.0") : Math.Round((rockBox.Apex2.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x3 = (Math.Abs(rockBox.Apex3.X) <= _currentConfig.Grid.Xsize && rockBox.Apex3.X > 0) ? "m" + rockBox.Apex3.X.ToString("0.0") : Math.Round((rockBox.Apex3.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y3 = (Math.Abs(rockBox.Apex3.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex3.Y > 0) ? "m" + rockBox.Apex3.Y.ToString("0.0") : Math.Round((rockBox.Apex3.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");

                    //file.WriteLine("   {0}          {1}      {2}      {3}  {4}    {5}     {6}    {7}   {8}    {9}", rockBox.RockType, rockBox.RockId, x0,y0,x1,y1,x2,y2,x3,y3);
                    file.WriteLine(String.Format("   {0,-15}{1,-8}{2,-16}{3,-10}{4,-14}{5,-14}{6,-14}{7,-11}{8,-14}{9}", rockBox.RockType, (rockBox.RockId >= 0) ? rockBox.RockId.ToString() : "i" + Math.Abs(rockBox.RockId), x0, y0, x1, y1, x2, y2, x3, y3).Replace(",", "."));
                }
                file.WriteLine(@"~");
                file.WriteLine();

                file.WriteLine(@"/T_STRUCTURE_DESCRIPTION");
                file.WriteLine(@"/T");
                file.WriteLine(@"/0-2");
                file.WriteLine(@"/1-3");
                file.WriteLine(@"/x=X/size");
                file.WriteLine(@"/y=Y/size");
                file.WriteLine(@"/t=T(K)");
                file.WriteLine();
                file.WriteLine(@"/INITIAL_GEOTHERM");

                foreach (var geoTherm in _currentConfig.Geotherms)
                {
                    var x0 = (Math.Abs(geoTherm.Apex0.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex0.X > 0) ? "m" + geoTherm.Apex0.X.ToString("0.#") : Math.Round((geoTherm.Apex0.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y0 = (Math.Abs(geoTherm.Apex0.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex0.Y > 0) ? "m" + geoTherm.Apex0.Y.ToString("0.#") : Math.Round((geoTherm.Apex0.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x1 = (Math.Abs(geoTherm.Apex1.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex1.X > 0) ? "m" + geoTherm.Apex1.X.ToString("0.#") : Math.Round((geoTherm.Apex1.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y1 = (Math.Abs(geoTherm.Apex1.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex1.Y > 0) ? "m" + geoTherm.Apex1.Y.ToString("0.#") : Math.Round((geoTherm.Apex1.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x2 = (Math.Abs(geoTherm.Apex2.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex2.X > 0) ? "m" + geoTherm.Apex2.X.ToString("0.#") : Math.Round((geoTherm.Apex2.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y2 = (Math.Abs(geoTherm.Apex2.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex2.Y > 0) ? "m" + geoTherm.Apex2.Y.ToString("0.#") : Math.Round((geoTherm.Apex2.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x3 = (Math.Abs(geoTherm.Apex3.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex3.X > 0) ? "m" + geoTherm.Apex3.X.ToString("0.#") : Math.Round((geoTherm.Apex3.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y3 = (Math.Abs(geoTherm.Apex3.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex3.Y > 0) ? "m" + geoTherm.Apex3.Y.ToString("0.#") : Math.Round((geoTherm.Apex3.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");

                    if (Math.Abs(geoTherm.LeftOceanicAge) < 0.1 && Math.Abs(geoTherm.RightOceanicAge) < 0.1 &&
                        Math.Abs(geoTherm.ThermalDiffusivity) < 0.1)
                    {
                        file.WriteLine(String.Format("{0}       {1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}   {9}   {10}   {11}   {12}", geoTherm.GeothermType, x0, y0, x1, y1, x2, y2, x3, y3, geoTherm.T0.ToString("0.#"), geoTherm.T1.ToString("0.#"), geoTherm.T2.ToString("0.#"), geoTherm.T3.ToString("0.#")).Replace(",", "."));
                    }
                    else
                    {
                        file.WriteLine(String.Format("{0}       {1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}   {9}   {10}   {11}   {12}   {13}   {14}   {15}", geoTherm.GeothermType, x0, y0, x1, y1, x2, y2, x3, y3, geoTherm.T0.ToString("0.#"), geoTherm.T1.ToString("0.#"), geoTherm.T2.ToString("0.#"), geoTherm.T3.ToString("0.#"), geoTherm.LeftOceanicAge.ToString("0e+00"), geoTherm.RightOceanicAge.ToString("0e+00"), geoTherm.ThermalDiffusivity.ToString("0e+00")).Replace(",", "."));

                    }

                }
                file.Write(@"~");

            }
        }

        private void SaveInitToFile(string sfName)
        {
            using (StreamWriter file = new StreamWriter(File.Open(sfName, FileMode.Create), Encoding.ASCII))
            {

                file.WriteLine(@"/GRID_PARAMETERS_DESCRIPTIONS");
                file.WriteLine("{0,10}-xnumx", _currentConfig.Grid.Xnumx);
                file.WriteLine("{0,10}-ynumy", _currentConfig.Grid.Ynumy);
                file.WriteLine("{0,10}-mnumx", _currentConfig.Grid.Mnumx);
                file.WriteLine("{0,10}-Mnumy {1} {2}", _currentConfig.Grid.Mnumy, _currentConfig.Grid.Mnumy_left.ToString("+0;-#"), _currentConfig.Grid.Mnumy_rigth.ToString("+0;-#"));
                file.WriteLine("{0,10}-xsize(m)", _currentConfig.Grid.Xsize);
                file.WriteLine("{0,10}-ysize(m)", _currentConfig.Grid.Ysize);
                file.WriteLine("{0,10}-pinit(Pa)", _currentConfig.Grid.Pinit.ToString("0000"));
                file.WriteLine("{0,10}-GXKOEF(m/sek^2)", _currentConfig.Grid.GxKoef.ToString().Replace(",", "."));
                file.WriteLine("{0,10}-GYKOEF(m/sek^2)", _currentConfig.Grid.GyKoef.ToString().Replace(",", "."));
                
                file.WriteLine("{0}-timesum(Years)", _currentConfig.Grid.TimeSum);
                file.WriteLine("{0}-nonstab {1}-dx {2}-dy", _currentConfig.Grid.NonStab, _currentConfig.Grid.dx.ToString().Replace(",", "."), _currentConfig.Grid.dy.ToString().Replace(",", "."));
                file.WriteLine();
                file.WriteLine(@"/MARKERS_TYPES_FILE_NAME_Y(Name)_N(0)");
                file.WriteLine(_currentConfig.MarkerTypesFileName);
                file.WriteLine(@"/DATA_OUTPUT_FILE_NAME____TYPE");
                file.WriteLine("{0}  {1}                        ", _currentConfig.OutputPrnFile, _currentConfig.OutputType);
                file.WriteLine();
                file.WriteLine(@"/ROCKS_DESCRIPTIONS/___NUM_______________NU(Pa^MM*s)___DE(J)_________DV(J/bar)_____SS(Pa)________MM(Power)_____LL(KOEF)____RO(kg/M^3)____bRo(1/K)______aRo(1/kbar)___CP(J/kg)______Kt(Wt/(m*K))__Ht(Wt/kg)");

                foreach (var rock in _currentConfig.Rocks)
                {
                    #region ParametersCommnets
                    //RockNum = GetIntFromLine(props[0]), //b0 = GetNumberFromString(props[13]),
                    //n0 = GetNumberFromString(props[1]), //b1 = GetNumberFromString(props[14]),
                    //n1 = GetNumberFromString(props[2]), //e0 = GetNumberFromString(props[15]),
                    //s0 = GetNumberFromString(props[3]), //e1 = GetNumberFromString(props[16]),
                    //s1 = GetNumberFromString(props[4]), //Ro = GetNumberFromString(props[17]),
                    //Nu = GetNumberFromString(props[5]), //bRo = GetNumberFromString(props[18]),
                    //dh = GetNumberFromString(props[6]), //aRo = GetNumberFromString(props[19]),
                    //Dv = GetNumberFromString(props[7]), //Cp = GetNumberFromString(props[20]),
                    //Ss = GetNumberFromString(props[8]), //Kt = GetNumberFromString(props[21]),
                    //Mm = GetNumberFromString(props[9]), //kf = GetNumberFromString(props[22]),
                    //Ll = GetNumberFromString(props[10]), //kp = GetNumberFromString(props[23]),
                    //a0 = GetNumberFromString(props[11]), //Ht = GetNumberFromString(props[24])
                    //a1 = GetNumberFromString(props[12]),
                    #endregion
                    if (rock.RockName != null) file.WriteLine("/{0}", rock.RockName);
                    file.WriteLine(String.Format("      {0,-4}{1} {2} {3} {4}        {5,8}      {6,8}      {7,8}      {8,8}     {9,8}  {10} {11} {12} {13} {14} {15} {16}  {17,8}      {18,8}      {19,8}      {20,8}      {21,8}  {22,9}  {23,8}  {24,8}",
                        rock.RockNum, rock.n0.ToString("0e+00"), rock.n1.ToString("0e+00"), rock.s0.ToString("0e+00"),
                        rock.s1.ToString("0e+00"), rock.Nu.ToString("0.00E+00"), rock.dh.ToString("0.00E+00"), rock.Dv.ToString("0.00E+00"),
                        rock.Ss.ToString("0.00E+00"), rock.Mm.ToString("0.00E+00"), rock.Ll.ToString("0.00"), rock.a0.ToString("0e+00"), rock.a1.ToString("0e+00"),
                        rock.b0.ToString("0.000"), rock.b1.ToString("0.000"), rock.e0.ToString("0.0"), rock.e1.ToString("0.0"), rock.Ro.ToString("0.00E+00"),
                        rock.bRo.ToString("0.00E+00"), rock.aRo.ToString("0.00E+00"), rock.Cp.ToString("0.00E+00"), rock.Kt.ToString("0.00E+00"), rock.kf.ToString("0.00E+00"), rock.kp.ToString("0.00E+00"), rock.Ht.ToString("0.00E-00")).Replace(",","."));
                }
                file.WriteLine(@"~");
                file.WriteLine();
                foreach (var line in _currentConfig.kostyl)
                {
                    file.WriteLine(line);
                }
                file.WriteLine(@"~");
                file.WriteLine();

                file.WriteLine(@"/ROCKS_BOXES_DESCRIPTION");
                file.WriteLine(@"/0-2");
                file.WriteLine(@"/1-3");
                file.WriteLine(@"/x=X/xsize");
                file.WriteLine(@"/y=Y/ysize");
                file.WriteLine(@"/Type:");
                file.WriteLine(@"/0__simple_rectangle");
                file.WriteLine(@"/1__simple_additional_rectangle");
                file.WriteLine(@"/2__simple_rectangle_with_changing_of_markers_type");
                file.WriteLine(@"/3__simple_circular_pattern");
                file.WriteLine(@"/4__simple_circular_pattern_with_changing_of_markers_type");
                file.WriteLine(@"/Type__Rock__x0____________y0____________x1___________y1_____________x2_____________y2_______________x3_______________y3");

                foreach (var rockBox in _currentConfig.RockBoxes)
                {
                    var x0 = (Math.Abs(rockBox.Apex0.X) <= _currentConfig.Grid.Xsize && rockBox.Apex0.X > 0) ? "m" + rockBox.Apex0.X.ToString("0.0") : Math.Round((rockBox.Apex0.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y0 = (Math.Abs(rockBox.Apex0.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex0.Y > 0) ? "m" + rockBox.Apex0.Y.ToString("0.0") : Math.Round((rockBox.Apex0.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x1 = (Math.Abs(rockBox.Apex1.X) <= _currentConfig.Grid.Xsize && rockBox.Apex1.X > 0) ? "m" + rockBox.Apex1.X.ToString("0.0") : Math.Round((rockBox.Apex1.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y1 = (Math.Abs(rockBox.Apex1.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex1.Y > 0) ? "m" + rockBox.Apex1.Y.ToString("0.0") : Math.Round((rockBox.Apex1.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x2 = (Math.Abs(rockBox.Apex2.X) <= _currentConfig.Grid.Xsize && rockBox.Apex2.X > 0) ? "m" + rockBox.Apex2.X.ToString("0.0") : Math.Round((rockBox.Apex2.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y2 = (Math.Abs(rockBox.Apex2.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex2.Y > 0) ? "m" + rockBox.Apex2.Y.ToString("0.0") : Math.Round((rockBox.Apex2.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x3 = (Math.Abs(rockBox.Apex3.X) <= _currentConfig.Grid.Xsize && rockBox.Apex3.X > 0) ? "m" + rockBox.Apex3.X.ToString("0.0") : Math.Round((rockBox.Apex3.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y3 = (Math.Abs(rockBox.Apex3.Y) <= _currentConfig.Grid.Ysize && rockBox.Apex3.Y > 0) ? "m" + rockBox.Apex3.Y.ToString("0.0") : Math.Round((rockBox.Apex3.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");

                    //file.WriteLine("   {0}          {1}      {2}      {3}  {4}    {5}     {6}    {7}   {8}    {9}", rockBox.RockType, rockBox.RockId, x0,y0,x1,y1,x2,y2,x3,y3);
                    file.WriteLine(String.Format("   {0,-15}{1,-8}{2,-16}{3,-10}{4,-14}{5,-14}{6,-14}{7,-11}{8,-14}{9}", rockBox.RockType, (rockBox.RockId >= 0) ? rockBox.RockId.ToString() : "i" + Math.Abs(rockBox.RockId), x0, y0, x1, y1, x2, y2, x3, y3).Replace(",","."));
                }
                file.WriteLine(@"~");
                file.WriteLine();

                file.WriteLine(@"/T_STRUCTURE_DESCRIPTION");
                file.WriteLine(@"/T");
                file.WriteLine(@"/0-2");
                file.WriteLine(@"/1-3");
                file.WriteLine(@"/x=X/size");
                file.WriteLine(@"/y=Y/size");
                file.WriteLine(@"/t=T(K)");
                file.WriteLine();
                file.WriteLine(@"/INITIAL_GEOTHERM");

                foreach (var geoTherm in _currentConfig.Geotherms)
                {
                    var x0 = (Math.Abs(geoTherm.Apex0.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex0.X > 0 ) ? "m" + geoTherm.Apex0.X.ToString("0.#") : Math.Round((geoTherm.Apex0.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y0 = (Math.Abs(geoTherm.Apex0.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex0.Y > 0) ? "m" + geoTherm.Apex0.Y.ToString("0.#") : Math.Round((geoTherm.Apex0.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x1 = (Math.Abs(geoTherm.Apex1.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex1.X > 0) ? "m" + geoTherm.Apex1.X.ToString("0.#") : Math.Round((geoTherm.Apex1.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y1 = (Math.Abs(geoTherm.Apex1.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex1.Y > 0) ? "m" + geoTherm.Apex1.Y.ToString("0.#") : Math.Round((geoTherm.Apex1.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x2 = (Math.Abs(geoTherm.Apex2.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex2.X > 0) ? "m" + geoTherm.Apex2.X.ToString("0.#") : Math.Round((geoTherm.Apex2.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y2 = (Math.Abs(geoTherm.Apex2.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex2.Y > 0) ? "m" + geoTherm.Apex2.Y.ToString("0.#") : Math.Round((geoTherm.Apex2.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");
                    var x3 = (Math.Abs(geoTherm.Apex3.X) <= _currentConfig.Grid.Xsize && geoTherm.Apex3.X > 0) ? "m" + geoTherm.Apex3.X.ToString("0.#") : Math.Round((geoTherm.Apex3.X / _currentConfig.Grid.Xsize), 1).ToString("0.#");
                    var y3 = (Math.Abs(geoTherm.Apex3.Y) <= _currentConfig.Grid.Ysize && geoTherm.Apex3.Y > 0) ? "m" + geoTherm.Apex3.Y.ToString("0.#") : Math.Round((geoTherm.Apex3.Y / _currentConfig.Grid.Ysize), 1).ToString("0.#");

                    if (Math.Abs(geoTherm.LeftOceanicAge) < 0.1 && Math.Abs(geoTherm.RightOceanicAge) < 0.1 &&
                        Math.Abs(geoTherm.ThermalDiffusivity) < 0.1)
                    {
                        file.WriteLine(String.Format("{0}       {1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}   {9}   {10}   {11}   {12}", geoTherm.GeothermType, x0, y0, x1, y1, x2, y2, x3, y3, geoTherm.T0.ToString("0.#"), geoTherm.T1.ToString("0.#"), geoTherm.T2.ToString("0.#"), geoTherm.T3.ToString("0.#")).Replace(",","."));
                    }
                    else
                    {
                        file.WriteLine(String.Format("{0}       {1}  {2}  {3}  {4}  {5}  {6}  {7}  {8}   {9}   {10}   {11}   {12}   {13}   {14}   {15}", geoTherm.GeothermType, x0, y0, x1, y1, x2, y2, x3, y3, geoTherm.T0.ToString("0.#"), geoTherm.T1.ToString("0.#"), geoTherm.T2.ToString("0.#"), geoTherm.T3.ToString("0.#"), geoTherm.LeftOceanicAge.ToString("0e+00"), geoTherm.RightOceanicAge.ToString("0e+00"), geoTherm.ThermalDiffusivity.ToString("0e+00")).Replace(",","."));

                    }

                }
                file.Write(@"~");

            }
        }

        private void Button4_OnClick(object sender, RoutedEventArgs e)
        {

        }

        private void AttachMovingEvents(LineSeries points)
        {
            int indexOfPointToMove = -1;

            // Subscribe to the mouse down event on the line series
            points.MouseDown += (s, ev) =>
            {
                // only handle the left mouse button (right button can still be used to pan)
                if (ev.ChangedButton == OxyMouseButton.Left)
                {

                    int indexOfNearestPoint = (int)Math.Round(ev.HitTestResult.Index);
                    var nearestPoint = points.Transform(points.Points[indexOfNearestPoint]);

                    // Check if we are near a point
                    if ((nearestPoint - ev.Position).Length < 10)
                    {
                        // Start editing this point
                        indexOfPointToMove = indexOfNearestPoint;
                    }
                    else
                    {
                        return;
                    }


                    // Change the linestyle while editing
                    points.LineStyle = LineStyle.DashDot;

                    _ruleX = new LineSeries();
                    _ruleY = new LineSeries();

                    _ruleX.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, _graphModel.Axes[0].ActualMaximum));
                    _ruleX.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y));
                    _ruleX.LineStyle = LineStyle.Dot;
                    _ruleX.StrokeThickness = 1;

                    _ruleY.Points.Add(new DataPoint(_graphModel.Axes[1].ActualMinimum, points.Points[indexOfPointToMove].Y));
                    _ruleY.Points.Add(new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y));
                    _ruleY.LineStyle = LineStyle.Dot;
                    _ruleY.StrokeThickness = 1;

                    _ruleX.Color = OxyColors.Red;
                    _ruleY.Color = OxyColors.Red;

                    _graphModel.Series.Add(_ruleX);
                    _graphModel.Series.Add(_ruleY);

                    // Remember to refresh/invalidate of the plot
                    _graphModel.InvalidatePlot(false);


                    IBox rb;
                    if (points.Tag.ToString().Contains("Geotherm"))
                    {
                        rb = _currentConfig.Geotherms.FirstOrDefault(x => x.Name == points.Tag.ToString());
                    }
                    else
                    {
                        rb = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());
                    }
                    
                    
                    if (rb != null)
                    {
                        rb.FreezeLogging = true;
                        var hl = new HistoryLog();

                        var x0 = rb.Apex0.X;
                        var y0 = rb.Apex0.Y;
                        var x1 = rb.Apex1.X;
                        var y1 = rb.Apex1.Y;
                        var x2 = rb.Apex2.X;
                        var y2 = rb.Apex2.Y;
                        var x3 = rb.Apex3.X;
                        var y3 = rb.Apex3.Y;

                        hl.Undo = () =>
                        {
                            rb.FreezeLogging = true;
                            rb.Apex0.X = x0;
                            rb.Apex0.Y = y0;
                            rb.Apex1.X = x1;
                            rb.Apex1.Y = y1;
                            rb.Apex2.X = x2;
                            rb.Apex2.Y = y2;
                            rb.Apex3.X = x3;
                            rb.Apex3.Y = y3;
                            rb.FreezeLogging = false;
                        };
                        _history.Add(hl);
                        
                    }

                    // Set the event arguments to handled - no other handlers will be called.
                    ev.Handled = true;
                }
            };

            points.MouseMove += (s, ev) =>
            {
                if (indexOfPointToMove >= 0)
                {
                    if (indexOfPointToMove == 4)
                    {
                        points.Points[0] = points.InverseTransform(ev.Position);
                        //_graphModel.InvalidatePlot(false);
                        //ev.Handled = true;
                    }
                    if (indexOfPointToMove == 0)
                    {
                        points.Points[4] = points.InverseTransform(ev.Position);
                        //_graphModel.InvalidatePlot(false);
                        //ev.Handled = true;
                    }

                    points.Points[indexOfPointToMove] = (ev.IsControlDown) ? AdjustDataPoint(points.InverseTransform(ev.Position), (int) _graphModel.Axes[0].ActualMinorStep, (int) _graphModel.Axes[1].ActualMinorStep) : points.InverseTransform(ev.Position);

                    _ruleX.Points[0] = new DataPoint(points.Points[indexOfPointToMove].X, _graphModel.Axes[0].ActualMaximum);
                    _ruleX.Points[1] = new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y);
                    _ruleY.Points[0] = new DataPoint(Int32.MinValue, points.Points[indexOfPointToMove].Y);
                    _ruleY.Points[1] = new DataPoint(points.Points[indexOfPointToMove].X, points.Points[indexOfPointToMove].Y);

                    _graphModel.InvalidatePlot(false);
                    ev.Handled = true;
                }
            };

            points.MouseUp += (s, ev) =>
            {
                //var currentBox = areas[points.Tag.ToString()];

               // IBox rb = (points.Tag.ToString().Contains("Geotherm")) ? _currentConfig.Geotherms.FirstOrDefault(x=>x.Name == points.Tag.ToString()) : _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());
                
                IBox rb;

                if ((points.Tag.ToString().Contains("Geotherm")))
                {
                    rb = _currentConfig.Geotherms.FirstOrDefault(x => x.Name == points.Tag.ToString());
                }
                else
                {
                    rb = _currentConfig.RockBoxes.FirstOrDefault(x => x.Name == points.Tag.ToString());
                }

                if (rb != null)
                {

                    if (indexOfPointToMove == 0 || indexOfPointToMove == 4)
                    {
                        rb.Apex0 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }
                    if (indexOfPointToMove == 1)
                    {
                        rb.Apex1 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));

                    }
                    if (indexOfPointToMove == 2)
                    {
                        rb.Apex3 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }
                    if (indexOfPointToMove == 3)
                    {
                        rb.Apex2 = new ModPoint((Convert.ToInt32(points.Points[indexOfPointToMove].X) / 100) * 100, (Convert.ToInt32(points.Points[indexOfPointToMove].Y / 100) * 100));
                    }

                    if (_history.Logs != null && _history.Logs.Count > 0)
                    {
                        var hl = _history.Logs.Last();
                        if (hl.Do == null)
                        {
                            var x0 = rb.Apex0.X;
                            var y0 = rb.Apex0.Y;
                            var x1 = rb.Apex1.X;
                            var y1 = rb.Apex1.Y;
                            var x2 = rb.Apex2.X;
                            var y2 = rb.Apex2.Y;
                            var x3 = rb.Apex3.X;
                            var y3 = rb.Apex3.Y;

                            hl.Do = () =>
                            {
                                rb.FreezeLogging = true;
                                rb.Apex0.X = x0;
                                rb.Apex0.Y = y0;
                                rb.Apex1.X = x1;
                                rb.Apex1.Y = y1;
                                rb.Apex2.X = x2;
                                rb.Apex2.Y = y2;
                                rb.Apex3.X = x3;
                                rb.Apex3.Y = y3;
                                rb.FreezeLogging = false;
                            };
                        }
                    }

                    
                    
                    
                    rb.FreezeLogging = false;
                }

                GeometryDataGrid.Items.Refresh();

                _graphModel.Series.Remove(_ruleX);
                _graphModel.Series.Remove(_ruleY);

                // Stop editing
                indexOfPointToMove = -1;
                points.LineStyle = LineStyle.Solid;
                _graphModel.InvalidatePlot(false);
                ev.Handled = true;

            };

        }

        private int AdjustValue(double value, int accuracy)
        {
            var rem = value % accuracy;
            if (rem > accuracy/2d)
            {
                return Convert.ToInt32((value - rem) + accuracy);
            }
            else
            {
                return Convert.ToInt32(value - rem);
            }
        }

        private DataPoint AdjustDataPoint(DataPoint pt, int xAccuracy, int yAccuracy)
        {
            return new DataPoint(AdjustValue(pt.X, xAccuracy), AdjustValue(pt.Y, yAccuracy) );
        }

        private void CommitInitButton_OnClick(object sender, RoutedEventArgs e)
        {

            //var mw = new ModelNameWindow() {Owner = this};
            //if (mw.ShowDialog() == false) return;
            //var modelName = ((App) Application.Current).CurrentModelName;

            if (!Directory.Exists("temp")) Directory.CreateDirectory("temp");

            var uploadfile = "temp\\init.t3c";

            SaveInitToFile(uploadfile);
            //PseudoSaveInitToFile(uploadfile);

            var btw = new BatchTaskWindow(uploadfile) {Owner = this};
            btw.ShowDialog();

            //var keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath, Config.Config.Instace.UserFingerprint);
            //var keyFiles = new[] { keyFile };
            //var username = Config.Config.Instace.UserLogin;

            //var methods = new List<AuthenticationMethod>();
            //methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            //var con = new ConnectionInfo(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, methods.ToArray());

            //bool dirExists;

            //string result;
            //using (var sshclient = new SshClient(con))
            //{
            //    sshclient.Connect();
            //    //TODO рабочая директория
            //    var commandResponse = sshclient.RunCommand("module add slurm\ncd _scratch\ncd collision\nls");
            //    result = commandResponse.Result;
            //    var dirItems = result.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();

            //    dirExists = dirItems.Contains("base_lomonosov");

            //    using (var sftpclient = new SftpClient(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, keyFile))
            //    {
            //        sftpclient.Connect();

            //        var destinationFolder = modelName;

            //        if (dirExists)
            //        {
            //            //var cr = sshclient.RunCommand("scp -r base_lomonosov " + destinationFolder);
            //            var cr = sshclient.RunCommand("module add slurm\ncd _scratch\ncd collision\nscp -r base_lomonosov " + destinationFolder);
            //            result = cr.Result;

            //            sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/collision/" + destinationFolder);
            //            //sftpclient.DeleteFile(@"init.t3c");

            //            using (var fileStream = new FileStream(uploadfile, FileMode.Open))
            //            {
            //                sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
            //                sftpclient.UploadFile(fileStream, Path.GetFileName(uploadfile), true);
            //            }

            //        }
            //        else
            //        {
            //            //TODO копирование с локалки 
            //        }

            //        //sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/collision/"+destinationFolder);

            //        //// var listDirectory = client.ListDirectory(workingdirectory);

            //        ////foreach (var fi in listDirectory)
            //        ////{
            //        ////    gg += (" - " + fi.Name + "\n");
            //        ////}

            //        //using (var fileStream = new FileStream(uploadfile, FileMode.Open))
            //        //{
            //        //    sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
            //        //    sftpclient.UploadFile(fileStream, Path.GetFileName(uploadfile));
            //        //}
            //    }

            //}

        }

        private void LoadTxtSourceButton_OnClick(object sender, RoutedEventArgs e)
        {
            fbd = new FolderBrowserDialog();
            fbd.SelectedPath = @"E:\Sergey\Univer\postgrad\results\inversed";
            fbd.ShowNewFolderButton = false;

            DialogResult result = fbd.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) return;

            ResultListBox.Items.Clear();
            isPrn = false;

            files = Directory.GetFiles(fbd.SelectedPath).ToList().Where(x=>x.Contains(".txt")).ToList();

            if (files.Count == 0)
            {
                MessageBox.Show("В указаном каталоге нет подходящих файлов!");
                return;
            }

            foreach (var file in files.OrderBy(Config.Tools.ExtractNumberFromString ))
            {
                var cInd = file.LastIndexOf("c", StringComparison.Ordinal);
                //var templ = file.Substring(0, cInd);
                if (cInd == -1) continue;

                var tAnalog = file.Remove(cInd, 1).Insert(cInd,"t");

                if (files.Contains(tAnalog))
                    ResultListBox.Items.Add(String.Format("{0} | {1}", file.Substring(fbd.SelectedPath.Length + 1),tAnalog.Substring(fbd.SelectedPath.Length+1)));
            }

            isPrn = false;
            PrnParsStackPanel.Visibility = Visibility.Collapsed;
            OverlayStackPanel.Visibility = Visibility.Visible;

        }

        private void LoadPrnSourceButton_OnClick(object sender, RoutedEventArgs e)
        {
            
            fbd = new FolderBrowserDialog();
            fbd.SelectedPath = @"E:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500";
            fbd.ShowNewFolderButton = false;

            DialogResult result = fbd.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) return;

            files = Directory.GetFiles(fbd.SelectedPath).ToList().Where(x => x.EndsWith(".prn")).ToList();

            if (files.Count == 0)
            {
                MessageBox.Show("В указаном каталоге нет подходящих файлов!");
                return;
            }

            // TODO подумать как лучше определить поведение добавление файлов в ResultListBox (чистится ли он полностью или добавляются новые)
            ResultListBox.Items.Clear();
            foreach (var file in files)
            {
                ResultListBox.Items.Add(file);
            }

            isPrn = true;
            PrnParsStackPanel.Visibility = Visibility.Visible;
            OverlayStackPanel.Visibility = Visibility.Collapsed;
            //var fp = @"G:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500\voac160.prn";
            //ResultImage.Source = DrawFromPrn(fp, "tk");
        }

        private void ProceedPicturingButton_Click(object sender, RoutedEventArgs e)
        {

            if (ResultListBox.Items.Count <= 0) return;

            worker = new BackgroundWorker();
            
            _images.Clear();

            if (!ReadGraphSizePars()) return;
            
            var selPar = PrnParComboBox.SelectedItem.ToString();
            var dRect = new IntRectangle((uint) GraphConfig.Instace.XBegin,
                (uint) GraphConfig.Instace.YBegin,
                (uint) (GraphConfig.Instace.XEnd - GraphConfig.Instace.XBegin),
                (uint) (GraphConfig.Instace.YEnd - GraphConfig.Instace.YBegin));

            DrawingProgressBar.Maximum = (ResultListBox.SelectedIndex == -1)
                ? ResultListBox.Items.Count
                : ResultListBox.SelectedItems.Count;

            DrawingProgressBar.Value = 0;

            var filesToProceed = GetInputPrnTxtFiles();

            if (!Directory.Exists(fbd.SelectedPath + "\\Images\\"))
                Directory.CreateDirectory(fbd.SelectedPath + "\\Images\\");
            DrawingProgressBar.Visibility = Visibility.Visible;

            Dictionary<int[], Dictionary<string, List<Point>> > overlayPoints = null;
            if (!string.IsNullOrEmpty(OverlayFileTextBox.Text))
            {
                overlayPoints = new Dictionary<int[], Dictionary<string, List<Point>>>();
                var overlayFile = OverlayFileTextBox.Text;

                using ( var sr = new StreamReader(overlayFile) )
                {
                    var ln = "";
                    while ( (ln = sr.ReadLine()) != null)
                    {
                        if (ln.StartsWith("#")) continue;
                        if (string.IsNullOrWhiteSpace(ln)) continue;
                        
                        var rgbarr = new int[] {0,0,0}; //todo фракционировать
                        overlayPoints.Add(rgbarr, new Dictionary<string, List<Point>>());

                        var lnSs = ln.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries );
                        if (lnSs.Length > 1)
                        {
                            var colArr = lnSs[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            rgbarr[0] = Config.Tools.ParseOrDefaultInt( colArr[0], 0 );
                            rgbarr[1] = Config.Tools.ParseOrDefaultInt(colArr[1], 0);
                            rgbarr[2] = Config.Tools.ParseOrDefaultInt(colArr[2], 0);
                        }

                        var mln = "";
                        string[] slns;
                        while ((mln = sr.ReadLine()) != null && !mln.Contains(@"--***--"))
                        {
                            if (mln.StartsWith("#")) continue;
                            
                            slns = mln.Split(new char[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                            if (slns.Length <= 1) continue;

                            var xm = Convert.ToDouble(slns[0]);
                            var zm = Convert.ToDouble(slns[1]);
                            var flm = slns.Last();
                            flm = flm.Substring( flm.LastIndexOf(@"\", StringComparison.Ordinal) + 1 );
                            flm = flm.Substring(0, flm.Length - 4);

                            if (!overlayPoints[rgbarr].ContainsKey(flm))
                            {
                                overlayPoints[rgbarr].Add(flm, new List<Point> {new Point(xm, zm)} );
                            }
                            else
                            {
                                overlayPoints[rgbarr][flm].Add(new Point(xm,zm));
                            }
                            
                        }

                    }
                }

            }

            if (isPrn && selPar == "txt")
            {
                return;
            }

            worker.DoWork += (s, ev) =>
            {
                bool isPrnLoc = isPrn; //чтобы обезопаситься от изменения isPrn в процессе выполнения воркера
                

                foreach (var fl in filesToProceed.Keys.OrderBy(ExtractNumberFromString).ToList())
                {
                    Dictionary<int[], List<Point>> overlay = null;
                    if (overlayPoints != null)
                    {
                        var fl_short = fl.Substring( fl.LastIndexOf(@"\", StringComparison.Ordinal) + 1 );
                        fl_short = fl_short.Substring( 0, fl_short.Length - 4 );
                        fl_short = fl_short.Remove(fl_short.LastIndexOf("c"), 1);

                        overlay = overlayPoints.Keys.Where(col => overlayPoints[col].ContainsKey(fl_short)).ToDictionary(col => col, col => overlayPoints[col][fl_short]);
                    }

                    var curImg = (isPrnLoc) 
                        ? GraphTools.DrawFromPrn(fl, selPar, dRect) 
                        : GraphTools.DrawFromTxt(fl, filesToProceed[fl], overlay); 
                    
                    _images.Add(curImg);

                    var imName = fl.Substring(fl.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
                    imName = imName.Substring(0, imName.Length - 4);
                    _imagesnames.Add(imName);

                    _images.Last().Freeze();
                    Dispatcher.Invoke(
                        new Action(
                            () =>
                                SaveDrawingToFile(_images.Last().Drawing,
                                    fbd.SelectedPath + "\\Images\\" +
                                    imName + ".png", 1)));
                    Dispatcher.Invoke(new Action(() => DrawingProgressBar.Value++));
                }
                Dispatcher.Invoke(new Action(() => DrawingProgressBar.Visibility = Visibility.Collapsed));
                Dispatcher.Invoke(new Action(() => ResultImage.Source = _images[0]));
                Dispatcher.Invoke(new Action(() => ResultImage.Tag = 0)); //индекс выбранного элемента
                Dispatcher.Invoke(new Action(() => ImageNameLabel.Content = _imagesnames[0]));
                Dispatcher.Invoke(new Action(() => LeftButton.IsEnabled = true));
                Dispatcher.Invoke(new Action(() => RightButton.IsEnabled = true));
                
                Process.Start(fbd.SelectedPath + "\\Images\\");
            };

            worker.RunWorkerAsync();

        }

        /// <summary>
        /// Получить список путей выбранных файлов для визуализации (txt или prn)
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetInputPrnTxtFiles()
        {
            var itemsToProceed = (ResultListBox.SelectedIndex == -1) ? ResultListBox.Items : ResultListBox.SelectedItems;
            var result = new Dictionary<string, string>();
            foreach (var itm in itemsToProceed)
            {
                if (isPrn)
                {
                    result.Add(itm.ToString(), null);
                }
                else
                {
                    var ffiles = itm.ToString().Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries);
                    result.Add(fbd.SelectedPath + "\\" + ffiles[0],
                        fbd.SelectedPath + "\\" + ffiles[1]);
                }
            }
            return result;
        } 

        private bool ReadGraphSizePars()
        {
            GraphConfig.Instace.XSize = Config.Tools.ParseOrDefaultInt(XSizeBox.Text) * 1000;
            GraphConfig.Instace.YSize = Config.Tools.ParseOrDefaultInt(ZSizeBox.Text) * 1000;

            if (GraphConfig.Instace.XSize * GraphConfig.Instace.YSize == 0)
            {
                MessageBox.Show("Вы не ввели размеры модели!");
                return false;
            }

            GraphConfig.Instace.XBegin = Config.Tools.ParseOrDefaultInt(XBegBox.Text) * 1000;
            GraphConfig.Instace.XEnd = Config.Tools.ParseOrDefaultInt(XEndBox.Text) * 1000;

            GraphConfig.Instace.YBegin = Config.Tools.ParseOrDefaultInt(ZBegBox.Text) * 1000;
            GraphConfig.Instace.YEnd = Config.Tools.ParseOrDefaultInt(ZEndBox.Text) * 1000;

            return true;
        }

        private List<int> GetIndexesOfNearestSidePoints(LineSeries seria, DataPoint pt)
        {
            //var lst = seria.Points.ToList();
            //lst.RemoveAt(4);
            //lst = lst.OrderBy(x => GraphTools.PointsDistance(x, pt)).ToList();
            //return new List<int> { seria.Points.IndexOf(lst[0]), seria.Points.IndexOf(lst[1]) };

            var lst = new List<double>();
            for (int i = 0; i < seria.Points.Count-1; i++)
            {
                lst.Add(GraphTools.DistanceToLine(new List<DataPoint> {seria.Points[i], seria.Points[i+1] }, pt));
            }
            var ordered = lst.OrderBy(x => x).ToList();

            var ind = lst.IndexOf(ordered[0]);

            return new List<int> { ind , ((ind + 1) == lst.Count) ? 0 : ind + 1 };
        }

        private List<int> GetIndexesOfNearestSidePoints(LineSeries seria, ScreenPoint pt, int range = 0)
        {
            var lst = new List<double>();
            for (int i = 0; i < seria.Points.Count - 1; i++)
            {
                lst.Add(GraphTools.DistanceToLine(new List<ScreenPoint> { seria.Transform(seria.Points[i]), seria.Transform(seria.Points[i + 1]) }, pt));
            }
            var ordered = lst.OrderBy(x => x).ToList();

            var ind = lst.IndexOf(ordered[0]);

            if (range > 0 && ordered[0] > range) return null;

            return new List<int> { ind, ((ind + 1) == lst.Count) ? 0 : ind + 1 };
        }

        private double ExtractNumberFromString(string line)
        {
            var numstring = Regex.Match(line, @"\d+").Value;
            return Config.Tools.ParseOrDefaultDouble(numstring);
        }

        private void SetDefaultGraphConf()
        {
            Config.GraphConfig.Instace.XSize = 4000;
            Config.GraphConfig.Instace.YSize = 400;

            Config.GraphConfig.Instace.XBegin = 1500;
            Config.GraphConfig.Instace.XEnd = 3000;

            Config.GraphConfig.Instace.YBegin = 0;
            Config.GraphConfig.Instace.YEnd = 300;

            Config.GraphConfig.Instace.Isoterms = new List<double>() {300,600,900,1200,1500};
            GraphConfig.Instace.LoadFromFile();
        }

        private void SetDefaultConf()
        {
            if (!File.Exists("config.xml"))
            {
                MessageBox.Show("Возможно, Вы первый раз запустили программу, либо ещё не настроили её. Вам будет предложено ввести параметры Вашего доступа в кластер. Если Вам нужны только оффлайн-функции, просто закройте окно настроек.");

                var ucw = new UserCInfigWindow(false);
                if (ucw.ShowDialog() == true)
                {
                    ConfigManager.Write("config.xml");
                }
                else
                {
                    HomeTab.IsEnabled = false;
                    //InitTab.IsEnabled = false;
                    CommitInitButton.IsEnabled = false;
                    isOffline = true;
                }

                return;
            }
            ConfigManager.Read("config.xml");
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

        private void EnableRbControls()
        {
            AddBoxButton.IsEnabled = true;
            RemoveBoxButton.IsEnabled = true;

            LayerUpButton.IsEnabled = true;
            LayerDownButton.IsEnabled = true;

            AddThermoBoxButton.IsEnabled = true;
            RemoveThermoBoxButton.IsEnabled = true;

            ThermoLayerUpButton.IsEnabled = true;
            ThermoLayerDownButton.IsEnabled = true;
        }

        private void CreateInitButton_OnClick(object sender, RoutedEventArgs e)
        {
            bool rewrite = _currentConfig != null;

            GeometryHideBox.IsChecked = true;
            EnableRbControls();

            if (rewrite)
            {
                rewrite =
                    (MessageBox.Show("У вас уже открыто редактирование модели. Вы уверены, что хотите открыть новое? Все несохраненные данные будут потеряны.", "Перезапись", MessageBoxButton.YesNo, MessageBoxImage.Warning) ==
                     MessageBoxResult.Yes);
            }
            else
            {
                rewrite = true;
            }

            if (rewrite)
            {
                try
                {
                    _currentConfig = new InitConfig();
                    ReadInitFromFile("init.t3c");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка чтения файла: " + ex.Message);
                }
            }


            RocksPropertiesButton.IsEnabled = true;
            if (!isOffline) CommitInitButton.IsEnabled = true;

        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            var curInd = Convert.ToInt16(ResultImage.Tag);
            if (curInd <= 0) return;
            curInd--;
            ResultImage.Tag = curInd;
            ResultImage.Source = _images[curInd];

            ImageNameLabel.Content = _imagesnames[curInd];
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            var curInd = Convert.ToInt16(ResultImage.Tag);
            if (curInd >= _images.Count-1) return;
            curInd++;
            ResultImage.Tag = curInd;
            ResultImage.Source = _images[curInd];

            ImageNameLabel.Content = _imagesnames[curInd];
        }

       
        private void XSizeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = OnlyNumeric(e.Text);
        }

        public static bool OnlyNumeric(string text)
        {
            Regex regex = new Regex("^[0-9]*$");
            return !regex.IsMatch(text);
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PrnParComboBox.ItemsSource = Config.GraphConfig.Instace.PrnParameters;
            PrnParComboBox.SelectedIndex = 0;
            GeometryHideBox.IsChecked = false;
        }


        private void AddBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isNewBoxAdding = true;
        }

        private void PTtTrendsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var trendWindow = new PTtBuilderWindow();
            trendWindow.ShowDialog();
        }

        private void NewTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var btw = new BatchTaskWindow();
            btw.ShowDialog();
        }

        private void UserConfigButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ucw = new UserCInfigWindow {Owner = this};

            ucw.ShowDialog();
        }

        private void CancelTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var cw = new CancelTaskWindow {Owner = this};

            if (cw.ShowDialog() == true)
            {
                UpdateSqueue();
            }
        }

        private void UpdateTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var utw = new UpdateTaksWindow {Owner = this};
            utw.ShowDialog();

        }

        private void I2JSlabTaskButton_OnClick(object sender, RoutedEventArgs e)
        {
            var i2jslabWnd = new I2JslabBatchWindow {Owner = this};
            i2jslabWnd.ShowDialog();
        }

        private void FilesViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            var fww = new FilesViewWindow() {Owner = this};
            fww.Show();
        }

        private void AmirT3CButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "amir.t3c|amir.t3c";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var amir = new Amir();
                amir.ImportFromFile(ofd.FileName);
            }
        }

        private void DiaryButton_OnClick(object sender, RoutedEventArgs e)
        {

            if (string.IsNullOrWhiteSpace(Config.Config.Instace.DiaryPath) || !File.Exists(Config.Config.Instace.DiaryPath))
            {
                MessageBox.Show("Вы не указали в настройках файл дневника, либо указали на несуществующий файл.");
                return;
            }

            Process proc = new Process
            {
                StartInfo =
                {
                    FileName = Config.Config.Instace.DiaryPath,
                    UseShellExecute = true
                }
            };
            proc.Start();    
        }

        private void RocksPropertiesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var rpw = new RocksPropertiesWindow(_currentConfig) {Owner = this};
            rpw.ShowDialog();
        }

        private void ExitItem_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GeometryDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selInd = GeometryDataGrid.SelectedIndex;

            if (selInd == -1) return;

            var oldSelection = _selectedArea;
            
            if (_currentConfig == null || _currentConfig.RockBoxes == null) return;
            _selectedArea = _graphModel.Series.FirstOrDefault(x=>x.Tag.ToString() ==  _currentConfig.RockBoxes[selInd].Name + "area") as AreaSeries;
            if (_selectedArea == null) return;

            if (oldSelection != null) oldSelection.Color = OxyColors.Transparent;
            _selectedArea.Color = OxyColors.Red;
            _selectedArea.StrokeThickness = 5;
            _graphModel.InvalidatePlot(false);
        }

        private void GraphConfigButton_OnClick(object sender, RoutedEventArgs e)
        {
            var pcw = new PicturesConfigWindow {Owner = this};
            pcw.ShowDialog();
        }

        private void DocumentationItem_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            var lbitems =
                            (from object lbitem in ResultListBox.Items select lbitem.ToString()).ToList()
                                .OrderBy(ExtractNumberFromString)
                                .ToList();


            foreach (var item in lbitems)
            {
                var ffiles = item.ToString().Split(new[] {" | "}, StringSplitOptions.RemoveEmptyEntries);
                var mymodel = GraphTools.CreateModelFromTxt(fbd.SelectedPath + "\\" + ffiles[0], fbd.SelectedPath + "\\" + ffiles[1]);
                ResultImageView.Model = mymodel;
                mymodel.InvalidatePlot(false);
            } */

        }

        private void GeothermButton_OnClick(object sender, RoutedEventArgs e)
        {
            var gw = new GeothermWindow();
            gw.Owner = this;
            gw.ShowDialog();
        }

        private void AdjustAxesScaleButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_graphModel == null || _graphModel.Axes == null || _graphModel.Axes.Count < 2) return;

            var axeX = _graphModel.Axes[0] as LinearAxis;
            var axeY = _graphModel.Axes[1] as LinearAxis;

            axeX.Zoom(axeY.Scale);

            //var axeHeight = axeY.ActualMaximum - axeY.ActualMinimum;

            //var actualWidth = _graphModel.Width;
            //var actualHeight = _graphModel.Height;

            //var targetWidth = (actualWidth * axeHeight) / actualHeight;

            //var curMidX = (axeX.Maximum + axeX.Minimum) / 2;

            

            //axeX.Minimum = curMidX - (targetWidth / 2);
            //axeX.Maximum = curMidX + targetWidth / 2;

            _graphModel.InvalidatePlot(false);

        }

        private void AddThermoBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _currentConfig.Geotherms == null) return;

            var newThermoBox = new LineSeries();

            newThermoBox.Points.Add(new DataPoint(_currentConfig.Grid.Xsize / 2, _currentConfig.Grid.Ysize / 2));
            newThermoBox.Points.Add(new DataPoint(_currentConfig.Grid.Xsize / 2, _currentConfig.Grid.Ysize / 2 + 50000));
            newThermoBox.Points.Add(new DataPoint(_currentConfig.Grid.Xsize / 2 + 500000, _currentConfig.Grid.Ysize / 2 + 50000));
            newThermoBox.Points.Add(new DataPoint(_currentConfig.Grid.Xsize / 2 + 500000, _currentConfig.Grid.Ysize / 2));
            newThermoBox.Points.Add(new DataPoint(_currentConfig.Grid.Xsize / 2, _currentConfig.Grid.Ysize / 2));

            var tb = new Geotherm
            {
                Name = "Geotherm" +
                       _currentConfig.Geotherms.Select(
                           x => Config.Tools.ExtractNumberFromString(x.Name)).ToList().Max() + 1,
                GeothermType = 0,
                Apex0 = new ModPoint(_currentConfig.Grid.Xsize/2, _currentConfig.Grid.Ysize/2),
                Apex1 = new ModPoint(_currentConfig.Grid.Xsize/2, _currentConfig.Grid.Ysize/2 + 50000),
                Apex2 = new ModPoint(_currentConfig.Grid.Xsize/2 + 500000, _currentConfig.Grid.Ysize/2),
                Apex3 = new ModPoint(_currentConfig.Grid.Xsize/2 + 500000, _currentConfig.Grid.Ysize/2 + 50000)
            };



            _currentConfig.Geotherms.Add(tb);

            newThermoBox.Tag = tb.Name;
            newThermoBox.MarkerFill = OxyColors.Orange;
            newThermoBox.Color = OxyColors.Orange;
            newThermoBox.MarkerType = MarkerType.Square;
            newThermoBox.MarkerSize = 4;

            _graphModel.Series.Add(newThermoBox);
            _graphModel.Annotations.Add(new PointAnnotation {Tag = tb.Name + "T0" });
            _graphModel.Annotations.Add(new PointAnnotation { Tag = tb.Name + "T1" });
            _graphModel.Annotations.Add(new PointAnnotation { Tag = tb.Name + "T2" });
            _graphModel.Annotations.Add(new PointAnnotation { Tag = tb.Name + "T3" });


            newThermoBox.IsVisible = (GeothermsBox.IsChecked == true);
            foreach (var annotation in _graphModel.Annotations)
            {
                annotation.TextColor = (GeothermsBox.IsChecked == true) ? OxyColors.Red : OxyColors.Transparent;
            }

            AttachMovingEvents(newThermoBox);
            AttachChangeEvents(tb);
            
            GeothermsDataGrid.Items.Refresh();
            _graphModel.InvalidatePlot(false);

        }

        private void RemoveThermoBoxButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _currentConfig.Geotherms == null) return;
           
            var tb = GeothermsDataGrid.SelectedItem as Geotherm;
            if (tb == null) return;

            _currentConfig.Geotherms.Remove(tb);

            var gts = _graphModel.Series.FirstOrDefault(x => Convert.ToString(x.Tag) == tb.Name);
            if (gts == null) return;
            _graphModel.Series.Remove(gts);

            var annots = _graphModel.Annotations.Where(x => x.Tag != null && x.Tag.ToString().Contains(tb.Name + "T") ).ToList();

            foreach (var annot in annots)
            {
                _graphModel.Annotations.Remove(annot);
            }

            _graphModel.InvalidatePlot(false);
            GeothermsDataGrid.Items.Refresh();
        }

        private void ThermoLayerUpButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _currentConfig.Geotherms == null) return;

            var tb = GeothermsDataGrid.SelectedItem as Geotherm;
            if (tb == null) return;

            var curInd = _currentConfig.Geotherms.IndexOf(tb);

            if (curInd <= 0) return;

            var bufTb = _currentConfig.Geotherms[curInd - 1];
            _currentConfig.Geotherms[curInd - 1] = tb;
            _currentConfig.Geotherms[curInd] = bufTb;

            GeothermsDataGrid.Items.Refresh();

            var gts = _graphModel.Series.FirstOrDefault(x => x.Tag != null && Convert.ToString(x.Tag) == tb.Name);
            if (gts == null) return;

            var serInd = _graphModel.Series.IndexOf(gts);
            var bufSer = _graphModel.Series[serInd - 1];

            _graphModel.Series[serInd - 1] = gts;
            _graphModel.Series[serInd] = bufSer;
            _graphModel.InvalidatePlot(false);

        }

        private void ThermoLayerDownButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_currentConfig == null || _currentConfig.Geotherms == null) return;

            var tb = GeothermsDataGrid.SelectedItem as Geotherm;
            if (tb == null) return;

            var curInd = _currentConfig.Geotherms.IndexOf(tb);

            if (curInd >= _currentConfig.Geotherms.Count - 1) return;

            var bufTb = _currentConfig.Geotherms[curInd + 1];
            _currentConfig.Geotherms[curInd + 1] = tb;
            _currentConfig.Geotherms[curInd] = bufTb;

            GeothermsDataGrid.Items.Refresh();

            var gts = _graphModel.Series.FirstOrDefault(x => x.Tag != null && x.Tag == tb.Name);
            if (gts == null) return;

            var serInd = _graphModel.Series.IndexOf(gts);
            var bufSer = _graphModel.Series[serInd + 1];

            _graphModel.Series[serInd + 1] = gts;
            _graphModel.Series[serInd] = bufSer;
            _graphModel.InvalidatePlot(false);
        }

        private void EditPrnButton_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            var ofd1 = new OpenFileDialog();
            if (ofd1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var inds = new List<uint>();

                using (StreamReader sr = new StreamReader(ofd1.FileName))
                {
                    string str = "";
                    while ( (str = sr.ReadLine()) != null)
                    {
                        inds.Add(Convert.ToUInt32(str));
                    }
                }

                var ofd2 = new OpenFileDialog();
                if (ofd2.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

                PrnWorker.ChangeMarkersRockId(ofd2.FileName, inds, 33);

            } */

            PrnWorker.ReadBoundaryConditions(@"H:\modelling_results\pz2\p150_l140_c30_m600_v5-1_\voac20.prn");
        }

        private void SelectOverlayButton_OnClick(object sender, RoutedEventArgs e)
        {
            var oofd = new OpenFileDialog();

            if (oofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                overlayFile = oofd.FileName;
                OverlayFileTextBox.Text = overlayFile;
            }

        }

        private void OverlayParsCb_OnUnchecked(object sender, RoutedEventArgs e)
        {
            overlayFile = null;
            OverlayFileTextBox = null;
        }
    }
}
