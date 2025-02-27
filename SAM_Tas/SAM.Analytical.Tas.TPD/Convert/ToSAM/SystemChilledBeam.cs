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
                ZonePosition = chilledBeam.ZonePosition.ToSAM()
            };

            result.SetReference(((ZoneComponent)chilledBeam).Reference());
            
            result.Description = dynamic.Description;

            return result;
        }
    }
}
