using System.ComponentModel;
using SAM.Analytical.Tas.TPD;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(ISystemObject)), Description("System Object Parameter")]
    public enum SystemObjectParameter
    {
        [ParameterProperties("Reference", "Reference"), ParameterValue(Core.ParameterType.String)] Reference,
    }
}