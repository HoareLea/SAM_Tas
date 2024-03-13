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

            SystemDXCoilUnit result = new SystemDXCoilUnit(dynamic.Name) 
            {
                CoolingDuty = dXCoilUnit.CoolingDuty?.ToSAM_Duty(),
                HeatingDuty = dXCoilUnit.HeatingDuty?.ToSAM_Duty(),
                DesignFlowRate = designFlowRate,
                OverallEfficiency = overallEfficiency
            };

            result.Description = dynamic.Description;
            result.SetReference(((ZoneComponent)dXCoilUnit).Reference());

            return result;
        }
    }
}
