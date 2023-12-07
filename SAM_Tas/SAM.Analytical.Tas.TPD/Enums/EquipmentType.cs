using System.ComponentModel;

namespace SAM.Analytical.Tas.TPD
{
    [Description("Equipment Type")]
    public enum EquipmentType
    {
        [Description("Undefined")] Undefined,
        [Description("Radiator")] Radiator,
        [Description("Fan Coil Unit")] FanCoilUnit,
        [Description("DX Coil Unit")] DXCoilUnit,
        [Description("Chilled Beam")] ChilledBeam,
        [Description("Chilled Beam with Heating")] ChilledBeamWithHeating,
    }
}
