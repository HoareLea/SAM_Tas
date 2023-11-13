using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(ApertureConstruction)), Description("ApertureConstruction Parameter")]
    public enum ApertureConstructionParameter
    {
        [ParameterProperties("Additional Heat Transfer", "Additional Heat Transfer [%]"), DoubleParameterValue(-50, 150)] AdditionalHeatTransfer,
    }
}