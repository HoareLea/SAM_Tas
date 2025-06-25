using System.ComponentModel;
using SAM.Core.Attributes;
using SAM.Core.Systems;

namespace SAM.Analytical.Tas.TPD
{
    [AssociatedTypes(typeof(ISystemController)), Description("System Controller Parameter")]
    public enum SystemControllerParameter
    {
        [ParameterProperties("LUA Code", "LUA Code"), ParameterValue(Core.ParameterType.String)] LUACode,
        [ParameterProperties("LUA Enabled", "LUA Enabled"), ParameterValue(Core.ParameterType.Boolean)] LUAEnabled,
    }
}