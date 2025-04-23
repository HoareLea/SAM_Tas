using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Zone)), Description("Zone Parameter")]
    public enum ZoneParameter
    {
        [ParameterProperties("Zone Type", "Zone Type"), ParameterValue(Core.ParameterType.String)] ZoneType,
    }
}