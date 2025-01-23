using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdFlowRateType ToTPD(this FlowRateType flowRateType)
        {
            switch (flowRateType)
            {
                case FlowRateType.None:
                    return tpdFlowRateType.tpdFlowRateNone;

                case FlowRateType.NearestZoneFlowRate:
                    return tpdFlowRateType.tpdFlowRateNearestZoneFlowRate;

                case FlowRateType.AllAttachedZonesFlowRate:
                    return tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

                case FlowRateType.AllAttachedZonesSized:
                    return tpdFlowRateType.tpdFlowRateAllAttachedZonesSized;

                case FlowRateType.AllAttachedZonesFreshAir:
                    return tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;

                case FlowRateType.NearestZoneFreshAir:
                    return tpdFlowRateType.tpdFlowRateNearestZoneFreshAir;

                case FlowRateType.Value:
                    return tpdFlowRateType.tpdFlowRateValue;

                case FlowRateType.Sized:
                    return tpdFlowRateType.tpdFlowRateSized;
            }

            throw new System.NotImplementedException();
        }
    }
}
