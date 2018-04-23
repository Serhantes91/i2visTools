using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using I2VISTools.Subclasses;
using I2VISTools.Tools;
using Renci.SshNet;
using Application = System.Windows.Application;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;

namespace I2VISTools.Windows
{
    /// <summary>
    /// Interaction logic for FilesViewWindow.xaml
    /// </summary>
    public partial class FilesViewWindow : Window
    {

        //private List<string> _files = new List<string>();

        private List<ClusterFileInfo> _files = new List<ClusterFileInfo>();

        BackgroundWorker worker = new BackgroundWorker();

        private string _initialPath;

        private string _curPath;

        public string CurrentPath
        {
            get { return _curPath; }
            set
            {
                _curPath = value;
                Dispatcher.BeginInvoke(new Action(delegate { CurrentDirectoryLabel.Content = _curPath; }));
                
            }
        }

        PrivateKeyFile keyFile = new PrivateKeyFile(Config.Config.Instace.UserKeyFilePath, Cryptor.Decrypt(Config.Config.Instace.UserFingerprint, "abc123")); //todo надо сделать подобный ssh sftp-manager
        string username = Config.Config.Instace.UserLogin;
        private SftpClient sftpclient;

        public FilesViewWindow()
        {
            InitializeComponent();

            FilesListView.ItemsSource = _files;
            _initialPath = "_scratch/" + Config.Config.Instace.ClusterWorkingDirectory;

            sftpclient = new SftpClient(Config.Config.Instace.ClusterHost, Config.Config.Instace.ClusterPort,
                username, keyFile);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var bwork = new BackgroundWorker();
            bwork.DoWork += (o, args) =>
            {
                var cmds = new List<string> { "cd _scratch/" + Config.Config.Instace.ClusterWorkingDirectory + "/", "ls -la --time-style=full-iso" };
                var result = ((App)Application.Current).SSHManager.RunCommands(cmds);

                CurrentPath = _initialPath;

                Dispatcher.BeginInvoke(new Action(delegate { FillFilesView(result); }));
            };

            bwork.RunWorkerCompleted += (o, args) =>
            {
                Dispatcher.BeginInvoke(new Action(delegate { LoadingBar.Visibility = Visibility.Collapsed; }));
            };

            bwork.RunWorkerAsync();
            
        }

        private void FillFilesView(string result)
        {
            var fls = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            fls.RemoveAt(0);

            _files.Clear();

            FilesListView.ItemsSource = null;

            if (_initialPath != CurrentPath) _files.Add(new ClusterFileInfo() {Name = "   ", Type = "dup"} ) ;

            foreach (var file in fls)
            {
                if (file == @"cat: /etc/banner: No such file or directory") continue; //todo криво, конечно. надо подумать как изменить

                
                var curFileArray = file.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (curFileArray.Count() < 9 ) continue;
                if (curFileArray.Last() == "." || curFileArray.Last() == "..") continue;
                //10 May 2008 14:32:17 GMT


                if (curFileArray.Last() == "base_lomonosov" && _initialPath == CurrentPath) continue; //скрываем базовую папку, ибо нефиг её трогать

                var md = DateTime.Today;

                DateTime.TryParse(String.Format("{0} {1}", curFileArray[5], curFileArray[6]), out md);

                var curFileInfo = new ClusterFileInfo { 
                    Type = curFileArray[0], 
                    LinkCount = Config.Tools.ParseOrDefaultInt(curFileArray[1]), 
                    Owner = curFileArray[2], Group = curFileArray[3], 
                    Size = Config.Tools.ParseOrDefaultUInt(curFileArray[4], 0), 
                    ModificationDate = md, 
                    Name = curFileArray[8]};

                _files.Add(curFileInfo);
            }

            FilesListView.ItemsSource = _files;
            FilesListView.Items.Refresh();
            
        }


        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {

            if (e.ChangedButton != MouseButton.Left) return;

            var currentItem = FilesListView.SelectedItem as ClusterFileInfo;
            if (currentItem == null) return;

            if (currentItem.Type[0] != 'd') return;

            if (currentItem.Name == "   " || currentItem.Name == "zzzzz")
            {
                CurrentPath = CurrentPath.Substring(0, CurrentPath.Remove(CurrentPath.Length - 1).LastIndexOf("/", StringComparison.Ordinal) + 1);
            }
            else
            {
                CurrentPath += currentItem.Name + "/";
            }

            var cmds = new List<string> { "cd " + CurrentPath, "ls -la --time-style=full-iso" };
            var result = ((App)Application.Current).SSHManager.RunCommandsWithReport(cmds);


            FillFilesView(result);

        }

