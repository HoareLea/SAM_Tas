using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(AnalyticalModelSimulationResult)), Description("AnalyticalModelSimulationResult Parameter")]
    public enum AnalyticalModelSimulationResultParameter
    {
        [ParameterProperties("Annual Total Consumption", "Annual Total Consumption [kWh]"), ParameterValue(Core.ParameterType.Double)] AnnualTotalConsumption,
        [ParameterProperties("Annual CO2 Emission", "Annual CO2 Emission [kg]"), ParameterValue(Core.ParameterType.Double)] AnnualCO2Emission,
        [ParameterProperties("Annual Cost", "Annual Cost"), ParameterValue(Core.ParameterType.Double)] AnnualCost,
        [ParameterProperties("Annual Unmet Hours", "Annual Unmet Hours [h]"), ParameterValue(Core.ParameterType.Double)] AnnualUnmetHours,
    }
}