using SAM.Core;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static ParameterSet ParameterSet(this Setting setting, Zone zone)
        {
            return Core.Tas.Create.ParameterSet(setting, zone, typeof(Space));
        }

        public static ParameterSet ParameterSet(this Setting setting, Element element)
        {
            return Core.Tas.Create.ParameterSet(setting, element, typeof(Panel));
        }

        public static ParameterSet ParameterSet(this Setting setting, window window)
        {
            return Core.Tas.Create.ParameterSet(setting, window, typeof(Aperture));
        }

        public static ParameterSet ParameterSet(this Setting setting, Building building)
        {
            return Core.Tas.Create.ParameterSet(setting, building, typeof(RelationCluster));
        }
    }
}