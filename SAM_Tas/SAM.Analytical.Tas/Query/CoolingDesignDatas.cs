using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TSD.CoolingDesignData> CoolingDesignDatas(this TSD.SimulationData simulationData)
        {
            List<TSD.CoolingDesignData> result = new List<TSD.CoolingDesignData>();

            int index = 0;
            TSD.CoolingDesignData coolingDesignData = simulationData.GetCoolingDesignData(index);
            while (coolingDesignData != null)
            {
                result.Add(coolingDesignData);
                index++;

                coolingDesignData = simulationData.GetCoolingDesignData(index);
            }

            return result;
        }
    }
}