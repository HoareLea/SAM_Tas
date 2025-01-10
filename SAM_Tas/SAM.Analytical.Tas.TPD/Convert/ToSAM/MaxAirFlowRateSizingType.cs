using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static MaxAirFlowRateSizingType ToSAM_MaxAirFlowRateSizingType(this global::TPD.tpdSizedVariable tpdSizedVariable)
        {
            switch(tpdSizedVariable)
            {
                case global::TPD.tpdSizedVariable.tpdSizedVariableSize:
                    return MaxAirFlowRateSizingType.FanLoadRatio;

                case global::TPD.tpdSizedVariable.tpdSizedVariableSizeDone:
                    return MaxAirFlowRateSizingType.FanLoadRatio;

                case global::TPD.tpdSizedVariable.tpdSizedVariableValue:
                    return MaxAirFlowRateSizingType.Value;

                case global::TPD.tpdSizedVariable.tpdSizedVariableNone:
                    return MaxAirFlowRateSizingType.AirFlowWaterFlowRatio;
            }

            throw new System.NotImplementedException();
        }
    }
}
