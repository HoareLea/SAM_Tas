using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    [Description("Chilled Beam with Heating Data Type")]
    public enum ChilledBeamWithHeatingDataType
    {
        [Description("Cooling Sensible Load")] CoolingSensibleLoad = 1,
        [Description("Cooling Latent Load")] CoolingLatentLoad = 2,
        [Description("Heating Load")] HeatingLoad = 3,
        [Description("Condensation")] Condensation = 4,
    }
}
