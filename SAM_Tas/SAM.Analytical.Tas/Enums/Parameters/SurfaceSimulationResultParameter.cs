using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(SurfaceSimulationResult)), Description("SurfaceSimulationResult Parameter")]
    public enum SurfaceSimulationResultParameter
    {
        [ParameterProperties("ZoneSurfaceReference", "ZoneSurfaceReference"), SAMObjectParameterValue(typeof(Core.Tas.ZoneSurfaceReference))] ZoneSurfaceReference,
    }
}