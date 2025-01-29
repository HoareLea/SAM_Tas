using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ScheduleFunctionType ToSAM(this tpdScheduleFunctionType tpdScheduleFunctionType)
        {
            switch(tpdScheduleFunctionType)
            {
                case tpdScheduleFunctionType.tpdScheduleFunctionAllZonesLoad:
                    return ScheduleFunctionType.AllZonesLoad;

                case tpdScheduleFunctionType.tpdScheduleFunctionNearestZoneLoad:
                    return ScheduleFunctionType.NearestZoneLoad;

                case tpdScheduleFunctionType.tpdScheduleFunctionNone:
                    return ScheduleFunctionType.None;
            }

            throw new System.NotImplementedException();
        }
    }
}
