using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(SpaceSimulationResult)), Description("SpaceSimulationResult Parameter")]
    public enum SpaceSimulationResultParameter
    {
        [ParameterProperties("Dry Bulb Temperature Profile", "Dry Bulb Temperature Profile"), SAMObjectParameterValue(typeof(Profile))] DryBulbTemperatureProfile,
        [ParameterProperties("Zone Guid", "Zone Guid"), ParameterValue(Core.ParameterType.String)] ZoneGuid,
        [ParameterProperties("Zone Number", "Zone Number"), ParameterValue(Core.ParameterType.Integer)] ZoneNumber,
        [ParameterProperties("Cooling Design Day Name", "Cooling Design Day Name"), ParameterValue(Core.ParameterType.String)] CoolingDesignDayName,
        [ParameterProperties("Heating Design Day Name", "Heating Design Day Name"), ParameterValue(Core.ParameterType.String)] HeatingDesignDayName,
    }
}