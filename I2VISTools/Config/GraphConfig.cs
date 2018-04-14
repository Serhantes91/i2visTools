using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using I2VISTools.Subclasses;
using OxyPlot;

namespace I2VISTools.Config
{
    public class GraphConfig
    {
        //делаем класс одиночным (singleton)
        private static GraphConfig _instance = new GraphConfig();

        /// <summary>
        /// Свойство возвращает единственно возможный экземпляр класса настроек приложения
        /// </summary>
        public static GraphConfig Instace
        {
            get { return _instance; }
        }

        public int XSize { get; set; }
        public int YSize { get; set; }

        public int XBegin { get; set; }
        public int XEnd { get; set; }
        public int YBegin { get; set; }
        public int YEnd { get; set; }

        public List<double> Isoterms { get; set; } 

        public List<string> PrnParameters = new List<string>() { "pr","vx","vy","exx","eyy","exy","sxx","syy","sxy","ro", "nu","nd", "mu", "ep", "et", "pr0", "prb", "dv","tk", "cp", "kt", "ht", "txt" };
        public List<PrnParameterRange> PrnValuesRanges = new List<PrnParameterRange>(); 

        public List<RockColor> RocksColor = new List<RockColor>()
                {
		        	new RockColor { Index = 0, Color = OxyColor.FromRgb(255, 255, 255)}, //0
		        	new RockColor { Index = 1, Color = OxyColor.FromRgb(129, 254, 201)}, //1
		        	new RockColor { Index = 2, Color = OxyColor.FromRgb(255, 128, 0)}, //2
		        	new RockColor { Index = 3, Color = OxyColor.FromRgb(174, 87, 0)}, //3
		        	new RockColor { Index = 4, Color = OxyColor.FromRgb(255, 128, 0)}, //4
		        	new RockColor { Index = 5, Color = OxyColor.FromRgb(192, 192, 192)}, //5
		        	new RockColor { Index = 6, Color = OxyColor.FromRgb(128, 128, 128)}, //6
		        	new RockColor { Index = 7, Color = OxyColor.FromRgb(0, 128, 0)}, //7
		        	new RockColor { Index = 8, Color = OxyColor.FromRgb(0, 215, 0)}, //8
		        	new RockColor { Index = 9, Color = OxyColor.FromRgb(0, 0, 183)}, //9
		        	new RockColor { Index = 10, Color = OxyColor.FromRgb(82, 51, 172)}, //10
		        	new RockColor { Index = 11, Color = OxyColor.FromRgb(138, 184, 253)}, //11
		        	new RockColor { Index = 12, Color = OxyColor.FromRgb(0, 127, 255)}, //12
		        	new RockColor { Index = 13, Color = OxyColor.FromRgb(0, 0, 125)}, //13
		        	new RockColor { Index = 14, Color = OxyColor.FromRgb(25, 25, 25)}, //14
		        	new RockColor { Index = 15, Color = OxyColor.FromRgb(220, 200, 0)}, //15
		        	new RockColor { Index = 16, Color = OxyColor.FromRgb(141, 52, 22)}, //16
		        	new RockColor { Index = 17, Color = OxyColor.FromRgb(90, 43, 7)}, //17
		        	new RockColor { Index = 18, Color = OxyColor.FromRgb(1,	0, 0)}, //18
		        	new RockColor { Index = 19, Color = OxyColor.FromRgb(0, 1, 0)}, //19
		        	new RockColor { Index = 20, Color = OxyColor.FromRgb(0,	0, 1)}, //20
		        	new RockColor { Index = 21, Color = OxyColor.FromRgb(1, 1, 0)}, //21
		        	new RockColor { Index = 22, Color = OxyColor.FromRgb(0,	0, 0)}, //22
		        	new RockColor { Index = 23, Color = OxyColor.FromRgb(255, 255, 81)}, //23
		        	new RockColor { Index = 24, Color = OxyColor.FromRgb(255, 230, 48)}, //24
		        	new RockColor { Index = 25, Color = OxyColor.FromRgb(119, 119, 60)}, //25
		        	new RockColor { Index = 26, Color = OxyColor.FromRgb(128, 128, 0)}, //26
		        	new RockColor { Index = 27, Color = OxyColor.FromRgb(185, 4, 200)}, //27
		        	new RockColor { Index = 28, Color = OxyColor.FromRgb(236, 112, 254)}, //28
		        	new RockColor { Index = 29, Color = OxyColor.FromRgb(255, 127, 127)}, //29
		        	new RockColor { Index = 30, Color = OxyColor.FromRgb(255, 153, 153)}, //30
		        	new RockColor { Index = 31, Color = OxyColor.FromRgb(253, 99, 77)}, //31
		        	new RockColor { Index = 32, Color = OxyColor.FromRgb(216, 20, 39)}, //32
		        	new RockColor { Index = 33, Color = OxyColor.FromRgb(1, 1, 1) }, // 33
		        	new RockColor { Index = 34, Color = OxyColor.FromRgb(255, 0, 0) }, //34
		        	new RockColor { Index = 35, Color = OxyColor.FromRgb(218, 152, 92)} //35
		        };

