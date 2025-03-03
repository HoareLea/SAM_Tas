using TPD;
using SAM.Analytical.Systems;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DesignConditionSizedFlowValue ToSAM(this SizedFlowVariable sizedFlowVariable)
        {
            if (sizedFlowVariable == null)
            {
                return null;
            }

            double value = sizedFlowVariable.Value;
            double sizeFraction = sizedFlowVariable.SizeFraction;
            SizingType sizingType = sizedFlowVariable.Type.ToSAM();
            double sizeValue1 = sizedFlowVariable.SizeValue1;
            double sizeValue2 = sizedFlowVariable.SizeValue2;
            SizedFlowMethod sizedFlowMethod = sizedFlowVariable.Method.ToSAM();

            HashSet<string> designConditionNames = null;

            List<DesignConditionLoad> designConditionLoads = sizedFlowVariable.DesignConditionLoads();
            if (designConditionLoads != null && designConditionLoads.Count != 0)
            {
                designConditionNames = new HashSet<string>();
                foreach (DesignConditionLoad designConditionLoad in designConditionLoads)
                {
                    designConditionNames.Add(designConditionLoad.Name);
                }
            }

            DesignConditionSizedFlowValue result = new DesignConditionSizedFlowValue(value, sizeFraction, sizingType, sizeValue1, sizeValue2, sizedFlowMethod, designConditionNames);

            return result;
        }
    }
}
