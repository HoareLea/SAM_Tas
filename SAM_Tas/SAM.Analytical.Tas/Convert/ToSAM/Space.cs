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
            parameterSet.Add("IsUsed", zone.isUsed == 1);

            Space space = new Space(zone.name, null);
            space.Add(parameterSet);

            return space;
        }
    }
}
