using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;

namespace I2VISTools.ResultClasses
{
    public class DiaryNote
    {

        public string Id { get; set; }

        public string ModelName { get; set; }
        public string Size { get; set; }
        public string Resolution { get; set; }
        public string Hl { get; set; }
        public string Hc { get; set; }
        public string Ho { get; set; }
        public string V { get; set; }
        public string Vmps { get; set; } //TODO сделать зависимым от V
        public string Tpush { get; set; }
        public string Alpha { get; set; }
        public string OceanWidth { get; set; }
        public string PassiveMargin { get; set; }
        public string ActiveMArgin { get; set; }
        public string GeoTherm { get; set; }
        public string UnderLithoshereT { get; set; }
        public string Adiabate { get; set; }
        public string OceanicAge { get; set; }
        public string Hr { get; set; }
        public string tdeep { get; set; }
        public string zdeep { get; set; }
        public int TaskId { get; set; }
        public DateTime StartTime { get; set; }
        public int StepsNum { get; set; }
        public string Duration { get; set; }
        public string StopReason { get; set; }

        public string AdditionalInfo { get; set; }
        public string Comment { get; set; }
    }
}
