using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Aperture)), Description("Aperture Parameter")]
    public enum ApertureParameter
    {
        [ParameterProperties("Pane BuildingElement Guid", "Frame BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] PaneBuildingElementGuid,
        [ParameterProperties("Frame BuildingElement Guid", "Frame BuildingElement Guid"), ParameterValue(Core.ParameterType.String)] FrameBuildingElementGuid,
        [ParameterProperties("Frame ZoneSurfaceReference 1", "Frame ZoneSurfaceReference 1"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] FrameZoneSurfaceReference_1,
        [ParameterProperties("Frame ZoneSurfaceReference 2", "Frame ZoneSurfaceReference 2"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] FrameZoneSurfaceReference_2,
        [ParameterProperties("Pane ZoneSurfaceReference 1", "Pane ZoneSurfaceReference 1"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] PaneZoneSurfaceReference_1,
        [ParameterProperties("Pane ZoneSurfaceReference 2", "Pane ZoneSurfaceReference 2"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] PaneZoneSurfaceReference_2,
    }
}