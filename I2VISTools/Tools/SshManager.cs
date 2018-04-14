using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Renci.SshNet;

namespace I2VISTools.Tools
{
    public class SshManager
    {

        public string KeyFile { get; set; }
        public string Passphrase { get; set; }

        public string UserName { get; set; }
        public string Host { get; set; }

        public int Port { get; set; }

        private PrivateKeyFile _privateKeyFile;
        private ConnectionInfo _connectionInfo;

        private ConnectionInfo _compilerConnectionInfo;

        public int TimeOut { get; set; }

        public SshManager(Config.Config config)
        {
            try
            {
                KeyFile = config.UserKeyFilePath;
                Passphrase = config.UserFingerprint;
                UserName = config.UserLogin;
                Host = config.ClusterHost;
                Port = config.ClusterPort;

                _privateKeyFile = new PrivateKeyFile(KeyFile, Cryptor.Decrypt(Passphrase, "abc123"));

                var keyFiles = new[] {_privateKeyFile};
                var username = UserName;

                var methods = new List<AuthenticationMethod>();
                methods.Add(new PrivateKeyAuthenticationMethod(username, keyFiles));

                _connectionInfo = new ConnectionInfo(Host, Port, username, methods.ToArray())
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };

                _compilerConnectionInfo = new ConnectionInfo("compiler." + Host, Port, username, methods.ToArray())
                {
                    Timeout = TimeSpan.FromSeconds(60)
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }


        public string RunCommands(List<string> commands, bool isCompiler = false )
        {
            string result;

            var curConnect = (isCompiler) ? _compilerConnectionInfo : _connectionInfo;

            try
            {
                using (var client = new SshClient(curConnect))
                {
                    client.Connect();

                    var totalString = commands.Aggregate("", (current, command) => current + (command + "\n"));
                    var commandResponse = client.RunCommand(totalString);

                    result = commandResponse.Result;
                    client.Disconnect();
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ((ex.InnerException == null) ? "" : ex.InnerException.Message), "Ошибка соединения!");
                return null;
            }

            

            

            
        }

        public string RunCommand(string command, bool isCompiler = false)
        {
            var lst = new List<string> {command};
            return RunCommands(lst, isCompiler);
        }

        public string RunCommandsWithReport(List<string> commands, bool isCompiler = false)
        {
            string result;

            var curConnect = (isCompiler) ? _compilerConnectionInfo : _connectionInfo;

            using (var client = new SshClient(curConnect))
            {
                client.Connect();

                var totalString = commands.Aggregate("", (current, command) => current + (command + "\n"));
                var commandResponse = client.RunCommand(totalString);

                result = commandResponse.Result;
                result += commandResponse.Error;

                client.Disconnect();
            }
            return result;
        }

    }
}
