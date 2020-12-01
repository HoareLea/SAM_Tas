using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(Space)), Description("Space Parameter")]
    public enum ZoneParameter
    {
        [ParameterProperties("Guid", "Guid"), ParameterValue(Core.ParameterType.String)] Guid,
    }
}