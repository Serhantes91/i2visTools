using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using I2VISTools.Tools;

namespace I2VISTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string CurrentModelName { get; set; }

        public SshManager SSHManager { get; set; }

    }
}
