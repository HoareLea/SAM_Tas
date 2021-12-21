using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TSD.HeatingDesignData> HeatingDesignDatas(this TSD.SimulationData simulationData)
        {
            List<TSD.HeatingDesignData> result = new List<TSD.HeatingDesignData>();

            int index = 0;
            TSD.HeatingDesignData heatingDesignData = simulationData.GetHeatingDesignData(index);
            while (heatingDesignData != null)
            {
                result.Add(heatingDesignData);
                index++;

                heatingDesignData = simulationData.GetHeatingDesignData(index);
            }

            return result;
        }
    }
}