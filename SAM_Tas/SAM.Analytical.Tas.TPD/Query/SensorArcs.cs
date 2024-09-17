using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<SensorArc> SensorArcs(this Controller controller) 
        { 
            if(controller == null)
            {
                return null;
            }

            List<SensorArc> result = new List<SensorArc>();

            int count = controller.GetSensorArcCount();
            for (int i = 1; i <= count; i++)
            {
                SensorArc sensorArc = controller.GetSensorArc(i);
                if (sensorArc == null)
                {
                    continue;
                }

                result.Add(sensorArc);
            }

            return result;
        }
    }
}