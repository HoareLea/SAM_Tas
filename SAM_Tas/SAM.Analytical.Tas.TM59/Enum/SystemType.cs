using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum SystemType
    {
        [Description("Undefined")] Undefined = 2,
        [Description("Nat Vent")] NaturalVentilation = 0,
        [Description("Mech Vent")] MechanicalVentilation = 1,
    }
}
