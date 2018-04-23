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
using OxyPlot;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Логика взаимодействия для ModelNameWindow.xaml
    /// </summary>
    public partial class ModelNameWindow : Window
    {
        private string defaultName;
        public string OutName { get; set; }

        public ModelNameWindow(string defaultName = null)
        {
            InitializeComponent();
            this.defaultName = defaultName;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var name = InputTextBox.Text;

            if (String.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Вы не ввели имя!");
                return;
            }

            if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
            {
                MessageBox.Show("В имени использованы недопустимые символы!");
                return;
            }

            OutName = name;

            DialogResult = true;
        }

        private void ModelNameWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            InputTextBox.Text = defaultName;
            InputTextBox.Focus();
        }
    }
}
