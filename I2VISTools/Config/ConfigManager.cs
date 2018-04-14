using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace I2VISTools.Config
{
    public class ConfigManager
    {
        public static bool Write(string path)
        {
            
                if (File.Exists(path)) File.Delete(path);
                XmlSerializer x = new XmlSerializer(Config.Instace.GetType());
                StreamWriter writer = new StreamWriter(path);
                x.Serialize(writer, Config.Instace);
                return true;
            
        }


        public static bool Read(string path)
        {
            XmlSerializer x = new XmlSerializer(typeof(Config));
            StreamReader reader = new StreamReader(path);
            Config prefs = (Config)x.Deserialize(reader);

            Type myType = prefs.GetType();
            var props = new List<PropertyInfo>(myType.GetProperties());

            foreach (PropertyInfo prop in props)
            {
                var propValue = prop.GetValue(prefs, null);
                if (prop.Name == "Instace") continue;
                prop.SetValue(Config.Instace, propValue, null);
            }

            return true;
        }
    }
}
