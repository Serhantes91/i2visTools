using System;
using System.Collections.Generic;
using System.Data;
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
    /// Interaction logic for CancelTaskWindow.xaml
    /// </summary>
    public partial class CancelTaskWindow : Window
    {
        public CancelTaskWindow()
        {
            InitializeComponent();
        }

        private void TaskNumberBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Config.Tools.OnlyNumeric(e.Text);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var cmds = new List<string> { "module add slurm", "squeue -u " + Config.Config.Instace.UserLogin };
            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);
            if (String.IsNullOrWhiteSpace(result))
            {
                MessageBox.Show("Ваших задач не обнаружено!");
                DialogResult = false;
            }

            var subresults = result.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            subresults.RemoveAt(0);
            
            SqueueListBox.ItemsSource = subresults;
            SqueueListBox.Items.Refresh();
            //SqueuesBox.Text = result;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (SqueueListBox.SelectedIndex == -1) return;

            var tasksIds = (from object item in SqueueListBox.SelectedItems select item.ToString().Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries)[0]).Aggregate("", (current, taskId) => current + (taskId + " "));
            tasksIds = tasksIds.Substring(0, tasksIds.Length - 1);

            if (String.IsNullOrWhiteSpace(tasksIds)) return;

            //var tasksText = (TaskNumberBox.Text == "all") ? "-u " + Config.Config.Instace.UserLogin : TaskNumberBox.Text;

            var cmds = new List<string> { "module add slurm", "scancel " + tasksIds };
            var result = ((App)Application.Current).SSHManager.RunCommandsWithReport(cmds);

            if (!String.IsNullOrWhiteSpace(result)) MessageBox.Show(result);
            DialogResult = true;

        }
    }
}
