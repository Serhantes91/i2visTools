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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace I2VISTools.CustomControls
{

    public enum Position { Top, Bottom, Left, Right }

    /// <summary>
    /// Interaction logic for LabeledTextBox.xaml
    /// </summary>
    public partial class LabeledTextBox : UserControl
    {
        public LabeledTextBox()
        {
            InitializeComponent();
        }

        private Position _labelPosition;
        public Position LabelPosition
        {
            get
            {
                return _labelPosition;
            }
            set
            {
                _labelPosition = value;
                if (_labelPosition == Position.Left)
                {
                    MainStack.Orientation = Orientation.Horizontal;
                    MainStack.Children.Remove(HeaderLabel);
                    MainStack.Children.Insert(0, HeaderLabel);
                }

                if (_labelPosition == Position.Right)
                {
                    MainStack.Orientation = Orientation.Horizontal;
                    MainStack.Children.Remove(HeaderLabel);
                    MainStack.Children.Add(HeaderLabel);
                }

                if (_labelPosition == Position.Top)
                {
                    MainStack.Orientation = Orientation.Vertical;
                    MainStack.Children.Remove(HeaderLabel);
                    MainStack.Children.Insert(0, HeaderLabel);
                }

                if (_labelPosition == Position.Bottom)
                {
                    MainStack.Orientation = Orientation.Horizontal;
                    MainStack.Children.Remove(HeaderLabel);
                    MainStack.Children.Add(HeaderLabel);
                }

            }
        }

        public double TitleWidth
        {
            get { return HeaderLabel.Width; }
            set { HeaderLabel.Width = value; }
        }

        public double TitleHeight
        {
            get { return HeaderLabel.Height; }
            set { HeaderLabel.Height = value; }
        }

        public double TextBoxWidth
        {
            get { return BodyBox.Width; }
            set { BodyBox.Width = value; }
        }

        public double TextBoxHeight
        {
            get { return BodyBox.Height; }
            set { BodyBox.Height = value; }
        }

        public HorizontalAlignment HorizontalTitleAlignment
        {
            get { return HeaderLabel.HorizontalContentAlignment; }
            set { HeaderLabel.HorizontalContentAlignment = value; }
        }

        public VerticalAlignment VerticalTitleAlignment
        {
            get { return HeaderLabel.VerticalContentAlignment; }
            set { HeaderLabel.VerticalContentAlignment = value; }
        }


        private string _title;

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                HeaderLabel.Content = _title;
            }
        }

        public string Text
        {
            get { return BodyBox.Text; }
            set { BodyBox.Text = value; }
        }

        public double TextFontSize
        {
            get { return BodyBox.FontSize; }
            set { BodyBox.FontSize = value; }
        }

        public double TitleFontSize
        {
            get { return HeaderLabel.FontSize; }
            set { HeaderLabel.FontSize = value; }
        }

    }
}
