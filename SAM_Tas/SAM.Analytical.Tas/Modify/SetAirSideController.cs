namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void SetAirSideController(this TPD.Controller controller, AirSideControllerSetup airSideControllerSetup, double extra1 = 0, double extra2 = 0)
        {
            if(airSideControllerSetup == AirSideControllerSetup.Undefined)
            {
                return;
            }

            // Set up airside controller function (for commonly used controller setups)
            switch (airSideControllerSetup)
            {
                case AirSideControllerSetup.ThermUL:
                    // thermostat: upper limit / cooling setpoint
                    // extra 1 = offset
                    // extra 2 = control band  
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = 1;
                    controller.Max = 1;
                    controller.Min = 0;
                    controller.SensorType = TPD.tpdSensorType.tpdThermostatSensor;
                    controller.SensorPresetType = TPD.tpdSensorPresetType.tpdUpperLimit;
                    controller.Setpoint = extra1;
                    break;
                case AirSideControllerSetup.ThermLL:
                    // thermostat: lower limit / heating setpoint
                    // extra 1 = offset
                    // extra 2 = control band        
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = -1;
                    controller.Max = 1;
                    controller.Min = 0;
                    controller.SensorType = TPD.tpdSensorType.tpdThermostatSensor;
                    controller.SensorPresetType = TPD.tpdSensorPresetType.tpdLowerLimit;
                    controller.Setpoint = extra1;
                    break;
                case AirSideControllerSetup.TempHighZero:
                    // temperature: zero signal at high temp
                    // extra 1 = setpoint
                    // extra 2 = control band
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = -1;
                    controller.Max = 1;
                    controller.Min = 0;
                    controller.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    controller.Setpoint = extra1;
                    break;
                case AirSideControllerSetup.TempLowZero:
                    // temperature: zero signal at low temp
                    // extra 1 = setpoint
                    // extra 2 = control band
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = 1;
                    controller.Max = 1;
                    controller.Min = 0;
                    controller.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    controller.Setpoint = extra1;
                    break;
                case AirSideControllerSetup.TempPassThrough:
                    // temperature pass through
                    // extra 1 = offset
                    controller.ControlType = TPD.tpdControlType.tpdControlPassThrough;
                    controller.Setpoint = extra1;
                    controller.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    break;
                case AirSideControllerSetup.ThermBothL:
                    // thermostat: both upper and lower limits
                    // extra 1 = min value
                    // extra 2 = control band
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = -1;
                    controller.Max = 1;
                    controller.Min = extra1;
                    controller.SensorType = TPD.tpdSensorType.tpdThermostatSensor;
                    controller.SensorPresetType = TPD.tpdSensorPresetType.tpdLowerAndUpperLimit;
                    controller.Setpoint = 0;
                    break;
                case AirSideControllerSetup.Press:
                    // pressure (note gradient not specified)
                    // extra 1 = setpoint
                    // extra 2 = control band
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.SensorType = TPD.tpdSensorType.tpdPressureSensor;
                    controller.Setpoint = extra1;
                    controller.Band = extra2;
                    controller.Min = 0;
                    controller.Max = 1;
                    break;
                case AirSideControllerSetup.AlwaysReturnOne:
                    // always return one (fresh air only mixing box)
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Min = 1;
                    controller.Max = 1;
                    controller.Setpoint = 0;
                    break;
                case AirSideControllerSetup.TempDifference:
                    // temperature difference arc1 minus arc2
                    // extra 1 = setpoint
                    // extra 2 = control band
                    controller.ControlType = TPD.tpdControlType.tpdControlDifference;
                    controller.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    controller.Setpoint = extra1;
                    controller.Band = extra2;
                    controller.Gradient = 1;
                    controller.Max = 1;
                    controller.Min = 0;
                    break;
                case AirSideControllerSetup.EnthLowZero:
                    // enthalpy: zero signal at low enthalpy
                    // extra 1 = setpoint
                    // extra 2 = control band
                    controller.Band = extra2;
                    controller.ControlType = TPD.tpdControlType.tpdControlNormal;
                    controller.Gradient = 1;
                    controller.Max = 1;
                    controller.Min = 0;
                    controller.SensorType = TPD.tpdSensorType.tpdEnthalpySensor;
                    controller.Setpoint = extra1;
                    break;
            }

        }
    }
}