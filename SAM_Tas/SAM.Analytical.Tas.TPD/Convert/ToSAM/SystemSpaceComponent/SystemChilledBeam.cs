using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChilledBeam ToSAM(this ChilledBeam chilledBeam)
        {
            if (chilledBeam == null)
            {
                return null;
            }

            dynamic @dynamic = chilledBeam;

            double designFlowRate = System.Convert.ToDouble((chilledBeam.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = chilledBeam.HeatingEfficiency.Value;

            SystemChilledBeam result = new SystemChilledBeam(@dynamic.Name)
            {
                HeatingDuty = chilledBeam.HeatingDuty?.ToSAM(),
                CoolingDuty = chilledBeam.CoolingDuty?.ToSAM(),
                BypassFactor = chilledBeam.BypassFactor?.ToSAM(),
                HeatingEfficiency = chilledBeam.HeatingEfficiency?.ToSAM(),
                DesignFlowRate = chilledBeam.DesignFlowRate?.ToSAM(),
                DesignFlowType = chilledBeam.DesignFlowType.ToSAM(),
                ZonePosition = chilledBeam.ZonePosition.ToSAM(),
                ScheduleName = @dynamic.GetSchedule()?.Name,
                Heating = (int)@dynamic.Flags == 1
            };

            CoolingGroup coolingGroup = @dynamic.GetCoolingGroup();
            if (coolingGroup != null)
            {
                result.SetValue(SystemChilledBeamParameter.CoolingCollection, new CollectionLink(CollectionType.Cooling, ((dynamic)coolingGroup).Name));
            }

            HeatingGroup heatingGroup = @dynamic.GetHeatingGroup();
            if (heatingGroup != null)
            {
                result.SetValue(SystemChilledBeamParameter.HeatingCollection, new CollectionLink(CollectionType.Heating, ((dynamic)heatingGroup).Name));
            }

            result.SetReference(((ZoneComponent)chilledBeam).Reference());
            
            result.Description = dynamic.Description;

            return result;
        }
    }
}
