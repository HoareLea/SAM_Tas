using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Construction)), Description("Construction Parameter")]
    public enum ConstructionParameter
    {
        [ParameterProperties("Additional Heat Transfer", "Additional Heat Transfer [%]"), DoubleParameterValue(-50, 150)] AdditionalHeatTransfer,
    }
}