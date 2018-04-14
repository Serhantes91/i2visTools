using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;

namespace I2VISTools.ModelConfigClasses
{
    public class Amir
    {
        public List<OutputAmirUnit> OutputUnits { get; set; }

        public Amir()
        {

            OutputUnits = new List<OutputAmirUnit>();

            printmod = 2010;
            timedir = 1;
            movemod = 1;
            tempmod = 1;
            markmod = 1;
            ratemod = 0;
            gridmod = 0;
            intermod = 0;
            intermod1 = 0;
            outgrid = 2;
            densimod = 2;

            erosmod = 0;
            eroslev = 4000;
            eroscon = 3e-11;
            eroskoe = 0e-13;
            sedilev = 4000;
            sedicon = 1e-11;
            sedikoe = 0e-13;
            sedimcyc = 20;
            waterlev = 4000;
            slopemax = 0.30;

            divmin = 1e-04;
            stoksmin = 1e-06;
            stoksmod = 1;
            presmod = 1;
            stoksfd = 0;
            nubeg = 1e16;
            nuend = 1e25;
            nucontr = 1e18;
            hidry = 10e3;
            hidrl = 6e-1;
            strmin = 0e-10;
            strmax = 5e28;

            heatmin = 1e-8;
            heatmod = 0;
            heatfd = 0;
            heatdif = 1.0;
            frictyn = 14;
            adiabyn = 1;

        }

        public Amir(List<string> prnFiles) : this()
        {
            foreach (var file in prnFiles)
            {
                OutputUnits.Add(new OutputAmirUnit(file, 'c'));
                OutputUnits.Add(new OutputAmirUnit(file, 't'));
            }
        }

        //GENERAL_PARAMETERs
        public int printmod { get; set; }
        public int timedir { get; set; }
        public int movemod { get; set; }
        public int tempmod { get; set; }
        public int markmod { get; set; }
        public int ratemod { get; set; }
        public int gridmod { get; set; }
        public int intermod { get; set; }
        public int intermod1 { get; set; }
        public int outgrid { get; set; }
        public int densimod { get; set; }
        // /GENERAL_PARAMETERk
        //2010-printmod
        //1-timedir
        //1-movemod
        //1-tempmod
        //1-markmod
        //0-ratemod
        //0-gridmod
        //0-intermod
        //0-intermod1
        //2-outgrid 
        //2-densimod

        //EROSION SEDIMENTATION PARAMETERS
        public int erosmod { get; set; }
        public int eroslev { get; set; }
        public double eroscon { get; set; }
        public double eroskoe { get; set; }
        public int sedilev { get; set; }
        public double sedicon { get; set; }
        public double sedikoe { get; set; }
        public int sedimcyc { get; set; }
        public int waterlev { get; set; }
        public double slopemax { get; set; }
        // /ERROSION_SEDIMENTATION_PARAMETERS
        //0-erosmod
        //4000-eroslev
        //3e-11-eroscon
        //0e-13-eroskoe
        //4000-sedilev
        //1e-11-sedicon
        //0e-13-sedikoe
        //20-sedimcyc
        //4000-waterlev
        //0.30-slopemax

        // stokes continuity parameters
        public double divmin { get; set; }
        public double stoksmin { get; set; }
        public int stoksmod { get; set; }
        public int presmod { get; set; }
        public int stoksfd { get; set; }
        public double nubeg { get; set; }
        public double nuend { get; set; }
        public double nucontr { get; set; }
        public double hidry { get; set; }
        public double hidrl { get; set; }
        public double strmin { get; set; }
        public double strmax { get; set; }
        // /STOKES_CONTINUITY_PARAMETERS
        //1e-04-DIVVMIN
        //1e-06-STOKSMIN
        //1-stoksmod
        //1-presmod
        //0-stoksfd
        //1e+16-nubeg  
        //1e+25-nuend  
        //1e+18-nucontr
        //10e+3-hidry
        //6e-1-hidrl
        //0e-10-strmin
        //5e+28-strmax

        //HEAT TRANSFER PARAMETERS
        public double heatmin { get; set; }
        public int heatmod { get; set; }
        public int heatfd { get; set; }
        public double heatdif { get; set; }
        public int frictyn { get; set; }
        public int adiabyn { get; set; }
        // /HEAT_TRANSFER_PARAMETERS
        //1e-8-HEATMIN
        //0-heatmod
        //0-heatfd 
        //1.0-heatdif 
        //14-frictyn
        //1-adiabyn


