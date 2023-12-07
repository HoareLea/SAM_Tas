using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    [Description("Chilled Beam Data Type")]
    public enum ChilledBeamDataType
    {
        [Description("Cooling Sensible Load")] CoolingSensibleLoad = 1,
        [Description("Cooling Latent Load")] CoolingLatentLoad = 2,
        [Description("Condensation")] Condensation = 3,
    }
}
