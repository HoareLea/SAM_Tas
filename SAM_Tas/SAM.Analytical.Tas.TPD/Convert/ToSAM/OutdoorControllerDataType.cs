using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static OutdoorControllerDataType ToSAM_OutdoorControllerDataType(this global::TPD.tpdSensorType tpdSensorType)
        {
            switch(tpdSensorType)
            {
                case global::TPD.tpdSensorType.tpdTempSensor:
                    return OutdoorControllerDataType.DryBulbTemperature;

                case global::TPD.tpdSensorType.tpdHumRatSensor:
                    return OutdoorControllerDataType.HumidityRatio;

                case global::TPD.tpdSensorType.tpdRelHumSensor:
                    return OutdoorControllerDataType.RelativeHumidity;

                case global::TPD.tpdSensorType.tpdEnthalpySensor:
                    return OutdoorControllerDataType.Enthalpy;
                
                case global::TPD.tpdSensorType.tpdFlowSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdPollutantSensor:
                    return OutdoorControllerDataType.Pollutant;
                
                case global::TPD.tpdSensorType.tpdPressureSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdThermostatSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdHumidistatSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdLoadSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdWetbulbSensor:
                    return OutdoorControllerDataType.WetBulbTemperature;

                case global::TPD.tpdSensorType.tpdMinFlowSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdPartLoadSensor:
                    throw new System.NotImplementedException();
            }

            throw new System.NotImplementedException();
        }
    }
}
