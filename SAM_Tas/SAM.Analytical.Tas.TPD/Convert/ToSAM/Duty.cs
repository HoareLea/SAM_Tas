using SAM.Analytical.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizableValue ToSAM(this SizedVariable sizedVariable)
        {
            if(sizedVariable == null)
            {
                return null;
            }

            dynamic @dynamic = sizedVariable as dynamic;

            HashSet<string> designConditionNames = null;

            List<DesignConditionLoad> designConditionLoads = sizedVariable.DesignConditionLoads();
             if(designConditionLoads != null && designConditionLoads.Count != 0)
            {
                designConditionNames = new HashSet<string>();
                foreach(DesignConditionLoad designConditionLoad in designConditionLoads)
                {
                    designConditionNames.Add(designConditionLoad.Name);
                }
            }

            switch ((tpdSizedVariable)@dynamic.Type)
            {
                case tpdSizedVariable.tpdSizedVariableSizeDone:
                    return new DesignConditionSizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, designConditionNames);

                case tpdSizedVariable.tpdSizedVariableSize:
                    return new DesignConditionSizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, designConditionNames);

                case tpdSizedVariable.tpdSizedVariableNone:
                    return new UnlimitedValue();

                case tpdSizedVariable.tpdSizedVariableValue:
                    return new SizableValue(System.Convert.ToDouble(@dynamic.Value));
            }

            return null;
        }
    }
}
