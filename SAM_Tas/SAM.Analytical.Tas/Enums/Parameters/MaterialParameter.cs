using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Core.Material)), Description("Material Parameter")]
    public enum MaterialParameter
    {
        [ParameterProperties("UniqueId", "UniqueId"), ParameterValue(Core.ParameterType.String)] UniqueId,
    }
}