using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdSensorType ToTPD(this NormalControllerDataType normalControllerDataType)
        {
            switch (normalControllerDataType)
            {
                case NormalControllerDataType.DryBulbTemperature:
                    return tpdSensorType.tpdTempSensor;

                case NormalControllerDataType.WetBulbTemperature:
                    return tpdSensorType.tpdWetbulbSensor;

                case NormalControllerDataType.Pressure:
                    return tpdSensorType.tpdPressureSensor;

                case NormalControllerDataType.ThermostatTemperature:
                    return tpdSensorType.tpdThermostatSensor;

                case NormalControllerDataType.HumidityRatio:
                    return tpdSensorType.tpdHumRatSensor;

                case NormalControllerDataType.Enthalpy:
                    return tpdSensorType.tpdEnthalpySensor;

                case NormalControllerDataType.Flow:
                    return tpdSensorType.tpdFlowSensor;

                case NormalControllerDataType.RelativeHumidity:
                    return tpdSensorType.tpdHumidistatSensor;

                case NormalControllerDataType.MinimalFreshAir:
                    return tpdSensorType.tpdMinFlowSensor;

                case NormalControllerDataType.Pollutant:
                    return tpdSensorType.tpdPollutantSensor;
            }

            throw new System.NotImplementedException();
        }
    }
}
