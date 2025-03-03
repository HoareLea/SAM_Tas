using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizedFlowMethod ToSAM(this global::TPD.tpdSizeFlowMethod tpdSizeFlowMethod)
        {
            switch(tpdSizeFlowMethod)
            {
                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPeakPerson:
                    return SizedFlowMethod.PeakPerson;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowHourlyPerson:
                    return SizedFlowMethod.HourlyPerson;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPerMeterSquared:
                    return SizedFlowMethod.PerMeterSquared;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowACH:
                    return SizedFlowMethod.FlowACH;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPerMeterCubed:
                    return SizedFlowMethod.PerMeterCubed;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPeakInternalCondition:
                    return SizedFlowMethod.PeakInternalCondition;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPeakPersonAndArea:
                    return SizedFlowMethod.PeakPersonAndArea;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowDeltaT:
                    return SizedFlowMethod.TemperatureDifference;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowHourlyInternalCondition:
                    return SizedFlowMethod.HourlyInternalCondition;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowHourlyVentilation:
                    return SizedFlowMethod.HourlyVentilation;

                case global::TPD.tpdSizeFlowMethod.tpdSizeFlowPeakVentilation:
                    return SizedFlowMethod.PeakVentilation;
            }

            throw new System.NotImplementedException();
        }
    }
}
