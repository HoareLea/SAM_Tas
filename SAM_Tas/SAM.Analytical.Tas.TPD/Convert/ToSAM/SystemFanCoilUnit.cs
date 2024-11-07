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

            SystemFanCoilUnit result = new SystemFanCoilUnit(dynamic.Name);
            result.SetReference(((ZoneComponent)fanCoilUnit).Reference());

            result.Description = dynamic.Description;
            result.Pressure = fanCoilUnit.Pressure;
            result.CoolingDuty = fanCoilUnit.CoolingDuty?.ToSAM();
            result.HeatingDuty = fanCoilUnit.HeatingDuty?.ToSAM();
            result.DesignFlowRate = designFlowRate;
            result.HeatingEfficiency = heatingEfficiency;
            result.OverallEfficiency = overallEfficiency;

            return result;
        }
    }
}
