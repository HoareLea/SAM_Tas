using SAM.Core;
using TAS3D;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static ParameterSet ParameterSet(this Setting setting, Zone zone)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, zone, typeof(Zone), typeof(Space));
            parameterSet.Add("IsUsed", zone.isUsed == 1);

            return parameterSet;
        }

        public static ParameterSet ParameterSet(this Setting setting, Element element)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, element, typeof(Element), typeof(Panel));
            parameterSet.Add("IsUsed", element.isUsed == 1);

            return parameterSet;
        }

        public static ParameterSet ParameterSet(this Setting setting, window window)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, window, typeof(window), typeof(Aperture));
            parameterSet.Add("IsUsed", window.isUsed == 1);

            return parameterSet;
        }

        public static ParameterSet ParameterSet(this Setting setting, Building building)
        {
            return Core.Tas.Create.ParameterSet(setting, building, typeof(Building), typeof(RelationCluster));
        }

        public static ParameterSet ParameterSet(this Setting setting, zoneSet zoneSet)
        {
            return Core.Tas.Create.ParameterSet(setting, zoneSet, typeof(zoneSet), typeof(GuidCollection));
        }

        public static ParameterSet ParameterSet(this Setting setting, shade shade)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, shade, typeof(shade), typeof(Panel));
            parameterSet.Add("IsUsed", shade.isUsed == 1);

            return parameterSet;
        }

        public static ParameterSet ParameterSet(this Setting setting, ZoneData zoneData)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, zoneData, typeof(ZoneData), typeof(Space));

            return parameterSet;
        }

        public static ParameterSet ParameterSet(this Setting setting, SurfaceData surfaceData)
        {
            ParameterSet parameterSet = Core.Tas.Create.ParameterSet(setting, surfaceData, typeof(SurfaceData), typeof(Panel));

            return parameterSet;
        }
    }
}