        public List<OxyColor> ColorMap
        {
            get
            {
                return RocksColor.Select(x => x.Color).ToList();
            }
        }

        public void LoadFromFile()
        {
            try
            {
                if (!File.Exists("picConfig")) return;

                var rcls = new List<RockColor>();
                var it = new List<double>();

                using (var file = new StreamReader("picConfig"))
                {
                    string line = file.ReadLine();

                    while (line != "~")
                    {
                        if (line == null) return;
                        var sublines = line.Split(new[] {'\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
                        if (sublines.Count() < 5) continue;

                        var rc = new RockColor()
                        {
                            Index = Convert.ToInt32(sublines[0]),
                            Color =
                                OxyColor.FromArgb(Convert.ToByte(sublines[1]), Convert.ToByte(sublines[2]),
                                    Convert.ToByte(sublines[3]), Convert.ToByte(sublines[4]))
                        };
                        rcls.Add(rc);

                        line = file.ReadLine();
                    }

                    line = file.ReadLine();
                    while (line != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        it.Add(Convert.ToDouble(line));
                        line = file.ReadLine();
                    }
                }

                RocksColor = rcls;
                Isoterms = it;
            }
            catch (Exception ex)
            {
                return;
            }
            

        }

        //public List<int[]> ColorMap = new List<int[]>() {
        //            new[]{255, 255, 255}, //0
        //            new[]{129, 254, 201}, //1
        //            new[]{255, 128, 0}, //2
        //            new[]{174, 87, 0}, //3
        //            new[]{255, 128, 0}, //4
        //            new[]{192, 192, 192}, //5
        //            new[]{128, 128, 128}, //6
        //            new[]{0, 128, 0}, //7
        //            new[]{0, 215, 0}, //8
        //            new[]{0, 0, 183}, //9
        //            new[]{82, 51, 172}, //10
        //            new[]{138, 184, 253}, //11
        //            new[]{0, 127, 255}, //12
        //            new[]{0, 0, 125}, //13
        //            new[]{25, 25, 25}, //14
        //            new[]{220, 200, 0}, //15
        //            new[]{141, 52, 22}, //16
        //            new[]{90, 43, 7}, //17
        //            new[]{0,	0, 0}, //18
        //            new[]{0, 0, 0}, //19
        //            new[]{0,	0, 0}, //20
        //            new[]{0, 0, 0}, //21
        //            new[]{0,	0, 0}, //22
        //            new[]{255, 255, 81}, //23
        //            new[]{255, 230, 48}, //24
        //            new[]{119, 119, 60}, //25
        //            new[]{128, 128, 0}, //26
        //            new[]{185, 4, 200}, //27
        //            new[]{236, 112, 254}, //28
        //            new[]{255, 127, 127}, //29
        //            new[]{255, 153, 153}, //30
        //            new[]{253, 99, 77}, //31
        //            new[]{216, 20, 39}, //32
        //            new[]{0, 0, 0}, // 33
        //            new[]{255, 0, 0}, //34
        //            new[]{218, 152, 92} //35
        //        };

    }
}