        private void FilesListView_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            var upItem = _files.FirstOrDefault(x => (x.Name == "   " || x.Name == "zzzzz" ) && x.Type == "dup");
            if (upItem == null) return;

            var sortedColumn = e.Column;
            if (sortedColumn == null) return;

            if (sortedColumn.SortDirection == ListSortDirection.Ascending)
            {
                upItem.Name = "zzzzz" ;
                upItem.ModificationDate = DateTime.MaxValue;
                upItem.Size = UInt32.MaxValue;
            }
            else
            {
                upItem.Name = "   ";
                upItem.ModificationDate = new DateTime();
                upItem.Size = 0;
            }

        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            var cmds = new List<string> { "cd " + CurrentPath, "ls -la --time-style=full-iso" };
            var result = ((App)Application.Current).SSHManager.RunCommandsWithReport(cmds);

            FillFilesView(result);
        }

        private void RightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();
            var dwnldItem = new MenuItem {Header = "Скачать", Tag = "download"};

            var dgRow = sender as DataGridRow;
            if (dgRow == null) return;
            var fi = dgRow.Item as ClusterFileInfo;
            if (fi == null || fi.Type == "dup") return;
            //if (fi == null || fi.Type[0] == 'd') return;

            if (FilesListView.SelectedItems == null || FilesListView.SelectedItems.Count == 0 || !FilesListView.SelectedItems.Contains(dgRow.Item))
            {
                FilesListView.SelectedItem = dgRow.Item;
            }

