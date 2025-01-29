using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdScheduleFunctionType ToTPD(this ScheduleFunctionType scheduleFunctionType)
        {
            switch (scheduleFunctionType)
            {
                case ScheduleFunctionType.None:
                    return tpdScheduleFunctionType.tpdScheduleFunctionNone;

                case ScheduleFunctionType.AllZonesLoad:
                    return tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad;

                case ScheduleFunctionType.NearestZoneLoad:
                    return tpdScheduleFunctionType.tpdScheduleFunctionNearestZoneLoad;
            }

            throw new System.NotImplementedException();
        }
    }
}
