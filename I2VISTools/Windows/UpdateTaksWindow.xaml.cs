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
    /// Interaction logic for UpdateTaksWindow.xaml
    /// </summary>
    public partial class UpdateTaksWindow : Window
    {

        private List<string> _foldersList;

        public UpdateTaksWindow()
        {
            InitializeComponent();

            PartBox.Items.Add("hdd4");
            PartBox.Items.Add("hdd6");
            PartBox.Items.Add("regular4");
            PartBox.Items.Add("regular6");
            PartBox.Items.Add("gpu");
            PartBox.Items.Add("test");
            PartBox.Items.Add("gputest");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var cmds = new List<string> { "module add slurm", "cd _scratch", "cd " + Config.Config.Instace.ClusterWorkingDirectory, "ls" };
            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);

            _foldersList = result.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            _foldersList.Remove("base_lomonosov");

            TasksListBox.ItemsSource = _foldersList;
            TasksListBox.Items.Refresh();
        }
        
        private void UpdateTaskButton_Click(object sender, RoutedEventArgs e)
        {

            var cmds = new List<string> { "module add slurm", "module load intel/13.1.0", "module load mkl/4.0.2.146", "module load openmpi/1.5.5-icc", "cd _scratch", "cd " + Config.Config.Instace.ClusterWorkingDirectory };

            var time = Config.Tools.ParseOrDefaultInt(TimeBox.Text, -1);
            var timeString = (time == -1) ? "" : " -t " + time;

            if (PartBox.SelectionBoxItem.ToString().Contains("test") && time > 15) timeString = "";

            foreach (var curFolder in TasksListBox.SelectedItems)
            {
                cmds.Add(String.Format("cd {0}", curFolder));
                cmds.Add(String.Format("sbatch -p {0}{1} run i2fast", PartBox.SelectedItem, timeString));
                cmds.Add("cd ..");
            }

            cmds.RemoveAt(cmds.Count-1);

            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);
            MessageBox.Show(result);
        }
    }
}
