using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoilUnit ToSAM(this DXCoilUnit dXCoilUnit)
        {
            if (dXCoilUnit == null)
            {
                return null;
            }

            dynamic @dynamic = dXCoilUnit as dynamic;

            double designFlowRate = System.Convert.ToDouble((dXCoilUnit.DesignFlowRate as dynamic).Value);

            double overallEfficiency = dXCoilUnit.OverallEfficiency.Value;

            SystemDXCoilUnit result = new SystemDXCoilUnit(dynamic.Name);
            result.SetReference(((ZoneComponent)dXCoilUnit).Reference());

            result.Description = dynamic.Description;

            result.HeatingDuty = dXCoilUnit.HeatingDuty?.ToSAM();
            result.CoolingDuty = dXCoilUnit.CoolingDuty?.ToSAM();
            result.BypassFactor = dXCoilUnit.BypassFactor?.ToSAM();
            result.OverallEfficiency = dXCoilUnit.OverallEfficiency?.ToSAM();
            result.HeatGainFactor = dXCoilUnit.HeatGainFactor;
            result.Pressure = dXCoilUnit.Pressure;
            result.DesignFlowRate = dXCoilUnit.DesignFlowRate?.ToSAM();
            result.DesignFlowType = dXCoilUnit.DesignFlowType.ToSAM();
            result.MinimumFlowRate = dXCoilUnit.MinimumFlowRate?.ToSAM();
            result.MinimumFlowType = dXCoilUnit.MinimumFlowType.ToSAM();
            result.ZonePosition = dXCoilUnit.ZonePosition.ToSAM();
            result.ControlMethod = dXCoilUnit.ControlMethod.ToSAM();
            result.PartLoad = dXCoilUnit.PartLoad?.ToSAM();

            return result;
        }
    }
}
