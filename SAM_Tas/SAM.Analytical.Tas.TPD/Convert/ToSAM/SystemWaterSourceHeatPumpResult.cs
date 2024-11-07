using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceHeatPumpResult ToSAM_SystemWaterSourceHeatPumpResult(this HeatPump heatPump, int start, int end, params WaterSourceHeatPumpDataType[] waterSourcePumpDataTypes)
        {
            if (heatPump == null)
            {
                return null;
            }

            IEnumerable<WaterSourceHeatPumpDataType> waterSourceHeatPumpDataTypes_Temp = waterSourcePumpDataTypes == null || waterSourcePumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterSourceHeatPumpDataType)).Cast<WaterSourceHeatPumpDataType>() : waterSourcePumpDataTypes;

            Dictionary<WaterSourceHeatPumpDataType, IndexedDoubles> dictionary = new Dictionary<WaterSourceHeatPumpDataType, IndexedDoubles>();
            foreach (WaterSourceHeatPumpDataType waterSourceHeatPumpDataType in waterSourceHeatPumpDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)heatPump, waterSourceHeatPumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterSourceHeatPumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterSourceHeatPumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)heatPump);

            SystemWaterSourceHeatPumpResult result = new SystemWaterSourceHeatPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
