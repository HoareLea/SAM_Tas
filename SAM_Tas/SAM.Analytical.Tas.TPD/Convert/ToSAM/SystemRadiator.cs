﻿using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemRadiator ToSAM(this Radiator radiator)
        {
            if (radiator == null)
            {
                return null;
            }

            dynamic @dynamic = radiator as dynamic;

            double duty = System.Convert.ToDouble((radiator.Duty as dynamic).Value);
            double efficiency = System.Convert.ToDouble((radiator.Efficiency as dynamic).Value);

            SystemRadiator result = new SystemRadiator(dynamic.Name);
            result.SetReference(((ZoneComponent)radiator).Reference());
            result.Duty = radiator.Duty.ToSAM();
            result.Efficiency = efficiency;
            result.Description = dynamic.Description;

            return result;
        }
    }
}
