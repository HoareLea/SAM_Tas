using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    [Description("DX Coil Unit Data Type")]
    public enum DXCoilUnitDataType
    {
        [Description("Heating Load")] HeatingLoad = 1,
        [Description("Cooling Sensible Load")] CoolingSensibleLoad = 2,
        [Description("Cooling Latent Load")] CoolingLatentLoad = 3,
        [Description("Electrical Load")] ElectricalLoad = 4,
        [Description("Condensation")] Condensation = 5,
    }
}
