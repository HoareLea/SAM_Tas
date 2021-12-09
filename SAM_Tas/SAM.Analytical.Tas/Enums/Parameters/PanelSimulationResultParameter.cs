using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(PanelSimulationResult)), Description("PanelSimulationResult Parameter")]
    public enum PanelSimulationResultParameter
    {
        [ParameterProperties("Zone Name", "Zone Name"), ParameterValue(Core.ParameterType.String)] ZoneName,
    }
}