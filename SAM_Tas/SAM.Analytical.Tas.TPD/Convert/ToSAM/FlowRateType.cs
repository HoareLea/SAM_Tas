using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FlowRateType ToSAM(this tpdFlowRateType tpdFlowRateType)
        {
            switch(tpdFlowRateType)
            {
                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate:
                    return FlowRateType.AllAttachedZonesFlowRate;

                case tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir:
                    return FlowRateType.AllAttachedZonesFreshAir;

                case tpdFlowRateType.tpdFlowRateAllAttachedZonesSized:
                    return FlowRateType.Sized;

                case tpdFlowRateType.tpdFlowRateNearestZoneFlowRate:
                    return FlowRateType.NearestZoneFlowRate;

                case tpdFlowRateType.tpdFlowRateNone:
                    return FlowRateType.None;

                case tpdFlowRateType.tpdFlowRateSized:
                    return FlowRateType.Sized;

                case tpdFlowRateType.tpdFlowRateValue:
                    return FlowRateType.Value;

                case tpdFlowRateType.tpdFlowRateNearestZoneFreshAir:
                    return FlowRateType.NearestZoneFreshAir
                        ;
            }

            throw new System.NotImplementedException();
        }
    }
}
