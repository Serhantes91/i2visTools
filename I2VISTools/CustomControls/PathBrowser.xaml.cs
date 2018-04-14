using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace I2VISTools.CustomControls
{

    public enum DialogType {File, Folder}

    /// <summary>
    /// Interaction logic for PathBrowser.xaml
    /// </summary>
    public partial class PathBrowser : UserControl
    {

        OpenFileDialog ofd = new OpenFileDialog();
        FolderBrowserDialog fbd = new FolderBrowserDialog();

        public DialogType DialogTargetType { get; set; }

        public PathBrowser()
        {
            InitializeComponent();
        }

        public string FilePath
        {
            get { return PathTextBox.Text; }
            set { PathTextBox.Text = value; }
        }

        //public new double Width
        //{
        //    get { return OutterStack.Width; }
        //    set { OutterStack.Width = value; }
        //}

        //public new double Height
        //{
        //    get { return OutterStack.Height; }
        //    set { OutterStack.Height = value; }
        //}

        public string Title
        {
            get { return TitleLabel.Content.ToString(); }
            set { TitleLabel.Content = value; }
        }

        private void BrowseButon_Click(object sender, RoutedEventArgs e)
        {

            if (DialogTargetType == DialogType.Folder)
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    FilePath = fbd.SelectedPath;
                }
            }
            else
            {
                if (ofd.ShowDialog() == true)
                {
                    FilePath = ofd.FileName;
                }    
            }

        }

        public string FileFilters
        {
            get
            {
                return ofd.Filter;
            }
            set { ofd.Filter = value; }
        }



    }
}
