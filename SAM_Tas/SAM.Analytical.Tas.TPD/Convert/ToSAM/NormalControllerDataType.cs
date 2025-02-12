using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static NormalControllerDataType ToSAM_NormalControllerDataType(this global::TPD.tpdSensorType tpdSensorType)
        {
            switch(tpdSensorType)
            {
                case global::TPD.tpdSensorType.tpdTempSensor:
                    return NormalControllerDataType.DryBulbTemperature;

                case global::TPD.tpdSensorType.tpdHumRatSensor:
                    return NormalControllerDataType.HumidityRatio;

                case global::TPD.tpdSensorType.tpdRelHumSensor:
                    return NormalControllerDataType.RelativeHumidity;

                case global::TPD.tpdSensorType.tpdEnthalpySensor:
                    return NormalControllerDataType.Enthalpy;
                
                case global::TPD.tpdSensorType.tpdFlowSensor:
                    return NormalControllerDataType.Flow;
                
                case global::TPD.tpdSensorType.tpdPollutantSensor:
                    return NormalControllerDataType.Pollutant;
                
                case global::TPD.tpdSensorType.tpdPressureSensor:
                    return NormalControllerDataType.Pressure;
                
                case global::TPD.tpdSensorType.tpdThermostatSensor:
                    return NormalControllerDataType.ThermostatTemperature;
                
                case global::TPD.tpdSensorType.tpdHumidistatSensor:
                    return NormalControllerDataType.HumidistatRelativeHumidity;

                case global::TPD.tpdSensorType.tpdLoadSensor:
                    return NormalControllerDataType.Load;

                case global::TPD.tpdSensorType.tpdWetbulbSensor:
                    return NormalControllerDataType.WetBulbTemperature;

                case global::TPD.tpdSensorType.tpdMinFlowSensor:
                    return NormalControllerDataType.MinFlow;

                case global::TPD.tpdSensorType.tpdPartLoadSensor:
                    return NormalControllerDataType.PartLoad;
            }

            throw new System.NotImplementedException();
        }
    }
}
