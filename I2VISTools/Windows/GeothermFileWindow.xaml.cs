using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using I2VISTools.Config;
using I2VISTools.Tools;
using OxyPlot;
using OxyPlot.Series;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for GeothermFileWindow.xaml
    /// </summary>
    public partial class GeothermFileWindow : Window
    {

        private string tFile = "";
        private string cFile = "";

        private List<int> selectedInds = new List<int>();

        public List<LineSeries> ThermSeries;

        double[,] arr;
        private uint[] pxls;

        private int _airNodesNum = 1; //кол-во отведенное на воздух и воду

        public GeothermFileWindow(string tempFile, string compFile) 
        {
            cFile = compFile;
            tFile = tempFile;
            InitializeComponent();
        }

        private void DisplayButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInds.Count == 0) return;

            //todo сделать свой airNodes для каждого сечения
            _airNodesNum = 1;
            while ((arr[_airNodesNum, selectedInds[0]] == 0 || arr[_airNodesNum, selectedInds[0]] == 1) && _airNodesNum < arr.GetLength(0) - 1)
            {
                _airNodesNum++;
            }

            _airNodesNum = (_airNodesNum < 200) ? _airNodesNum - 1 : 0;


            ThermSeries = new List<LineSeries>();

            var oldArrZ = arr.GetLength(0);
            var oldArrX = arr.GetLength(1);

            arr = I2VISOutputReader.GetTemperatureArrayFromTxt(tFile);

            var ktZ =  Convert.ToDouble(arr.GetLength(0)) / Convert.ToDouble(oldArrZ);
            var ktX =  Convert.ToDouble(arr.GetLength(1)) / Convert.ToDouble(oldArrX);

            foreach (var ind in selectedInds)
            {
                var curSerie = new LineSeries();
                curSerie.Title = "UserData, position " + ind;

                for (int i = 0; i < arr.GetLength(0) - (_airNodesNum*ktZ + 1) ; i++)
                {
                    curSerie.Points.Add(new DataPoint(arr[i + Convert.ToInt32(_airNodesNum*ktZ), Convert.ToInt32(ind*ktX)], Convert.ToInt32(Convert.ToDouble(i)/ktZ) *1000));
                }
                
                ThermSeries.Add(curSerie);
            }

            DialogResult = true;

        }

        private void GeothermFileWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            List<int[]> C_map = GraphConfig.Instace.ColorMap.Select(x => new int[] {x.R, x.G, x.B}).ToList();
         
            arr = I2VISOutputReader.GetRocksArrayFromTxt(cFile);
            pxls = PixelsGraph.GetPixelsArray(arr, C_map);

            BitmapSource bitmapSource = BitmapSource.Create(4002, 402, 96, 96, PixelFormats.Pbgra32, null, pxls, (4002) * 4);
            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, 4002, 402));
            }

            TxtImage.Source = new DrawingImage(visual.Drawing);
            
        }


        private void TxtImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(TxtImage);

            var mousex = pos.X;
            var aw = TxtImage.ActualWidth;
            var mw = 4001d;
            
            var tarX = Convert.ToInt32((mw * mousex) / aw);

            selectedInds.Add(tarX);

            int red = 255;
            int green = 255;
            int blue = 0;
            int alpha = 255;

            for (int y = 0; y < arr.GetLength(0)-1; y++)
            {
                int i = arr.GetLength(1) * y + tarX;
                int i2 = arr.GetLength(1) * y + tarX+1;
                pxls[i] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
                pxls[i2] = (uint)((alpha << 24) + (red << 16) + (green << 8) + blue);
            }

            BitmapSource bitmapSource = BitmapSource.Create(4002, 402, 96, 96, PixelFormats.Pbgra32, null, pxls, (4002) * 4);
            var visual = new DrawingVisual();
            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                drawingContext.DrawImage(bitmapSource, new Rect(0, 0, 4002, 402));
            }

            TxtImage.Source = new DrawingImage(visual.Drawing);

        }
    }
}
