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

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for UserCInfigWindow.xaml
    /// </summary>
    public partial class UserCInfigWindow : Window
    {
        public UserCInfigWindow(bool fillBoxes = true)
        {
            InitializeComponent();
            if (fillBoxes) FillBoxes();
        }

        private void FillBoxes()
        {
            HostBox.Text = Config.Config.Instace.ClusterHost;
            PortBox.Text = Config.Config.Instace.ClusterPort.ToString();
            ClusterWorkingDirectoryBox.Text = Config.Config.Instace.ClusterWorkingDirectory;
            LocalDirectoryPath.FilePath = Config.Config.Instace.LocalWorkingDirectory;
            KeyFilePathBox.FilePath = Config.Config.Instace.UserKeyFilePath;
            PassphraseBox.Password = Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123");
            UserBox.Text = Config.Config.Instace.UserLogin;
            DiaryPath.FilePath = Config.Config.Instace.DiaryPath;
        }

        private void ApplySettings()
        {
            if (!CheckInputs())
            {
                MessageBox.Show("Не все поля заполнены!");
                return;
            }

            Config.Config.Instace.ClusterHost = HostBox.Text;
            Config.Config.Instace.ClusterPort = Config.Tools.ParseOrDefaultInt(PortBox.Text);
            Config.Config.Instace.ClusterWorkingDirectory = ClusterWorkingDirectoryBox.Text.EndsWith("/") ? ClusterWorkingDirectoryBox.Text : ClusterWorkingDirectoryBox.Text + "/";
            Config.Config.Instace.LocalWorkingDirectory = LocalDirectoryPath.FilePath;
            Config.Config.Instace.CurrentCluster = Config.Config.Cluster.Lomonosov;
            Config.Config.Instace.UserKeyFilePath = KeyFilePathBox.FilePath;
            Config.Config.Instace.UserFingerprint = Cryptor.Encrypt(PassphraseBox.Password, "abc123");
            Config.Config.Instace.UserLogin = UserBox.Text;
            Config.Config.Instace.DiaryPath = DiaryPath.FilePath;

            ((App)Application.Current).SSHManager = new SshManager(Config.Config.Instace);

        }

        private bool CheckInputs()
        {
            return !String.IsNullOrWhiteSpace(HostBox.Text) && 
                   !String.IsNullOrWhiteSpace(PortBox.Text) &&
                   !String.IsNullOrWhiteSpace(ClusterWorkingDirectoryBox.Text) &&
                   !String.IsNullOrWhiteSpace(LocalDirectoryPath.FilePath) &&
                   !String.IsNullOrWhiteSpace(KeyFilePathBox.FilePath) &&
                   !String.IsNullOrWhiteSpace(PassphraseBox.Password) && 
                   !String.IsNullOrWhiteSpace(UserBox.Text);
        }

        private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            if (CheckInputs()) DialogResult = true;
        }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            ApplySettings();

            ConfigManager.Write("config.xml");
            if (CheckInputs()) DialogResult = true;

        }
    }
}
