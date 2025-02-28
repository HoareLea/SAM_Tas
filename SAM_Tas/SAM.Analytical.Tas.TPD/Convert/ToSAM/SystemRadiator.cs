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

            dynamic @dynamic = radiator;

            double duty = System.Convert.ToDouble((radiator.Duty as dynamic).Value);
            double efficiency = System.Convert.ToDouble((radiator.Efficiency as dynamic).Value);

            SystemRadiator result = new SystemRadiator(dynamic.Name);
            result.SetReference(((ZoneComponent)radiator).Reference());
            result.Duty = radiator.Duty.ToSAM();
            result.Efficiency = efficiency;
            result.Description = dynamic.Description;

            result.ScheduleName = @dynamic.GetSchedule()?.Name;

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            if (heatingGroup != null)
            {
                result.SetValue(SystemRadiatorParameter.HeatingCollection, new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name));
            }

            return result;
        }
    }
}
