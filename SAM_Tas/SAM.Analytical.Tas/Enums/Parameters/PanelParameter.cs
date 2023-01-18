using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Panel)), Description("Panel Parameter")]
    public enum PanelParameter
    {
        [ParameterProperties("Zone Surface Guid", "Zone Surface Guid"), ParameterValue(Core.ParameterType.String)] ZoneSurfaceGuid,
        [ParameterProperties("BuildingElement Guid", "BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] BuildingElementGuid,
    }
}