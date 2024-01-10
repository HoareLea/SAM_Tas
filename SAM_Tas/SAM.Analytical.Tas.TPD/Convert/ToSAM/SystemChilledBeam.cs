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

            double coolingDuty = System.Convert.ToDouble((chilledBeam.CoolingDuty as dynamic).Value);
            double heatingDuty = System.Convert.ToDouble((chilledBeam.HeatingDuty as dynamic).Value);
            double designFlowRate = System.Convert.ToDouble((chilledBeam.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = chilledBeam.HeatingEfficiency.Value;

            SystemChilledBeam result = new SystemChilledBeam(@dynamic.Name) 
            {
                CoolingDuty = coolingDuty,
                HeatingDuty = heatingDuty,
                DesignFlowRate = designFlowRate,
                HeatingEfficiency = heatingEfficiency,
            };

            result.Description = dynamic.Description;
            result.SetReference(((ZoneComponent)chilledBeam).Reference());

            return result;
        }
    }
}
