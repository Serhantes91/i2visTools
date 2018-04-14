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
using I2VISTools.ModelConfigClasses;
using I2VISTools.Tools;
using OxyPlot;
using OxyPlot.Annotations;
using Renci.SshNet;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for I2jslabBatchWindow.xaml
    /// </summary>
    public partial class I2JslabBatchWindow : Window
    {

        private List<string> _foldersList;

        public I2JslabBatchWindow()
        {
            InitializeComponent();
        }

        private void StartTaskButton_Click(object sender, RoutedEventArgs e)
        {

            if (PrnListBox.SelectedIndex != -1)
            {
                List<string> files = (from object file in PrnListBox.SelectedItems select file.ToString()).ToList();

                var amir = new Amir(files);
                amir.ExportToFile("");

                var keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath, Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123")); //todo надо сделать подобный ssh sftp-manager
                var username = Config.Config.Instace.UserLogin;

                using ( var sftpclient = new SftpClient(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, keyFile))
                {
                    sftpclient.Connect();
                    sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + Config.Config.Instace.ClusterWorkingDirectory + TasksListBox.SelectedItem);
                    using (var fileStream = new FileStream("amir.t3c", FileMode.Open))
                    {
                        sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                        sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName("amir.t3c"), true);
                    }
                    sftpclient.Disconnect();
                }

            }

            var cmds = new List<string> { "module add slurm", "module load intel/13.1.0", "module load mkl/4.0.2.146", "module load openmpi/1.5.5-icc", "cd _scratch", "cd " + Config.Config.Instace.ClusterWorkingDirectory };

            var part = (TestRb.IsChecked == true) ? "test" : "gputest"; 

            foreach (var curFolder in TasksListBox.SelectedItems)
            {
                cmds.Add(String.Format("cd {0}", curFolder));
                cmds.Add(String.Format("sbatch -p {0} run i2jslab", part));
                cmds.Add("cd ..");
            }

            cmds.RemoveAt(cmds.Count - 1);

            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);
            MessageBox.Show(result);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var cmds = new List<string> { "module add slurm", "cd _scratch", "cd " + Config.Config.Instace.ClusterWorkingDirectory, "ls" };
            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);

            _foldersList = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            _foldersList.Remove("base_lomonosov");

            TasksListBox.ItemsSource = _foldersList;
            TasksListBox.Items.Refresh();
        }

        private void AmirButton_OnClick(object sender, RoutedEventArgs e)
        {

            var fbd = new FolderBrowserDialog();
            fbd.SelectedPath = @"E:\Sergey\Univer\postgrad\vsz\100_140_150_15_tcc500";
            fbd.ShowNewFolderButton = false;

            DialogResult result = fbd.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) return;

            var files = Directory.GetFiles(fbd.SelectedPath).ToList().Where(x => x.EndsWith(".prn")).ToList();

            var amir = new Amir(files);

            amir.ExportToFile("");

        }

        private void TasksListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = TasksListBox.SelectedItem;
            if (selectedItem == null) return;
            var selectedFolder = selectedItem.ToString();

            PrnListBox.SelectedIndex = -1;

            var cmds = new List<string> { "module add slurm", "cd _scratch", "cd " + Config.Config.Instace.ClusterWorkingDirectory, "cd " + selectedFolder, "ls" };
            var result = ((App)Application.Current).SSHManager.RunCommands(cmds).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(x=>x.EndsWith(".prn")).OrderBy(Config.Tools.ExtractNumberFromString).ToList();

            SelectDecPrnButton.IsEnabled = (result.Count > 0);
            
            PrnListBox.ItemsSource = result;
            PrnListBox.Items.Refresh();

        }

        private void SelectDecPrn_OnClick(object sender, RoutedEventArgs e)
        {
            PrnListBox.SelectedIndex = -1;

            PrnListBox.Focus();

            foreach (var item in PrnListBox.Items)
            {
                if (Config.Tools.ExtractNumberFromString((string) item)%10 == 0) PrnListBox.SelectedItems.Add(item);
            }

        }
    }
}
