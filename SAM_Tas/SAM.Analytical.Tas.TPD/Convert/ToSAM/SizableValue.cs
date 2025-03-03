using SAM.Analytical.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ISizableValue ToSAM(this SizedVariable sizedVariable)
        {
            if (sizedVariable == null)
            {
                return null;
            }

            dynamic @dynamic = sizedVariable as dynamic;

            tpdSizedVariable tpdSizedVariable = (tpdSizedVariable)@dynamic.Type;
            if (tpdSizedVariable == tpdSizedVariable.tpdSizedVariableNone)
            {
                return new UnlimitedValue();
            }

            HashSet<string> designConditionNames = null;

            List<DesignConditionLoad> designConditionLoads = sizedVariable.DesignConditionLoads();
            if (designConditionLoads != null && designConditionLoads.Count != 0)
            {
                designConditionNames = new HashSet<string>();
                foreach (DesignConditionLoad designConditionLoad in designConditionLoads)
                {
                    designConditionNames.Add(designConditionLoad.Name);
                }
            }

            SizeMethod sizeMethod = sizedVariable.Method.ToSAM();

            switch (tpdSizedVariable)
            {
                case tpdSizedVariable.tpdSizedVariableSizeDone:
                    return new DesignConditionSizableValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, designConditionNames) { SizeMethod = sizeMethod, SizingType = SizingType.Sized };

                case tpdSizedVariable.tpdSizedVariableSize:
                    return new DesignConditionSizableValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction, designConditionNames) { SizeMethod = sizeMethod, SizingType = SizingType.Sized };

                case tpdSizedVariable.tpdSizedVariableValue:
                    return new SizableValue(System.Convert.ToDouble(@dynamic.Value)) { SizeMethod = sizeMethod, SizingType = SizingType.Value };
            }

            return null;
        }
    }
}
