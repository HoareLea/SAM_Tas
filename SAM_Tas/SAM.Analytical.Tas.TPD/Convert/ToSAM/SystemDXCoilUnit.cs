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
            result.CoolingDuty = dXCoilUnit.CoolingDuty?.ToSAM();
            result.HeatingDuty = dXCoilUnit.HeatingDuty?.ToSAM();
            result.DesignFlowRate = designFlowRate;
            result.OverallEfficiency = overallEfficiency;

            return result;
        }
    }
}
