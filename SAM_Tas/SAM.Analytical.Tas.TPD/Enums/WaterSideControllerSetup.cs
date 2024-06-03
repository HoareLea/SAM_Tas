using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    public enum WaterSideControllerSetup
    {
        [Description("Undefined")] Undefined,
        [Description("Load")] Load,
        [Description("Temperature Low Zero")] TemperatureLowZero,
        [Description("Temperature Difference")] TemperatureDifference,
    }
}

