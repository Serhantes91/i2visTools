using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using I2VISTools.Subclasses;

namespace I2VISTools.Config
{
    public class GeologyConfig
    {
        public GeologyConfig()
        {
            MetamorphicFacies = new List<MetamorphicFacie>();
            Phases = new List<GeoPhase>();
        }

        //делаем класс одиночным (singleton)
        private static GeologyConfig _instance = new GeologyConfig();

        /// <summary>
        /// Свойство возвращает единственно возможный экземпляр класса настроек приложения
        /// </summary>
        public static GeologyConfig Instace
        {
            get { return _instance; }
        }

        /// <summary>
        /// Фации метаморфизма
        /// </summary>
        public List<MetamorphicFacie> MetamorphicFacies { get; set; }
        /// <summary>
        /// Фазы
        /// </summary>
        public List<GeoPhase> Phases { get; set; }

        /// <summary>
        /// Загрузка фаций из файла facies
        /// </summary>
        public void LoadFacies()
        {
            try
            {
                using (var sr = new System.IO.StreamReader("facies"))
                {
                    MetamorphicFacie curFacie = new MetamorphicFacie();
                    var str = "";

                    while ((str = sr.ReadLine()) != null)
                    {
                        if (str.StartsWith("#")) continue;
                        if (string.IsNullOrWhiteSpace(str)) continue;

                        if (str.StartsWith("~"))
                        {
                            var substrs = str.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);

                            curFacie = new MetamorphicFacie { Name = substrs[0].Substring(1) };
                            MetamorphicFacies.Add(curFacie);

                            //если есть цвет - присваиваем его свойству Color
                            if (substrs.Length > 1)
                            {
                                var colString = substrs[1].Replace(@"(", "").Replace(@")", "").Replace(" ", "");
                                var colComps = colString.Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
                                if (colComps.Length < 3) continue;
                                
                                var red = Tools.ParseOrDefaultInt(colComps[0], -1);
                                var green = Tools.ParseOrDefaultInt(colComps[1], -1);
                                var blue = Tools.ParseOrDefaultInt(colComps[2], -1);
                                if (red < 0 || green < 0 || blue < 0) continue;
                                curFacie.Color = Color.FromArgb(122, red, green, blue);
                            }

                            continue;
                        }

                        var substrings = str.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var curP = Tools.ParseOrDefaultDouble(substrings[1]);
                        var curT = Tools.ParseOrDefaultDouble(substrings[0]);

                        curFacie.Points.Add(new PTPoint(curP * 1e9, curT));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            
        }
        /// <summary>
        /// Загрузка фаз из файла phases
        /// </summary>
        public void LoadPhases()
        {
            try
            {
                using (var sr = new System.IO.StreamReader("phases"))
                {
                    GeoPhase curPhase = new GeoPhase();
                    var str = "";

                    while ((str = sr.ReadLine()) != null)
                    {
                        if (str.StartsWith("#")) continue;
                        if (string.IsNullOrWhiteSpace(str)) continue;

                        if (str.StartsWith("~"))
                        {
                            var substrs = str.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                            curPhase = new GeoPhase() { Name = substrs[0].Substring(1) };
                            Phases.Add(curPhase);

                            //если есть цвет - присваиваем его свойству Color
                            if (substrs.Length > 1)
                            {
                                var colString = substrs[1].Replace(@"(", "").Replace(@")", "").Replace(" ", "");
                                var colComps = colString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                if (colComps.Length < 3) continue;

                                var red = Tools.ParseOrDefaultInt(colComps[0], -1);
                                var green = Tools.ParseOrDefaultInt(colComps[1], -1);
                                var blue = Tools.ParseOrDefaultInt(colComps[2], -1);
                                if (red < 0 || green < 0 || blue < 0) continue;
                                curPhase.Color = Color.FromArgb(255, red, green, blue);
                            }

                            continue;
                        }

                        var substrings = str.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var curP = Tools.ParseOrDefaultDouble(substrings[1]);
                        var curT = Tools.ParseOrDefaultDouble(substrings[0]);

                        curPhase.Points.Add(new PTPoint(curP * 1e9, curT));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        
        public MetamorphicFacie GetFacie(PTPoint point)
        {
            return MetamorphicFacies.FirstOrDefault(x => x.IsPointWithin(point));
        }

    }
}
