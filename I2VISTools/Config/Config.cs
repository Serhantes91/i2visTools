using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Config
{
    public class Config
    {
        //делаем класс одиночным (singleton)
        private static Config _instance = new Config();

        /// <summary>
        /// Свойство возвращает единственно возможный экземпляр класса настроек приложения
        /// </summary>
        public static Config Instace
        {
            get { return _instance; }
        }

        public string UserLogin { get; set; }
        public string UserFingerprint { get; set; }
        public string UserKeyFilePath { get; set; }

        public string ClusterHost { get; set; }
        public int ClusterPort { get; set; }

        public string ClusterWorkingDirectory { get; set; }
        public string LocalWorkingDirectory { get; set; }
        public string DiaryPath { get; set; }

        public enum Cluster
        {
            Lomonosov,
            Chebyshev
        }

        public Cluster CurrentCluster { get; set; }

    }
}
