namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void SetWaterSideController(this TPD.PlantController plantController, WaterSideControllerSetup waterSideControllerSetup, double extra1 = 0, double extra2 = 0)
        {
            if(waterSideControllerSetup == WaterSideControllerSetup.Undefined)
            {
                return;
            }

            // Set up water side controller function (for commonly used controller setups)
            switch (waterSideControllerSetup)
            {
                case WaterSideControllerSetup.Load:
                    // load on collection
                    // extra 1 = setpoint
                    // extra 2 = control band
                    plantController.Band = extra2;
                    plantController.ControlType = TPD.tpdControlType.tpdControlNormal;
                    plantController.Gradient = 1;
                    plantController.Max = 1;
                    plantController.Min = 0;
                    plantController.SensorType = TPD.tpdSensorType.tpdLoadSensor;
                    plantController.Setpoint = extra1;
                    break;
                case WaterSideControllerSetup.TemperatureLowZero:
                    // temperature: zero signal at low temp
                    // extra 1 = setpoint
                    // extra 2 = control band
                    plantController.Band = extra2;
                    plantController.ControlType = TPD.tpdControlType.tpdControlNormal;
                    plantController.Gradient = 1;
                    plantController.Max = 1;
                    plantController.Min = 0;
                    plantController.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    plantController.Setpoint = extra1;
                    break;

                case WaterSideControllerSetup.TemperatureDifference:
                    // temperature difference arc1 minus arc2
                    // extra 1 = setpoint
                    // extra 2 = control band
                    plantController.ControlType = TPD.tpdControlType.tpdControlDifference;
                    plantController.SensorType = TPD.tpdSensorType.tpdTempSensor;
                    plantController.Setpoint = extra1;
                    plantController.Band = extra2;
                    plantController.Gradient = 1;
                    plantController.Max = 1;
                    plantController.Min = 0;
                    break;
                default:
                    break;
            }
        }
    }
}