using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdSizeFlowMethod ToTPD(this SizedFlowMethod sizedFlowMethod)
        {
            switch (sizedFlowMethod)
            {
                case SizedFlowMethod.FlowACH:
                    return tpdSizeFlowMethod.tpdSizeFlowACH;

                case SizedFlowMethod.HourlyInternalCondition:
                    return tpdSizeFlowMethod.tpdSizeFlowHourlyInternalCondition;

                case SizedFlowMethod.HourlyPerson:
                    return tpdSizeFlowMethod.tpdSizeFlowHourlyPerson;

                case SizedFlowMethod.HourlyVentilation:
                    return tpdSizeFlowMethod.tpdSizeFlowHourlyVentilation;

                case SizedFlowMethod.PeakInternalCondition:
                    return tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition;

                case SizedFlowMethod.PeakPerson:
                    return tpdSizeFlowMethod.tpdSizeFlowPeakPerson;

                case SizedFlowMethod.PeakPersonAndArea:
                    return tpdSizeFlowMethod.tpdSizeFlowPeakPersonAndArea;

                case SizedFlowMethod.PerMeterCubed:
                    return tpdSizeFlowMethod.tpdSizeFlowPerMeterCubed;

                case SizedFlowMethod.PerMeterSquared:
                    return tpdSizeFlowMethod.tpdSizeFlowPerMeterSquared;

                case SizedFlowMethod.HourlyPersonAndArea:
                    return tpdSizeFlowMethod.tpdSizeFlowHourlyPersonAndArea;

                case SizedFlowMethod.TemperatureDifference:
                    return tpdSizeFlowMethod.tpdSizeFlowDeltaT;

                case SizedFlowMethod.PeakVentilation:
                    return tpdSizeFlowMethod.tpdSizeFlowPeakVentilation;
            }

            throw new System.NotImplementedException();
        }
    }
}
