using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using I2VISTools.Subclasses;

namespace I2VISTools.InitClasses
{
    public interface IBox
    {
        ModPoint Apex0 { get; set; }
        ModPoint Apex1 { get; set; }
        ModPoint Apex2 { get; set; }
        ModPoint Apex3 { get; set; }

        bool FreezeLogging { get; set; }
        bool MultipleLogging { get; set; }
    }
}
