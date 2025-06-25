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

        [ParameterProperties("Dampen Value", "Dampen Value"), ParameterValue(Core.ParameterType.Boolean)] DampenValue,
        [ParameterProperties("Dampen Signal", "Dampen Signal"), ParameterValue(Core.ParameterType.Boolean)] DampenSignal,
        [ParameterProperties("Lua Hidden", "Lua Hidden"), ParameterValue(Core.ParameterType.Boolean)] LuaHidden,
        [ParameterProperties("Has Control Type", "Has Control Type"), ParameterValue(Core.ParameterType.Boolean)] HasControlType,
        [ParameterProperties("Has Controller Type", "Has Controller Type"), ParameterValue(Core.ParameterType.Boolean)] HasControllerType,
        [ParameterProperties("Has Sensor Type", "Has Sensor Type"), ParameterValue(Core.ParameterType.Boolean)] HasSensorType,
        [ParameterProperties("Has Sensor Preset Type", "Has Sensor Preset Type"), ParameterValue(Core.ParameterType.Boolean)] HasSensorPresetType,
        [ParameterProperties("Has Sensor Arc 1", "Has Sensor Arc 1"), ParameterValue(Core.ParameterType.Boolean)] HasSensorArc1,
        [ParameterProperties("Has Sensor Arc 2", "Has Sensor Arc 2"), ParameterValue(Core.ParameterType.Boolean)] HasSensorArc2,
        [ParameterProperties("Has Profile", "Has Profile"), ParameterValue(Core.ParameterType.Boolean)] HasProfile,
        [ParameterProperties("Has Setback Profile", "Has Setback Profile"), ParameterValue(Core.ParameterType.Boolean)] HasSetbackProfile,
        [ParameterProperties("Has Schedule", "Has Schedule"), ParameterValue(Core.ParameterType.Boolean)] HasSchedule,
        [ParameterProperties("Has Setpoint", "Has Setpoint"), ParameterValue(Core.ParameterType.Boolean)] HasSetpoint,
        [ParameterProperties("Has Band", "Has Band"), ParameterValue(Core.ParameterType.Boolean)] HasBand,
        [ParameterProperties("Has Gradient", "Has Gradient"), ParameterValue(Core.ParameterType.Boolean)] HasGradient,
        [ParameterProperties("Has Min", "Has Min"), ParameterValue(Core.ParameterType.Boolean)] HasMin,
        [ParameterProperties("Has Max", "Has Max"), ParameterValue(Core.ParameterType.Boolean)] HasMax,
        [ParameterProperties("Has Setback Setpoint", "Has Setback Setpoint"), ParameterValue(Core.ParameterType.Boolean)] HasSetbackSetpoint,
        [ParameterProperties("Has Setback Band", "Has Setback Band"), ParameterValue(Core.ParameterType.Boolean)] HasSetbackBand,
        [ParameterProperties("Has Setback Gradient ", "Has Setback Gradient "), ParameterValue(Core.ParameterType.Boolean)] HasSetbackGradient,
        [ParameterProperties("Has Setback Min", "Has Setback Min"), ParameterValue(Core.ParameterType.Boolean)] HasSetbackMin,
        [ParameterProperties("Has Setback Max", "Has Setback Max"), ParameterValue(Core.ParameterType.Boolean)] HasSetbackMax,
    }
}