using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Panel)), Description("Panel Parameter")]
    public enum PanelParameter
    {
        [ParameterProperties("Zone Surface Guid", "Zone Surface Guid"), ParameterValue(Core.ParameterType.String)] ZoneSurfaceGuid,
        [ParameterProperties("Zone Surface Number", "Zone Surface Number"), ParameterValue(Core.ParameterType.Integer)] ZoneSurfaceNumber,
        [ParameterProperties("BuildingElement Guid", "BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] BuildingElementGuid,
    }
}