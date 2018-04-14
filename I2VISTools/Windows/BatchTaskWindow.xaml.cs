using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
using Microsoft.Win32;
using Renci.SshNet;
using Renci.SshNet.Common;
using Path = System.Windows.Shapes.Path;
using System.IO.Compression;
using I2VISTools.Subclasses;
using I2VISTools.Tools;
using Ionic.Zip;

namespace I2VISTools.Windows
{

    delegate void UpdateProgressBarDelegate(DependencyProperty dp, object value);

    /// <summary>
    /// Interaction logic for BatchTaskWindow.xaml
    /// </summary>
    public partial class BatchTaskWindow : Window
    {

        private string _initPath;
        private string _amirPath;
        private string _modePath;
        private string _filePath;

        BackgroundWorker worker;

        public BatchTaskWindow()
        {
            InitializeComponent();

            TaskPartNameBox.Items.Add("hdd4");
            TaskPartNameBox.Items.Add("hdd6");
            TaskPartNameBox.Items.Add("regular4");
            TaskPartNameBox.Items.Add("regular6");
            TaskPartNameBox.Items.Add("gpu");
            TaskPartNameBox.Items.Add("test");
            TaskPartNameBox.Items.Add("gputest");


            InitsListBox.ItemsSource = initsAndParts;

        }

        public BatchTaskWindow(string initFile, string amirFile = "", string modeFile = "") : this()
        {
            InitBrowseButton.IsEnabled = false;
            InitPathBox.Text = initFile;
            MultipleTasksTab.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(amirFile))
            {
                AmirBrowseButton.IsEnabled = false;
                AmirPathBox.Text = amirFile;
            }
            if (!string.IsNullOrWhiteSpace(modeFile))
            {
                ModeBrowseButton.IsEnabled = false;
                ModePathBox.Text = modeFile;
            }

            
        }

        private void InitBrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog {Filter = "t3c-файлы|*.t3c"};

