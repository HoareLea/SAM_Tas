using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFanCoilUnit ToSAM(this FanCoilUnit fanCoilUnit)
        {
            if (fanCoilUnit == null)
            {
                return null;
            }

            dynamic @dynamic = fanCoilUnit;

            double coolingDuty = System.Convert.ToDouble((fanCoilUnit.CoolingDuty as dynamic).Value);
            double heatingDuty = System.Convert.ToDouble((fanCoilUnit.HeatingDuty as dynamic).Value);
            double designFlowRate = System.Convert.ToDouble((fanCoilUnit.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = fanCoilUnit.HeatingEfficiency.Value;
            double overallEfficiency = fanCoilUnit.OverallEfficiency.Value;

            SystemFanCoilUnit result = new SystemFanCoilUnit(dynamic.Name) 
            { 
                Pressure = fanCoilUnit.Pressure,
                CoolingDuty = coolingDuty,
                HeatingDuty = heatingDuty,
                DesignFlowRate = designFlowRate,
                HeatingEfficiency = heatingEfficiency,
                OverallEfficiency = overallEfficiency
            };

            result.Description = dynamic.Description;
            result.SetReference(((ZoneComponent)fanCoilUnit).Reference());

            return result;
        }
    }
}
