using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Aperture)), Description("Aperture Parameter")]
    public enum ApertureParameter
    {
        [ParameterProperties("Frame Zone Surface Guid", "Frame Zone Surface Guid"), ParameterValue(Core.ParameterType.String)] FrameZoneSurfaceGuid,
        [ParameterProperties("Pane Zone Surface Guid", "Pane Zone Surface Guid"), ParameterValue(Core.ParameterType.String)] PaneZoneSurfaceGuid,
        [ParameterProperties("Frame Zone Surface Number", "Frame Zone Surface Number"), ParameterValue(Core.ParameterType.Integer)] FrameZoneSurfaceNumber,
        [ParameterProperties("Pane Zone Surface Number", "Pane Zone Surface Number"), ParameterValue(Core.ParameterType.Integer)] PaneZoneSurfaceNumber,
        [ParameterProperties("Pane BuildingElement Guid", "Frame BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] PaneBuildingElementGuid,
        [ParameterProperties("Frame BuildingElement Guid", "Frame BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] FrameBuildingElementGuid,
    }
}