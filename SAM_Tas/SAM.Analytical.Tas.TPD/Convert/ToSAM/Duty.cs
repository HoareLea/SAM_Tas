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

            HashSet<string> names = null;

            List<DesignConditionLoad> designConditionLoads = sizedVariable.DesignConditionLoads();
             if(designConditionLoads != null && designConditionLoads.Count != 0)
            {
                names = new HashSet<string>();
                foreach(DesignConditionLoad designConditionLoad in designConditionLoads)
                {
                    names.Add(designConditionLoad.Name);
                }
            }

            switch ((tpdSizedVariable)@dynamic.Type)
            {
                case tpdSizedVariable.tpdSizedVariableSizeDone:
                    return new SizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, names);

                case tpdSizedVariable.tpdSizedVariableSize:
                    return new SizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, names);

                case tpdSizedVariable.tpdSizedVariableNone:
                    return new UnlimitedValue();

                case tpdSizedVariable.tpdSizedVariableValue:
                    return new SizableValue(System.Convert.ToDouble(@dynamic.Value));
            }

            return null;
        }
    }
}
