using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterToWaterHeatPumpResult ToSAM_SystemWaterToWaterHeatPumpResult(this WaterToWaterHeatPump waterToWaterHeatPump, int start, int end, params WaterToWaterHeatPumpDataType[] waterToWaterHeatPumpDataTypes)
        {
            if (waterToWaterHeatPump == null)
            {
                return null;
            }

            IEnumerable<WaterToWaterHeatPumpDataType> waterToWaterHeatPumpDataType_Temp = waterToWaterHeatPumpDataTypes == null || waterToWaterHeatPumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterToWaterHeatPumpDataType)).Cast<WaterToWaterHeatPumpDataType>() : waterToWaterHeatPumpDataTypes;

            Dictionary<WaterToWaterHeatPumpDataType, IndexedDoubles> dictionary = new Dictionary<WaterToWaterHeatPumpDataType, IndexedDoubles>();
            foreach (WaterToWaterHeatPumpDataType waterToWaterHeatPumpDataType in waterToWaterHeatPumpDataType_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)waterToWaterHeatPump, waterToWaterHeatPumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterToWaterHeatPumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterToWaterHeatPumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)waterToWaterHeatPump);

            SystemWaterToWaterHeatPumpResult result = new SystemWaterToWaterHeatPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
