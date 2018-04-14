using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I2VISTools.ModelConfigClasses
{
    public class OutputAmirUnit
    {
        //LOADFiLE_TYPE_AMIRAFILE_TYPE_XBEG_XEND_XRESOL_YBEG_YEND_YRESOL
        //voac0.prn     b voacc0.txt     c    0.0 1.0 4001 0.0 1.0  401

        public string LoadFile { get; set; }
        public char LoadFileType { get; set; }
        public string AmirFile { get; set; }
        public char AmirFileType { get; set; }
        public double XBeg { get; set; }
        public double XEnd { get; set; }
        public int XResolution { get; set; }
        public double YBeg { get; set; }
        public double YEnd { get; set; }
        public int YResolution { get; set; }

        public OutputAmirUnit()
        {
            
        }

        public OutputAmirUnit(string loadFile, char amirType)
        {

            loadFile = loadFile.Substring(loadFile.LastIndexOf(@"\", StringComparison.Ordinal)+1);

            LoadFile = loadFile;
            LoadFileType = 'b';

            var numPartStartIndex = loadFile.IndexOfAny("0123456789".ToCharArray());
            var numPartLastIndex = loadFile.LastIndexOfAny("0123456789".ToCharArray());
            var namePartString = loadFile.Substring(0, numPartStartIndex);
            var numPartString = loadFile.Substring(numPartStartIndex, numPartLastIndex - numPartStartIndex + 1);

            AmirFile = namePartString + amirType + numPartString + ".txt" ;
            AmirFileType = amirType;
            XBeg = 0;
            XEnd = 1;
            XResolution = 4001;
            YBeg = 0;
            YEnd = 1;
            YResolution = 401;
        }

    }
}