            dwnldItem.Click += (ss, ee) =>
            {
                if (FilesListView.SelectedItems == null || FilesListView.SelectedItems.Count == 0) return;

                List<ClusterFileInfo> files = new List<ClusterFileInfo>();

                foreach (var item in FilesListView.SelectedItems)
                {
                    var curFile = item as ClusterFileInfo;
                    if (curFile == null) continue;
                    if (curFile.Type[0] == 'd') continue;
                    files.Add(curFile);
                }

                if (files.Count == 0) return;


                var fbd = new FolderBrowserDialog();
                fbd.ShowNewFolderButton = false;
                DialogResult fbdresult = fbd.ShowDialog();
                if (fbdresult != System.Windows.Forms.DialogResult.OK) return;

                var folderPath = fbd.SelectedPath;

                var existingFiles = Directory.GetFiles(folderPath).Select(x=> x.Substring(x.LastIndexOf("\\", StringComparison.Ordinal)+1) ).ToList();

                foreach (var file in files)
                {
                    if (existingFiles.Contains(file.Name))
                    {
                        bool rewrite = (System.Windows.MessageBox.Show("В данном каталоге уже есть файлы с таким именем. Хотите ли вы их перезаписать?", "Перезапись", MessageBoxButton.YesNo, MessageBoxImage.Warning) ==
                        MessageBoxResult.Yes);
                        if (rewrite) break; else return;
                    }
                }

                //worker.WorkerReportsProgress = true;

                var stopWatch = new Stopwatch();

                DownloadProgressBar.Minimum = 0;
                //DownloadProgressBar.Maximum = files.Sum(x => x.Size);
                worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;
                worker.DoWork += (o, args) =>
                {
                    sftpclient.Connect();
                    sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + CurrentPath);
                    Action<ulong> progressAction = x =>
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            DownloadProgressBar.Value = x;
                            if (stopWatch.ElapsedMilliseconds % 2000 < 50 && stopWatch.ElapsedMilliseconds > 1000) LoadSpeedLabel.Content = GetSizeString( (x / (ulong) (stopWatch.ElapsedMilliseconds/1000d)) ) + "/сек";
                        }));
                    };

                    var curFileCt = 0;

                    foreach (var file in files)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate { DownloadProgressBar.Maximum = file.Size;
                                                                       DownloadingFileLabel.Content = file.Name;
                                                                       FilesStateLabel.Content = curFileCt++ + "/" + files.Count;
                                                                       stopWatch.Restart();
                        }));

                        using (var fileStream = new FileStream(folderPath + "\\" + file.Name, FileMode.Create))
                        {
                            sftpclient.BufferSize = 1536 * 1024; // bypass Payload error large files
                            sftpclient.DownloadFile(file.Name, fileStream, progressAction);
                        }
                    }
                };

                worker.RunWorkerCompleted += (o, args) =>
                {
                    DownloadProgressBar.Value = 0;
                    DownloadProgressBar.Visibility = Visibility.Collapsed;
                    DownloadingFileLabel.Content = "";
                    LoadSpeedLabel.Content = "";
                    FilesStateLabel.Content = "";
                    stopWatch.Stop();
                    FilesListView.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    sftpclient.Disconnect();
                };

                DownloadProgressBar.Visibility = Visibility.Visible;
                FilesListView.IsEnabled = false;
                RefreshButton.IsEnabled = false;
                worker.RunWorkerAsync();

                //sftpclient.Disconnect();
                

            };
            
            menu.Items.Add(dwnldItem);

            var deleteItem = new MenuItem
            {
                Header = "Удалить",
                Tag = "delete"
            };

            deleteItem.Click += (o, args) =>
            {
                bool confirmDelete = (System.Windows.MessageBox.Show("Вы уверены, что хотите удалить файл(ы)? Файл(ы) будут удалены безвозвратно.", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning) ==
                        MessageBoxResult.Yes);
                if (!confirmDelete) return;

                List<ClusterFileInfo> files = new List<ClusterFileInfo>();

                foreach (var item in FilesListView.SelectedItems)
                {
                    var curFile = item as ClusterFileInfo;
                    if (curFile == null) continue;
                    files.Add(curFile);
                }

                sftpclient.Connect();
                sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + CurrentPath);

                foreach (var file in files)
                {
                    if (file.Type == "dup") continue;
                    if (file.Type[0] == 'd')
                    {
                        //sftpclient.DeleteDirectory(file.Name);
                        var cmds = new List<string> { "cd _scratch/" + CurrentPath,  "rm -rf " + file.Name };
                        var result = ((App)Application.Current).SSHManager.RunCommandsWithReport(cmds);
                    }
                    else
                    {
                        sftpclient.DeleteFile(file.Name);
                    }
                }
                
                sftpclient.Disconnect();
                Refresh();

                
            };

            menu.Items.Add(deleteItem);

            if (FilesListView.SelectedItems != null && FilesListView.SelectedItems.Count == 1)
            {

                var renameItem = new MenuItem
                {
                    Header = "Переименовать",
                    Tag = "rename"
                };
                renameItem.Click += (o, args) =>
                {
                    var mnw = new ModelNameWindow(fi.Name);
                    if (mnw.ShowDialog() == true)
                    {
                        var name = mnw.OutName;

                        List<ClusterFileInfo> files = FilesListView.Items.OfType<ClusterFileInfo>().ToList();
                        if (files.FirstOrDefault(x => x.Name == name) != null)
                        {
                            MessageBox.Show(@"Файл/папка с таким именем уже присутствует!");
                            return;
                        }

                        var cmds = new List<string> { "cd _scratch/" + CurrentPath, "mv " + fi.Name + " " + name };
                        ((App)Application.Current).SSHManager.RunCommandsWithReport(cmds);
                        Refresh();
                    }
                };
                menu.Items.Add(renameItem);
            }

            //foreach (MenuItem item in menu.Items)
            //{
            //    item.Click += (ss, ee) =>
            //    {
            //        Lithology.AddInterval(new LithologyInterval() { Top = Math.Round(range.MinimumY, 1), Base = Math.Round(range.MaximumY, 1), Value = (ss as MenuItem).Tag.ToString() });
            //    };
            //}

            menu.PlacementTarget = FilesListView;
            menu.IsOpen = true;
        }

        private void ViewWindow_Closing(object sender, CancelEventArgs e)
        {
            if (worker.IsBusy) worker.CancelAsync(); //todo не работает
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesListView.SelectedIndex == -1) FilesStateLabel.Content = "";

            var filesNum = 0;

            foreach (var si in FilesListView.SelectedItems)
            {
                var file = si as ClusterFileInfo;
                if (file == null) continue;
                if (file.Type == "dup") continue;
                filesNum++;
            }

            FileSelectedLabel.Content = "Кол-во выделенных объектов: " + filesNum;
            if (filesNum == 0) FileSelectedLabel.Content = "";
        }

        

        private void FilesListView_OnDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (!files.Any()) return;

            //sftpclient.Connect();
            //sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + CurrentPath);

            worker = new BackgroundWorker();
            DownloadProgressBar.Minimum = 0;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += (o, args) =>
            {
                sftpclient.Connect();
                sftpclient.ChangeDirectory(sftpclient.WorkingDirectory + "/_scratch/" + CurrentPath);
                Action<ulong> progressAction = x =>
                {
                    Dispatcher.BeginInvoke(new Action(delegate { DownloadProgressBar.Value = x; }));
                };

                foreach (var file in files)
                {
                    Dispatcher.BeginInvoke(new Action(delegate
                    {
                        DownloadProgressBar.Maximum = new FileInfo(file).Length;
                        DownloadingFileLabel.Content = file.Substring(file.LastIndexOf("\\", StringComparison.Ordinal)+1);
                    }));

                    using (var fileStream = new FileStream(file, FileMode.Open))
                    {
                        sftpclient.BufferSize = 1536 * 1024;
                        if (IsDirectory(file))
                        {
                            var newDirName = file.Substring(file.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
                            sftpclient.CreateDirectory(newDirName);

                        }
                        else
                        {
                            sftpclient.UploadFile(fileStream, file.Substring(file.LastIndexOf("\\", StringComparison.Ordinal) + 1), true, progressAction);
                        }
                        
                    }
                }
            };

            worker.RunWorkerCompleted += (o, args) =>
            {
                DownloadProgressBar.Value = 0;
                DownloadProgressBar.Visibility = Visibility.Collapsed;
                DownloadingFileLabel.Content = "";
                FilesListView.IsEnabled = true;
                sftpclient.Disconnect();

                Dispatcher.BeginInvoke(new Action(Refresh));

            };

            DownloadProgressBar.Visibility = Visibility.Visible;
            FilesListView.IsEnabled = false;
            worker.RunWorkerAsync();

        }


        private bool IsDirectory(string fname)
        {
            return Directory.Exists(fname);
        }

        private string GetSizeString(ulong curSize)
        {
            if (curSize < 1024) return curSize + " Б";
            if (curSize >= 1024 && curSize < 1024 * 1024) return (curSize / 1024d).ToString("0.###") + " КБ";
            if (curSize >= 1024 * 1024 && curSize < 1024 * 1024 * 1024) return (curSize / Math.Pow(1024, 2)).ToString("0.###") + " МБ";
            if (curSize >= 1024 * 1024 * 1024) return (curSize / Math.Pow(1024, 3)).ToString("0.###") + " ГБ";
            return "";
        }
    }
}
