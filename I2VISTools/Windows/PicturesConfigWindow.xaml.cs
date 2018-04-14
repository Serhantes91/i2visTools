using System;
using System.Collections.Generic;
using System.IO;
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
using I2VISTools.Subclasses;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for PicturesConfigWindow.xaml
    /// </summary>
    public partial class PicturesConfigWindow : Window
    {

        private List<RockColor> _rockColors;
        private List<double> _geotherms;
        private List<PrnParameterRange> _ranges; 

        public PicturesConfigWindow()
        {
            InitializeComponent();
            _rockColors = CopyColors(GraphConfig.Instace.RocksColor);
            _geotherms = GraphConfig.Instace.Isoterms.ToList();
            _ranges = CopyPrnRange(GraphConfig.Instace.PrnValuesRanges);


            ColorsListBox.ItemsSource = _rockColors;
            IsotermsBox.ItemsSource = _geotherms;
            PrnValueRangeListBox.ItemsSource = _ranges;
            PrnParametersBox.ItemsSource = GraphConfig.Instace.PrnParameters;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            GraphConfig.Instace.RocksColor = _rockColors;
            GraphConfig.Instace.Isoterms = _geotherms;
            GraphConfig.Instace.PrnValuesRanges = _ranges;
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //for (int i = 0; i < GraphConfig.Instace.ColorMap.Count; i++)
            //{
            //    ColorsListBox.Items.Add(i);
            //}
            
        }


        private List<RockColor> CopyColors(List<RockColor> colors)
        {
            return colors.Select(color => new RockColor() {Color = color.Color, Index = color.Index}).ToList();
        }

        private List<PrnParameterRange> CopyPrnRange(List<PrnParameterRange> ranges)
        {
            return
                ranges.Select(
                    range => new PrnParameterRange {Min = range.Min, Max = range.Max, PrnMarking = range.PrnMarking})
                    .ToList();
        } 

        private void RemoveIsothermButton_Click(object sender, RoutedEventArgs e)
        {

            if (IsotermsBox.SelectedIndex == -1) return;
            var curTherm = (double)IsotermsBox.SelectedItem;

            _geotherms.Remove(curTherm);
            IsotermsBox.Items.Refresh();
            
        }

        private void AddIsothermButton_Click(object sender, RoutedEventArgs e)
        {
            var value = Config.Tools.ParseOrDefaultDouble(IsothermBox.Text);
            _geotherms.Add(value);
            IsotermsBox.Items.Refresh();
        }

        private void ApplyAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            using (var colConfFile = new StreamWriter("picConfig"))
            {
                foreach (var rcolor in _rockColors)
                {
                    colConfFile.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", rcolor.Index, rcolor.Color.A, rcolor.Color.R, rcolor.Color.G, rcolor.Color.B);
                }
                colConfFile.WriteLine("~");
                foreach (var isotherm in _geotherms)
                {
                    colConfFile.WriteLine(isotherm);
                }
            }
            

            GraphConfig.Instace.RocksColor = _rockColors;
            GraphConfig.Instace.Isoterms = _geotherms;
            GraphConfig.Instace.PrnValuesRanges = _ranges;
            DialogResult = true;
        }

        private void AddPrnRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrnParametersBox.SelectedIndex == -1) return;
            var min = Config.Tools.ParseOrDefaultDouble(MinValueBox.Text, double.NaN);
            var max = Config.Tools.ParseOrDefaultDouble(MaxValueBox.Text, double.NaN);

            if (double.IsNaN(min) || double.IsNaN(max)) return;

            if (min >= max) return;

            var range = new PrnParameterRange
            {
                PrnMarking = PrnParametersBox.Text,
                Min = min,
                Max = max
            };

            if (_ranges.FirstOrDefault(x => x.PrnMarking == range.PrnMarking) != null)
            {
                MessageBox.Show("Для данного параметра уже задан диапазон!");
                return;
            }

            _ranges.Add(range);
            PrnValueRangeListBox.Items.Refresh();
        }

        private void RemovePrnRangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (PrnValueRangeListBox.SelectedIndex == -1) return;
            var range = PrnValueRangeListBox.SelectedItem as PrnParameterRange;
            _ranges.Remove(range);
            PrnValueRangeListBox.Items.Refresh();
        }
    }
}
