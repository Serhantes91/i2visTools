using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.Subclasses
{
    public class ClusterFileInfo
    {
        public string Type { get; set; }
        public int LinkCount { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }
        public uint Size { get; set; }
        public DateTime ModificationDate { get; set; }
        public string Name { get; set; }

        public string Extension
        {
            get
            {
                if (Type == "dup" && Name == "   ") return null;
                if (Type == "dup" && Name == "zzzzz") return "zz?";
                if (Type[0] == 'd') return "'folder";
                var lastPtInd = Name.LastIndexOf(".", StringComparison.Ordinal);
                if (lastPtInd < 0) return "";
                if (lastPtInd == Name.Length - 1) return "";
                
                return Name.Substring(lastPtInd + 1).ToUpper();
            }
        }
    }
}
