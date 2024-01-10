using SAM.Core.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ISystemComponentResult ToSAM_SystemComponentResult(this ZoneComponent zoneComponent, int start, int end)
        {
            if (zoneComponent == null)
            {
                return null;
            }

            if (zoneComponent is FanCoilUnit)
            {
                return ((FanCoilUnit)zoneComponent).ToSAM_SystemFanCoilUnitResult(start, end);
            }

            if (zoneComponent is Radiator)
            {
                return ((Radiator)zoneComponent).ToSAM_SystemRadiatorResult(start, end);
            }

            if (zoneComponent is DXCoilUnit)
            {
                return ((DXCoilUnit)zoneComponent).ToSAM_SystemDXCoilUnitResult(start, end);
            }

            if (zoneComponent is ChilledBeam)
            {
                return ((ChilledBeam)zoneComponent).ToSAM_SystemChilledBeamResult(start, end);
            }

            return null;
        }
    }
}
