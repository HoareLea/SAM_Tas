using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    public enum BuildingCategory
    {
        [Description("Undefined")] Undefined,
        [Description("Category I")] Category_I = 0,
        [Description("Category II")] Category_II = 1,
    }
}
