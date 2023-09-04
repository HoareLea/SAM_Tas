using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum SizingType
    {
        [Description("Undefined")] Undefined,
        [Description("No Sizing")]  NoSizing,
        [Description("Design Sizing Only")] DesignSizingOnly,
        [Description("Sizing")] Sizing,

    }
}
