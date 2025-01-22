using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdHeatPumpType ToTPD(this HeatPumpType heatPumpType)
        {
            switch (heatPumpType)
            {
                case HeatPumpType.HVRFBC:
                    return tpdHeatPumpType.tpdHeatPumpHVRFBC;

                case HeatPumpType.MultiSplit:
                    return tpdHeatPumpType.tpdHeatPumpMultiSplit;

                case HeatPumpType.SingleSplit:
                    return tpdHeatPumpType.tpdHeatPumpSingleSplit;

                case HeatPumpType.VRF:
                    return tpdHeatPumpType.tpdHeatPumpVRF;

                case HeatPumpType.VRFBC:
                    return tpdHeatPumpType.tpdHeatPumpVRFBC;

            }

            throw new System.NotImplementedException();
        }
    }
}
