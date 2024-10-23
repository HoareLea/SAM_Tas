using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatPumpResult ToSAM_SystemHeatPumpResult(this HeatPump heatPump, int start, int end, params HeatPumpDataType[] pumpDataTypes)
        {
            if (heatPump == null)
            {
                return null;
            }

            IEnumerable<HeatPumpDataType> heatPumpDataTypes_Temp = pumpDataTypes == null || pumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(HeatPumpDataType)).Cast<HeatPumpDataType>() : pumpDataTypes;

            Dictionary<HeatPumpDataType, IndexedDoubles> dictionary = new Dictionary<HeatPumpDataType, IndexedDoubles>();
            foreach (HeatPumpDataType heatPumpDataType in heatPumpDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)heatPump, heatPumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(heatPumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[heatPumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)heatPump);

            SystemHeatPumpResult result = new SystemHeatPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
