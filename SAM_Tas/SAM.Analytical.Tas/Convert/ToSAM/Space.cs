using SAM.Core;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Space ToSAM(this TAS3D.Zone zone)
        {
            if (zone == null)
                return null;

            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, zone);

            Space space = new Space(zone.name, null);
            space.Add(parameterSet);

            return space;
        }

        public static Space ToSAM(this TSD.ZoneData zoneData)
        {
            ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, zoneData);

            Space space = new Space(zoneData.name, null);
            space.Add(parameterSet);

            return space;
        }
    }
}
