using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(SpaceSimulationResult)), Description("SpaceSimulationResult Parameter")]
    public enum SpaceSimulationResultParameter
    {
        [ParameterProperties("Dry Bulb Temperature Profile", "Dry Bulb Temperature Profile"), SAMObjectParameterValue(typeof(Profile))] DryBulbTemperatureProfile,
    }
}