using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static LiquidNormalControllerDataType ToSAM_LiquidNormalControllerDataType(this global::TPD.tpdSensorType tpdSensorType)
        {
            switch(tpdSensorType)
            {
                case global::TPD.tpdSensorType.tpdTempSensor:
                    return LiquidNormalControllerDataType.Temperature;

                case global::TPD.tpdSensorType.tpdHumRatSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdRelHumSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdEnthalpySensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdFlowSensor:
                    return LiquidNormalControllerDataType.Flow;
                
                case global::TPD.tpdSensorType.tpdPollutantSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdPressureSensor:
                    return LiquidNormalControllerDataType.Pressure;
                
                case global::TPD.tpdSensorType.tpdThermostatSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdHumidistatSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdLoadSensor:
                    return LiquidNormalControllerDataType.Load;

                case global::TPD.tpdSensorType.tpdWetbulbSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdMinFlowSensor:
                    throw new System.NotImplementedException();

                case global::TPD.tpdSensorType.tpdPartLoadSensor:
                    return LiquidNormalControllerDataType.Load;
            }

            throw new System.NotImplementedException();
        }
    }
}
