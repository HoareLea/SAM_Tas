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

            double designFlowRate = System.Convert.ToDouble((fanCoilUnit.DesignFlowRate as dynamic).Value);

            double heatingEfficiency = fanCoilUnit.HeatingEfficiency.Value;
            double overallEfficiency = fanCoilUnit.OverallEfficiency.Value;

            SystemFanCoilUnit result = new SystemFanCoilUnit(dynamic.Name) 
            { 
                Pressure = fanCoilUnit.Pressure,
                CoolingDuty = fanCoilUnit.CoolingDuty?.ToSAM(),
                HeatingDuty = fanCoilUnit.HeatingDuty?.ToSAM(),
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
