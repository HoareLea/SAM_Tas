using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Panel)), Description("Panel Parameter")]
    public enum PanelParameter
    {
        [ParameterProperties("ZoneSurfaceReference 1", "ZoneSurfaceReference 1"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] ZoneSurfaceReference_1,
        [ParameterProperties("ZoneSurfaceReference 2", "ZoneSurfaceReference 2"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] ZoneSurfaceReference_2,
        [ParameterProperties("BuildingElement Guid", "BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] BuildingElementGuid,
    }
}