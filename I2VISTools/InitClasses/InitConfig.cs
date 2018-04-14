using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace I2VISTools.InitClasses
{
    public class InitConfig
    {
        public CalcGrid Grid { get; set; }

        public int MarkerTypesFileName { get; set; }
        public string OutputPrnFile { get; set; }

        public char OutputType { get; set; }

        public List<RockDescription> Rocks { get; set; }
        public List<RockBox> RockBoxes { get; set; }
        public List<Geotherm> Geotherms { get; set; }

        public List<String> kostyl = new List<string>();

        public InitConfig()
        {
            Grid = new CalcGrid();
            Rocks = new List<RockDescription>();
            RockBoxes = new List<RockBox>();
            Geotherms = new List<Geotherm>();
        }

        public double GetValueFromString(string line, bool XAxis = true)
        {
            if (CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                line = line.Replace(".", ",");
            }
            if (line.StartsWith("m"))
            {
                return Config.Tools.ParseOrDefaultDouble(line.Remove(0, 1));
            }
            else
            {
                if (XAxis)
                {
                    return Config.Tools.ParseOrDefaultDouble(line)*Grid.Xsize;
                }
                else
                {
                    return Config.Tools.ParseOrDefaultDouble(line) * Grid.Ysize;
                }
            }
        }
 
    }
}