        public void ExportToFile(string filePath)
        {

            if (!string.IsNullOrWhiteSpace(filePath) && !filePath.EndsWith(@"\")) filePath = filePath + @"\";

            using (StreamWriter file = new StreamWriter(filePath + "amir.t3c"))
            {
                file.WriteLine(@"/LOADFiLE_TYPE_AMIRAFILE_TYPE_XBEG_XEND_XRESOL_YBEG_YEND_YRESOL");
                foreach (var ou in OutputUnits)
                {
                    file.WriteLine("{0,-15}{1,-2}{2,-15}{3,-5}{4,-4}{5,-4}{6,-5}{7,-4}{8,-4}{9,-5}", ou.LoadFile, ou.LoadFileType, ou.AmirFile, ou.AmirFileType, ou.XBeg.ToString("0.0").Replace(",", "."), ou.XEnd.ToString("0.0").Replace(",", "."), ou.XResolution, ou.YBeg.ToString("0.0").Replace(",", "."), ou.YEnd.ToString("0.0").Replace(",", "."), ou.YResolution);
                }
                file.WriteLine("~");
                file.WriteLine();

                file.WriteLine(@"/GENERAL_PARAMETERk");
                file.WriteLine(printmod.ToString().Replace(",",".")+"-printmod");
                file.WriteLine(timedir.ToString().Replace(",", ".") + "-timedir");
                file.WriteLine(movemod.ToString().Replace(",", ".") + "-movemod");
                file.WriteLine(tempmod.ToString().Replace(",", ".") + "-tempmod");
                file.WriteLine(markmod.ToString().Replace(",", ".") + "-markmod");
                file.WriteLine(ratemod.ToString().Replace(",", ".") + "-ratemod");
                file.WriteLine(gridmod.ToString().Replace(",", ".") + "-gridmod");
                file.WriteLine(intermod.ToString().Replace(",", ".") + "-intermod");
                file.WriteLine(intermod1.ToString().Replace(",", ".") + "-intermod1");
                file.WriteLine(outgrid.ToString().Replace(",", ".") + "-outgrid");
                file.WriteLine(densimod.ToString().Replace(",", ".") + "-densimod");
                file.WriteLine();

                file.WriteLine(@"/ERROSION_SEDIMENTATION_PARAMETERS");
                file.WriteLine(erosmod.ToString().Replace(",", ".") + "-erosmod");
                file.WriteLine(eroslev.ToString().Replace(",", ".") + "-eroslev");
                file.WriteLine(eroscon.ToString("0e+00").Replace(",", ".") + "-eroscon");
                file.WriteLine(eroskoe.ToString("0e+00").Replace(",", ".") + "-eroskoe");
                file.WriteLine(sedilev.ToString().Replace(",", ".") + "-sedilev");
                file.WriteLine(sedicon.ToString("0e+00").Replace(",", ".") + "-sedicon");
                file.WriteLine(sedikoe.ToString("0e+00").Replace(",", ".") + "-sedikoe");
                file.WriteLine(sedimcyc.ToString().Replace(",", ".") + "-sedimcyc");
                file.WriteLine(waterlev.ToString().Replace(",", ".") + "-waterlev");
                file.WriteLine(slopemax.ToString("0.00").Replace(",", ".") + "-slopemax");
                file.WriteLine();

                file.WriteLine(@"/STOKES_CONTINUITY_PARAMETERS");
                file.WriteLine(divmin.ToString("0e+00").Replace(",", ".") + "-DIVVMIN");
                file.WriteLine(stoksmin.ToString("0e+00").Replace(",", ".") + "-STOKSMIN");
                file.WriteLine(stoksmod.ToString().Replace(",", ".") + "-stoksmod");
                file.WriteLine(presmod.ToString().Replace(",", ".") + "-presmod");
                file.WriteLine(stoksfd.ToString().Replace(",", ".") + "-stoksfd");
                file.WriteLine(nubeg.ToString("0e+00").Replace(",", ".") + "-nubeg");
                file.WriteLine(nuend.ToString("0e+00").Replace(",", ".") + "-nuend");
                file.WriteLine(nucontr.ToString("0e+00").Replace(",", ".") + "-nucontr");
                file.WriteLine(hidry.ToString("0e+##").Replace(",", ".") + "-hidry");
                file.WriteLine(hidrl.ToString("0e+##").Replace(",", ".") + "-hidrl");
                file.WriteLine(strmin.ToString("0e+00").Replace(",", ".") + "-strmin");
                file.WriteLine(strmax.ToString("0e+00").Replace(",", ".") + "-strmax");
                file.WriteLine();

                file.WriteLine(@"/HEAT_TRANSFER_PARAMETERS");
                file.WriteLine(heatmin.ToString("0e+00").Replace(",", ".") + "-HEATMIN");
                file.WriteLine(heatmod.ToString().Replace(",", ".") + "-heatmod");
                file.WriteLine(heatfd.ToString().Replace(",", ".") + "-heatfd");
                file.WriteLine(heatdif.ToString("0.0").Replace(",", ".") + "-heatdif");
                file.WriteLine(frictyn + "-frictyn");
                file.WriteLine(adiabyn+"-adiabyn");
                file.WriteLine();
                file.WriteLine();
            }
        }

        public void ImportFromFile(string filePath)
        {
            OutputUnits = new List<OutputAmirUnit>();
            using (var file = new StreamReader(filePath))
            {

                string amirOutString = "";
                while (!amirOutString.StartsWith("~") && amirOutString != null)
                {
                    amirOutString = file.ReadLine();
                    var amirOutSubStrings = amirOutString.Split(new[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (amirOutSubStrings.Length < 10)
                    {
                        continue;
                    }
                    var amirOut = new OutputAmirUnit
                    {
                        LoadFile = amirOutSubStrings[0],
                        LoadFileType = amirOutSubStrings[1][0],
                        AmirFile = amirOutSubStrings[2],
                        AmirFileType = amirOutSubStrings[3][0],
                        XBeg = Config.Tools.ParseOrDefaultDouble(amirOutSubStrings[4]),
                        XEnd = Config.Tools.ParseOrDefaultDouble(amirOutSubStrings[5]),
                        XResolution = Config.Tools.ParseOrDefaultInt(amirOutSubStrings[6]),
                        YBeg = Config.Tools.ParseOrDefaultDouble(amirOutSubStrings[7]),
                        YEnd = Config.Tools.ParseOrDefaultDouble(amirOutSubStrings[8]),
                        YResolution = Config.Tools.ParseOrDefaultInt(amirOutSubStrings[9])
                    };
                    OutputUnits.Add(amirOut);
                    
                }

                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("/")) continue;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parPosition = line.LastIndexOf("-", StringComparison.Ordinal);
                    if (parPosition == -1) continue;
                    var parString = line.Substring(parPosition);
                    var valString = line.Substring(0, line.Length - parString.Length);

                    switch (parString)
                    {
                        case "-printmod":
                            printmod = Config.Tools.ParseOrDefaultInt(valString);break;
                        case "-timedir": timedir = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-movemod": movemod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-tempmod": tempmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-markmod": markmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-ratemod": ratemod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-gridmod": gridmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-intermod": intermod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-intermod1": intermod1 = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-outgrid": outgrid = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-densimod": densimod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-erosmod": erosmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-eroslev": eroslev = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-eroscon": eroscon = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-eroskoe": eroskoe = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-sedilev": sedilev = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-sedicon": sedicon = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-sedikoe": sedikoe = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-sedimcyc": sedimcyc = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-waterlev": waterlev = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-slopemax": slopemax = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-DIVVMIN": divmin = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-STOKSMIN": stoksmin = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-stoksmod": stoksmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-presmod": presmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-stoksfd": stoksfd = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-nubeg": nubeg = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-nuend": nubeg = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-nucontr": nucontr = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-hidry": hidry = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-hidrl": hidrl = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-strmin": strmin = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-strmax": strmax = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-HEATMIN": heatmin = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-heatmod": heatmod = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-heatfd": heatfd = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-heatdif": heatdif = Config.Tools.ParseOrDefaultDouble(valString); break;
                        case "-frictyn": frictyn = Config.Tools.ParseOrDefaultInt(valString); break;
                        case "-adiabyn": adiabyn = Config.Tools.ParseOrDefaultInt(valString); break;
                        default:
                            continue;
                            
                    }

                }

            }
        }
    
    }
}
