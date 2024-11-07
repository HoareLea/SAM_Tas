using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantSensorArc> PlantSensorArcs(this PlantController plantController) 
        { 
            if(plantController == null)
            {
                return null;
            }

            List<PlantSensorArc> result = new List<PlantSensorArc>();

            int count = plantController.GetSensorArcCount();
            for (int i = 1; i <= count; i++)
            {
                PlantSensorArc sensorArc = plantController.GetSensorArc(i);
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