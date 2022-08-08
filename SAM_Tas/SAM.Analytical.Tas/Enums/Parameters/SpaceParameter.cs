using System.ComponentModel;
using SAM.Core.Attributes;

namespace SAM.Analytical.Tas
{
    [AssociatedTypes(typeof(Space)), Description("Space Parameter")]
    public enum SpaceParameter
    {
        [ParameterProperties("Zone Guid", "Zone Guid"), ParameterValue(Core.ParameterType.String)] ZoneGuid,
    }
}