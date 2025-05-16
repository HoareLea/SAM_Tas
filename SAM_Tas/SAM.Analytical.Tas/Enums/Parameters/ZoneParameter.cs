using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Zone)), Description("Zone Parameter")]
    public enum ZoneParameter
    {
        [ParameterProperties("Zone Group Category TBD", "Zone Group Category TBD"), ParameterValue(Core.ParameterType.String)] TBDZoneGroup,
    }
}