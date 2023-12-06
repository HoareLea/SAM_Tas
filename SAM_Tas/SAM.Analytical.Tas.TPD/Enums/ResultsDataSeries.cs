using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    [Description("Result Data Type")]
    public enum ResultDataType
    {
        [Description("Cooling Sensible Load")] CoolingSensibleLoad = 2,
        [Description("Cooling Latent Load")] CoolingLatentLoad = 3,
        [Description("Temperature")] Temperature = 6,
        [Description("Humidity Ratio")] HumidityRatio = 7,
        [Description("Flow Rate")] FlowRate = 8
    }
}
