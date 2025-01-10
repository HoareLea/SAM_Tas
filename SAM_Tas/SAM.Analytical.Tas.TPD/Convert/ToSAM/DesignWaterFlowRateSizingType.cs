using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DesignWaterFlowRateSizingType ToSAM_DesignWaterFlowRateSizingType(this global::TPD.tpdSizedVariable tpdSizedVariable)
        {
            switch(tpdSizedVariable)
            {
                case global::TPD.tpdSizedVariable.tpdSizedVariableSize:
                    return DesignWaterFlowRateSizingType.AsPumpDesignFlowRate;

                case global::TPD.tpdSizedVariable.tpdSizedVariableSizeDone:
                    return DesignWaterFlowRateSizingType.AsPumpDesignFlowRate;

                case global::TPD.tpdSizedVariable.tpdSizedVariableValue:
                    return DesignWaterFlowRateSizingType.Value;
            }

            throw new System.NotImplementedException();
        }
    }
}
