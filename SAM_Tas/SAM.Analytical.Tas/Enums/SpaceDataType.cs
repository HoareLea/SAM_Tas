using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum SpaceDataType
    {
        [Description("Undefined")] Undefined,
        [Description("Dry Bulb Temperature")] DryBulbTemperature = 1,
        [Description("Mean Radiant Temperature")] MeanRadiantTemperature = 2,
        [Description("Resultant Temperature")] ResultantTemperature = 3,
        [Description("Sensible Load")] SensibleLoad = 4,
        [Description("Heating Load")] HeatingLoad = 5,
        [Description("Cooling Load")] CoolingLoad = 6,
        [Description("Solar Gain")] SolarGain = 7,
        [Description("Lighting Gain")] LightingGain = 8,
        [Description("Infiltration Ventilation Gain")] InfiltrationVentilationGain = 9,
        [Description("Air Movement Gain")] AirMovementGain = 10,
        [Description("Building Heat Transfer")] BuildingHeatTransfer = 11,
        [Description("External Conduction Opaque")] ExternalConductionOpaque = 12,
        [Description("External Conduction Glazing")] ExternalConductionGlazing = 13,
        [Description("Occupant Sensible Gain")] OccupantSensibleGain = 14,
        [Description("Equipment Sensible Gain")] EquipmentSensibleGain = 15,
        [Description("Humidity Ratio")] HumidityRatio = 16,
        [Description("Relative Humidity")] relativeHumidity = 17,
        [Description("Occupancy Latent Gain")] OccupancyLatentGain = 18,
        [Description("Equipment Latent Gain")] EquipmentLatentGain = 19,
        [Description("Latent Load")] LatentLoad = 20,
        [Description("Latent Removal Load")] LatentRemovalLoad = 21,
        [Description("Latent Addition Load")] LatentAdditionLoad = 22,
        [Description("Vapour Pressure")] VapourPressure = 23,
        [Description("Pollutant")] Pollutant = 24,
        [Description("Pressure")] Pressure = 25,
        [Description("Infiltration")] Infiltration = 26,
        [Description("Ventilation")] Ventilation = 27,
        [Description("ZoneApertureFlowIn")] ZoneApertureFlowIn = 28,
        [Description("ZoneApertureFlowOut")] ZoneApertureFlowOut = 29,
        [Description("IZAM In")] IZAMIn = 30,
        [Description("IZAM Out")] IZAMOut = 31
    }
}
