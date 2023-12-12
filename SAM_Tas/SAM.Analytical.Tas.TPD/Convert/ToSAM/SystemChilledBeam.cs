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

            double coolingDuty = System.Convert.ToDouble((chilledBeam.CoolingDuty as dynamic).Value);
            double heatingDuty = System.Convert.ToDouble((chilledBeam.HeatingDuty as dynamic).Value);
            double designFlowRate = System.Convert.ToDouble((chilledBeam.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = chilledBeam.HeatingEfficiency.Value;

            SystemChilledBeam result = new SystemChilledBeam((chilledBeam as dynamic).Name) 
            {
                CoolingDuty = coolingDuty,
                HeatingDuty = heatingDuty,
                DesignFlowRate = designFlowRate,
                HeatingEfficiency = heatingEfficiency,
            };
            
            result.SetReference(((ZoneComponent)chilledBeam).Reference());

            return result;
        }
    }
}
