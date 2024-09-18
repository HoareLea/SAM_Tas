using System.ComponentModel;

namespace SAM.Core.Tas.TPD
{
    [Description("Result Data Type.")]
    public enum ResultDataType
    {
        [Description("Undefined")] Undefined,
        [Description("Load")] Load,
        [Description("Electric Load")] ElectricLoad,
        [Description("Thermal Load")] ThermalLoad,
        [Description("Demand")] Demand,
        [Description("Electric Demand")] ElectricDemand,
        [Description("Thermal Demand")] ThermalDemand,
        [Description("Consumption")] Consumption,
        [Description("Electric Consumption")] ElectricConsumption,
        [Description("Thermal Consumption")] ThermalConsumption,
        [Description("Generated")] Generated,
        [Description("CO2")] Co2,
        [Description("Cost")] Cost,
        [Description("Fuel Type")] FuelType,
        [Description("Electricl Fuel Type")] ElectricFuelType,
        [Description("Thermal Fuel Type")] ThermalFuelType,
        [Description("Plant Component")] PlantComponent,
        [Description("Electric Plan Component")] ElectricPlantComponent,
        [Description("Thermal Plant Component")] ThermalPlantComponent,
        [Description("Unmet Hours")] UnmetHours
    }
}
