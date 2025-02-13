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

                case NormalControllerDataType.HumidityRatio:
                    return tpdSensorType.tpdHumRatSensor;

                case NormalControllerDataType.RelativeHumidity:
                    return tpdSensorType.tpdRelHumSensor;

                case NormalControllerDataType.Enthalpy:
                    return tpdSensorType.tpdEnthalpySensor;

                case NormalControllerDataType.Flow:
                    return tpdSensorType.tpdFlowSensor;

                case NormalControllerDataType.Pollutant:
                    return tpdSensorType.tpdPollutantSensor;

                case NormalControllerDataType.Pressure:
                    return tpdSensorType.tpdPressureSensor;

                case NormalControllerDataType.ThermostatTemperature:
                    return tpdSensorType.tpdThermostatSensor;

                case NormalControllerDataType.HumidistatRelativeHumidity:
                    return tpdSensorType.tpdHumidistatSensor;

                case NormalControllerDataType.Load:
                    return tpdSensorType.tpdLoadSensor;

                case NormalControllerDataType.WetBulbTemperature:
                    return tpdSensorType.tpdWetbulbSensor;

                case NormalControllerDataType.MinFlow:
                    return tpdSensorType.tpdMinFlowSensor;

                case NormalControllerDataType.PartLoad:
                    return tpdSensorType.tpdPartLoadSensor;
            }

            throw new System.NotImplementedException();
        }

        public static tpdSensorType ToTPD(this LiquidNormalControllerDataType liquidNormalControllerDataType)
        {
            switch (liquidNormalControllerDataType)
            {
                case LiquidNormalControllerDataType.Flow:
                    return tpdSensorType.tpdFlowSensor;

                case LiquidNormalControllerDataType.Temperature:
                    return tpdSensorType.tpdTempSensor;

                case LiquidNormalControllerDataType.Load:
                    return tpdSensorType.tpdLoadSensor;

                case LiquidNormalControllerDataType.Pressure:
                    return tpdSensorType.tpdPressureSensor;
            }

            throw new System.NotImplementedException();
        }
    }
}
