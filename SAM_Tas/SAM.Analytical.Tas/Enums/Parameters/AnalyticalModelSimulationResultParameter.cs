using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(AnalyticalModelSimulationResult)), Description("AnalyticalModelSimulationResult Parameter")]
    public enum AnalyticalModelSimulationResultParameter
    {
        [ParameterProperties("Total Consumption", "Total Consumption [W]"), ParameterValue(Core.ParameterType.Double)] TotalConsumption,
    }
}