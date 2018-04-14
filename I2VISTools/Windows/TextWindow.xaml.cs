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

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for TextWindow.xaml
    /// </summary>
    public partial class TextWindow : Window
    {

        public string TextContent { get; set; }

        public TextWindow( string text )
        {
            InitializeComponent();

            TextContent = text;
        }

        private void COpyButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TextWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            ResultBox.AppendText(TextContent);
        }
    }
}
