using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;

namespace I2VISTools.ModelClasses
{
    public class Marker
    {
        public uint Id { get; set; }
        public byte RockId { get; set; }
        public float XPosition { get; set; }
        public float YPosition { get; set; }
        public float Temperature { get; set; }
        public float Density { get; set; }
        public float WaterCons { get; set; }
        public float UndefinedPar1 { get; set; }
        public float UndefinedPar2 { get; set; }
        public float Viscosity { get; set; }
        public float Deformation { get; set; }
        public double Pressure { get; set; }
        public double Age { get; set; }
        public string PrnSource { get; set; }

        //1 - x-компонента (4 байта)
        //2 - y-компонента (4 байта)
        //3 - температура (4 байта)
        //4 - плотность (4 байта)
        //5 - содержание воды (4 байта)
        //6 - хз (4 байта)
        //7 - хз (4 байта)
        //8 - скорее всего - вязкость (4 байта)
        //9 - скорее всего - относительная деформация (4 байта)
        //10 - тип породы (1 байт)
    }
}