            if (ofd.ShowDialog() == true)
            {
                _initPath = ofd.FileName;

                if (!_initPath.EndsWith("\\init.t3c"))
                {
                    MessageBox.Show("Файл должен носить название \"init.t3c\"! Никак иначе!");
                    return;
                }

                InitPathBox.Text = _initPath;
            }

        }

        private void AmirBrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "t3c-файлы|*.t3c" };

            if (ofd.ShowDialog() == true)
            {
                _amirPath = ofd.FileName;
                AmirPathBox.Text = _amirPath;
            }
        }

        private void FileBrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "t3c-файлы|*.t3c" };

            if (ofd.ShowDialog() == true)
            {
                _filePath = ofd.FileName;
                FilePathBox.Text = _filePath;
            }
        }

        private void ModeBrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "t3c-файлы|*.t3c" };

            if (ofd.ShowDialog() == true)
            {
                _modePath = ofd.FileName;
                ModePathBox.Text = _modePath;
            }
        }

        private bool CheckFields()
        {
            return (!String.IsNullOrWhiteSpace(TaskNameBox.Text) && !String.IsNullOrWhiteSpace(InitPathBox.Text) &&
                    TaskPartNameBox.SelectedIndex != -1);
        }

        private void In2fastButton_OnClick(object sender, RoutedEventArgs e)
        {

            if (!CheckFields())
            {
                MessageBox.Show("Не все обязательные поля заполнены!");
                return;
            }


            try
            {

                var modelName = TaskNameBox.Text;
                var uploadfile = InitPathBox.Text;
                var testpart = (TestCb.IsChecked == true) ? "test" : "gputest";

                var modeFile = ModePathBox.Text;
                var amirFile = AmirPathBox.Text;

                worker = new BackgroundWorker();
                worker.DoWork += (o, args) =>
                {
                    In2FastCommit(uploadfile, modelName, testpart, modeFile, amirFile);
                };

                worker.RunWorkerCompleted += (o, args) =>
                {
                    MessageBox.Show("Завершено.");
                    Dispatcher.BeginInvoke(new Action(delegate { ProcessBar.Value = 0; }));
                };

                ProcessBar.Maximum = 6;
                ProcessBar.Value = 0;
                worker.RunWorkerAsync();
                
                
            }
            catch (SshConnectionException ex)
            {
                MessageBox.Show("Не удалось подключиться по ssh");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }

            

        }

        private void In2FastCommit(string initfile, string modelName, string part, string modeFile = "", string amirFile = "", double barValue = 0)
        {

            UpdateProgressBarDelegate updProgress = ProcessBar.SetValue;
            
            var keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath,
                    Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123"));
            var keyFiles = new[] { keyFile };
            var username = Config.Config.Instace.UserLogin;

            var methods = new List<AuthenticationMethod>();
            methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

            var con = new ConnectionInfo(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort,
                username, methods.ToArray());
            var compilercon = new ConnectionInfo("compiler." + Config.Config.Instace.ClusterHost,
                Config.Config.Instace.ClusterPort, username, methods.ToArray());

            bool dirExists;

            Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);

            string result;
            using (var sshclient = new SshClient(compilercon))
            {
                sshclient.Connect();
                //TODO рабочая директория
                var commandResponse = sshclient.RunCommand("module add slurm\ncd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "ls");
                result = commandResponse.Result;
                var dirItems = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                var destinationFolder = modelName;

                if (dirItems.Contains(modelName))
                {
                    MessageBox.Show("В вашей кластерной рабочей директории уже существует задача с таким именем!");
                    return;
                }

                Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);

                dirExists = dirItems.Contains("base_lomonosov");

                using (
                    var sftpclient = new SftpClient(Config.Config.Instace.ClusterHost,
                        Config.Config.Instace.ClusterPort, username, keyFile))
                {
                    sftpclient.Connect();

                    var initialDirectory = sftpclient.WorkingDirectory;

                    if (!dirExists)
                    {
                        sshclient.RunCommand("cd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "mkdir base_lomonosov");
                        sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + Config.Config.Instace.ClusterWorkingDirectory + "base_lomonosov");

                        //копирование с локалки 
                        ZipFile zipFile = new ZipFile("base_lomonosov.zip");
                        var tempFolder = @"temp\";
                        zipFile.ExtractAll(tempFolder);

                        var fullTempPath = System.IO.Path.GetFullPath(@"temp\base_lomonosov");
                        var fileEntries = Directory.GetFiles(fullTempPath);

                        foreach (var file in fileEntries)
                        {
                            using (var fileStream = new FileStream(file, FileMode.Open))
                            {
                                sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                                sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(file), true);
                            }
                        }

                        Directory.Delete(fullTempPath, true);

                    }

                    Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);

                    var cr =
                            sshclient.RunCommand(
                                "module add slurm\ncd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "scp -r base_lomonosov " + destinationFolder);
                    result = cr.Result;

                    sftpclient.ChangeDirectory(initialDirectory + "/_scratch/" + Config.Config.Instace.ClusterWorkingDirectory +
                                               destinationFolder);
                    //sftpclient.DeleteFile(@"init.t3c");

                    using (var fileStream = new FileStream(initfile, FileMode.Open))
                    {
                        sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                        sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(initfile), true);
                    }

                    if (!string.IsNullOrWhiteSpace(modeFile))
                    {
                        using (var fileStream = new FileStream(modeFile, FileMode.Open))
                        {
                            sftpclient.BufferSize = 4 * 1024; 
                            sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(modeFile), true);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(amirFile))
                    {
                        using (var fileStream = new FileStream(amirFile, FileMode.Open))
                        {
                            sftpclient.BufferSize = 4 * 1024;
                            sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(amirFile), true);
                        }
                    }

                    Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);
                }

                var commit = sshclient.RunCommand("module add slurm\n" +
                                                  "module load intel/13.1.0\n" +
                                                  "module load mkl/4.0.2.146\n" +
                                                  "module load openmpi/1.5.5-icc\n" +
                                                  "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + destinationFolder + "/\n" +
                                                  "touch cmp.sh\n" +
                                                  "echo \"icc -o in2fast in2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                  "echo \"icc -o i2fast i2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                  "echo \"icc -o i2jslab i2jslab.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                  "chmod +x cmp.sh\n" +
                                                  "./cmp.sh\n");


            }

            Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);

            using (var sshclient = new SshClient(con))
            {
                sshclient.Connect();
                var commandResponse = sshclient.RunCommand("module add slurm\n" +
                                                           "module load intel/13.1.0\n" +
                                                           "module load mkl/4.0.2.146\n" +
                                                           "module load openmpi/1.5.5-icc\n" +
                                                           "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + modelName + "/\n" +
                                                           String.Format("sbatch -p {0} run in2fast\n", part));
            }
            Dispatcher.Invoke(updProgress, ProgressBar.ValueProperty, ++barValue);
        }

        private string DiscretizePath(string path)
        {
            var separateFolders = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries); //todo подумать нужно ли вставлять бэкслеш в качестве разделителя
            return separateFolders.Aggregate("", (current, fld) => current + String.Format("cd {0}\n", fld));
        }

        private void RunAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckFields())
            {
                MessageBox.Show("Не все обязательные поля заполнены!");
                return;
            }

            try
            {
                //TODO инкапсулировать подключение к ssh
                var modelName = TaskNameBox.Text;
                var uploadfile = InitPathBox.Text;
                var selectedPart = TaskPartNameBox.SelectedItem as string;

                var keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath, Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123"));
                var keyFiles = new[] { keyFile };
                var username = Config.Config.Instace.UserLogin;

                var methods = new List<AuthenticationMethod>();
                methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

                var con = new ConnectionInfo(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, methods.ToArray());
                var compilercon = new ConnectionInfo("compiler." + Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, methods.ToArray());

                bool dirExists;

                var timeAddit = String.IsNullOrWhiteSpace(TimeBox.Text) ? "" : " -t " + TimeBox.Text;  //todo регулярное выр-ие в timebox чтобы там всегда только цифры были

                string result;
                using (var sshclient = new SshClient(compilercon))
                {
                    sshclient.Connect();
                    //TODO рабочая директория
                    var commandResponse = sshclient.RunCommand("module add slurm\ncd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "ls"); 
                    result = commandResponse.Result;
                    var dirItems = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                    var destinationFolder = modelName;

                    if (dirItems.Contains(modelName))
                    {
                        MessageBox.Show("В вашей кластерной рабочей директории уже существует задача с таким именем!");
                        return;
                    }

                    dirExists = dirItems.Contains("base_lomonosov"); //todo по-хорошему здесь бы не только проверять наличие директории, но и файлов внутри

                    using (var sftpclient = new SftpClient(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, keyFile))
                    {
                        sftpclient.Connect();

                        var initialDirectory = sftpclient.WorkingDirectory;

                        if (!dirExists)
                        {
                            sshclient.RunCommand("cd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "mkdir base_lomonosov");
                            sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/"+ Config.Config.Instace.ClusterWorkingDirectory + "base_lomonosov");

                            //копирование с локалки 
                            ZipFile zipFile = new ZipFile("base_lomonosov.zip");
                            var tempFolder = @"temp\";
                            zipFile.ExtractAll(tempFolder);

                            var fullTempPath = System.IO.Path.GetFullPath(@"temp\base_lomonosov");
                            var fileEntries = Directory.GetFiles(fullTempPath);

                            foreach (var file in fileEntries)
                            {
                                using (var fileStream = new FileStream(file, FileMode.Open))
                                {
                                    sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                                    sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(file), true);
                                }
                            }

                            Directory.Delete(fullTempPath, true);
                        }

                        //var cr = sshclient.RunCommand("scp -r base_lomonosov " + destinationFolder);
                        var cr = sshclient.RunCommand("module add slurm\ncd _scratch\n" + DiscretizePath(Config.Config.Instace.ClusterWorkingDirectory) + "scp -r base_lomonosov " + destinationFolder);
                        result = cr.Result;

                        sftpclient.ChangeDirectory(initialDirectory); 
                        sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + Config.Config.Instace.ClusterWorkingDirectory + destinationFolder);
                        //sftpclient.DeleteFile(@"init.t3c");

                        using (var fileStream = new FileStream(uploadfile, FileMode.Open))
                        {
                            sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                            sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(uploadfile), true);
                        }


                        sftpclient.Disconnect();
                    }

                    var commit = sshclient.RunCommand("module add slurm\n" +
                                                          "module load intel/13.1.0\n" +
                                                          "module load mkl/4.0.2.146\n" +
                                                          "module load openmpi/1.5.5-icc\n" +
                                                          "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + destinationFolder + "/\n" + //todo если что - ещё слешы вокруг working directory и n
                                                          "touch cmp.sh\n" +
                                                          "echo \"icc -o in2fast in2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                          "echo \"icc -o i2fast i2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                          "echo \"icc -o i2jslab i2jslab.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                                                          "chmod +x cmp.sh\n" +
                                                          "./cmp.sh\n");

                    //if (!string.IsNullOrWhiteSpace(commit.Error))
                    //{
                    //    MessageBox.Show(commit.Error, "Есть ошибки!");
                    //}

                    //result = commit.Result;
                }

                var testpart = (TestCb.IsChecked == true) ? "test" : "gputest";

                using (var sshclient = new SshClient(con))
                {
                    sshclient.Connect();
                    //TODO рабочая директория
                    var commandResponse = sshclient.RunCommand("module add slurm\n" +
                                                          "module load intel/13.1.0\n" +
                                                          "module load mkl/4.0.2.146\n" +
                                                          "module load openmpi/1.5.5-icc\n" +
                                                          "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + modelName + "//\n" +
                                                          String.Format("sbatch -p {0} run in2fast\n", testpart) +
                                                          String.Format("sbatch -p {0} run i2fast\n", selectedPart+timeAddit));

                    MessageBox.Show(commandResponse.Result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Last());
                }
            }
            catch (SshConnectionException ex)
            {
                MessageBox.Show("Не удалось подключиться по ssh");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }


        private void I2fastButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!CheckFields())
            {
                MessageBox.Show("Не все обязательные поля заполнены!");
                return;
            }

            try
            {
                //TODO инкапсулировать подключение к ssh
                var modelName = TaskNameBox.Text;
                var uploadfile = InitPathBox.Text;
                var selectedPart = TaskPartNameBox.SelectedItem as string;

                var keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath, Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123") );
                var keyFiles = new[] { keyFile };
                var username = Config.Config.Instace.UserLogin;

                var methods = new List<AuthenticationMethod>();
                methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

                var con = new ConnectionInfo(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, methods.ToArray());
                var compilercon = new ConnectionInfo("compiler." + Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, methods.ToArray());

                var timeAddit = String.IsNullOrWhiteSpace(TimeBox.Text) ? "" : " -t " + TimeBox.Text;

                if (selectedPart != null && (selectedPart.Contains("test") && Config.Tools.ParseOrDefaultInt(timeAddit) > 15))
                    timeAddit = " -t 15";

                #region компиляция (особо не нужна пока)

                //bool dirExists;

                //string result;
                //using (var sshclient = new SshClient(compilercon))
                //{
                //    sshclient.Connect();
                //    //TODO рабочая директория
                //    var commandResponse = sshclient.RunCommand("module add slurm\ncd _scratch\ncd collision\nls");
                //    result = commandResponse.Result;
                //    var dirItems = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                //    var destinationFolder = modelName;

                //    dirExists = dirItems.Contains("base_lomonosov");

                //    using (var sftpclient = new SftpClient(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort, username, keyFile))
                //    {
                //        sftpclient.Connect();

                //        if (dirExists)
                //        {
                //            //var cr = sshclient.RunCommand("scp -r base_lomonosov " + destinationFolder);
                //            var cr = sshclient.RunCommand("module add slurm\ncd _scratch\ncd collision\nscp -r base_lomonosov " + destinationFolder);
                //            result = cr.Result;

                //            sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/collision/" + destinationFolder);
                //            //sftpclient.DeleteFile(@"init.t3c");

                //            using (var fileStream = new FileStream(uploadfile, FileMode.Open))
                //            {
                //                sftpclient.BufferSize = 4 * 1024; // bypass Payload error large files
                //                sftpclient.UploadFile(fileStream, System.IO.Path.GetFileName(uploadfile), true);
                //            }

                //        }
                //        else
                //        {
                //            //TODO копирование с локалки 
                //        }

                //    }

                //    var commit = sshclient.RunCommand("module add slurm\n" +
                //                                          "module load intel/13.1.0\n" +
                //                                          "module load mkl/4.0.2.146\n" +
                //                                          "module load openmpi/1.5.5-icc\n" +
                //                                          "cd _scratch//collision//" + destinationFolder + "//\n" +
                //                                          "touch cmp.sh\n" +
                //                                          "echo \"icc -o in2fast in2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                //                                          "echo \"icc -o i2fast i2fast.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                //                                          "echo \"icc -o i2jslab i2jslab.c -mkl -lpthread -mcmodel=medium\" >> cmp.sh \n" +
                //                                          "chmod +x cmp.sh\n" +
                //                                          "./cmp.sh\n");

                //    //if (!string.IsNullOrWhiteSpace(commit.Error))
                //    //{
                //    //    MessageBox.Show(commit.Error, "Есть ошибки!");
                //    //}

                //    //result = commit.Result;
                //}

                #endregion

                using (var sshclient = new SshClient(con))
                {
                    sshclient.Connect();
                    //TODO рабочая директория
                    var commandResponse = sshclient.RunCommand("module add slurm\n" +
                                                          "module load intel/13.1.0\n" +
                                                          "module load mkl/4.0.2.146\n" +
                                                          "module load openmpi/1.5.5-icc\n" +
                                                          "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + modelName + "//\n" +
                                                          String.Format("sbatch -p {0} run i2fast\n", selectedPart+timeAddit));

                    MessageBox.Show(commandResponse.Result.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Last());
                }
            }
            catch (SshConnectionException ex)
            {
                MessageBox.Show("Не удалось подключиться по ssh");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CheckButton_OnClick(object sender, RoutedEventArgs e)
        {

            var part = (TestCb.IsChecked == true) ? "test" : "gputest";
            
            var cmds = new List<string> { "module add slurm", "squeue -p " + part + " -u " + Config.Config.Instace.UserLogin};

            var result = ((App)Application.Current).SSHManager.RunCommands(cmds);

            result = result.Substring(result.IndexOf('\n'), result.Length - result.IndexOf('\n'));

            if (String.IsNullOrWhiteSpace(result)) result = String.Format("Ваших задач в {0} не обнаружено. Возможно, всё уже посчиталось.", part);

            MessageBox.Show(result);

            //var resultSubStrings = result.Split('\n');

            //foreach (var substring in resultSubStrings)
            //{
            //    if (String.IsNullOrWhiteSpace(substring)) continue;
            //    var squeueParams = substring.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            //    string jobId, partition, user, status, time, nodelist;

            //    jobId = squeueParams[0];
            //    partition = squeueParams[1];
            //    user = squeueParams[3];
            //    status = squeueParams[4];
            //    time = squeueParams[5];
            //    nodelist = squeueParams[7];

            //    squeueTable.Rows.Add(jobId, partition, user, status, time, nodelist);
            //}
        }

        private void TimeBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Config.Tools.OnlyNumeric(e.Text);
        }

        /// <summary>
        /// Словарь с init-файлами и разделом кластера, куда запускать
        /// Ключ - путь к init-файлу, значение - раздел
        /// </summary>
        List<TaskInfo> initsAndParts = new List<TaskInfo>(); 

        private void AddInitButton_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Init-файл (*.t3c)|*.t3c",
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                foreach (var fname in ofd.FileNames)
                {
                    if (!initsAndParts.Select(x=>x.InitPath).ToList().Contains(fname))
                    {
                        initsAndParts.Add(new TaskInfo() {InitPath = fname, Part = "regular4"} );
                    }
                    else
                    {
                        MessageBox.Show("Файл " + fname + " уже есть в вашем списке!");
                    }
                }
                InitsListBox.Items.Refresh();
                
            }
        }

        private void RemoveInitButton_Click(object sender, RoutedEventArgs e)
        {
            if (InitsListBox.SelectedIndex == -1) return;

            var selectedTask = InitsListBox.SelectedItem as TaskInfo;
            
            if (selectedTask == null) return;

            initsAndParts.Remove(selectedTask);
            InitsListBox.Items.Refresh();
            

        }


        //private void InitsListBox_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    try
        //    {
        //        var item =
        //            ItemsControl.ContainerFromElement(InitsListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
        //        if (item != null)
        //        {

        //            var menu = new ContextMenu();

        //            foreach (var part in TaskPartNameBox.Items)
        //            {
        //                var mi = new MenuItem
        //                {
        //                    Header = part.ToString(),
        //                    Tag = part.ToString()
        //                };

        //                mi.Click += (o, args) =>
        //                {
        //                    var selectedTask = item.Content as TaskInfo;
        //                    initsAndParts[selectedKey] = mi.Tag.ToString();
        //                    InitsListBox.Items.Refresh();
        //                };

        //                menu.Items.Add(mi);
        //            }

        //            menu.IsOpen = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message);
        //    }
            
        //}

        private void MultiIn2FastButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AllNameAreFilled()) return;
            if (!AllNamesAreDifferent()) return;
            if (!AllNameAreCorrect()) return;
        }

        private void MultiI2FastButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AllNameAreFilled()) return;
            if (!AllNamesAreDifferent()) return;
            if (!AllNameAreCorrect()) return;
        }

        private bool AllNameAreFilled()
        {
            if (initsAndParts.Any(task => string.IsNullOrWhiteSpace(task.Name)))
            {
                MessageBox.Show("В вашем списке не всем задачам было присвоено имя!");
                return false;
            }
            return true;
        }

        private bool AllNamesAreDifferent()
        {
            if (initsAndParts.Select(x => x.Name).ToList().Distinct().Count() != initsAndParts.Count)
            {
                MessageBox.Show("Вы присвоили нескольким задачам одно и то же имя!");
                return false;
            }
            return true;
        }

        private bool AllNameAreCorrect()
        {
            foreach (var task in initsAndParts)
            {
                if (task.Name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) != -1)
                {
                    MessageBox.Show("В имени \"" + task.Name + "\"" + " использованы недопустимые символы!");
                    return false;
                }
            }
            return true;
        }
    }
}
