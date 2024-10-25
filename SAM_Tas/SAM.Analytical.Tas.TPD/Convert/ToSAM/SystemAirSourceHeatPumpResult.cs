using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirSourceHeatPumpResult ToSAM_SystemAirSourceHeatPumpResult(this AirSourceHeatPump airSourceHeatPump, int start, int end, params AirSourceHeatPumpDataType[] airSourcePumpDataTypes)
        {
            if (airSourceHeatPump == null)
            {
                return null;
            }

            IEnumerable<AirSourceHeatPumpDataType> airSourceHeatPumpDataTypes_Temp = airSourcePumpDataTypes == null || airSourcePumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(AirSourceHeatPumpDataType)).Cast<AirSourceHeatPumpDataType>() : airSourcePumpDataTypes;

            Dictionary<AirSourceHeatPumpDataType, IndexedDoubles> dictionary = new Dictionary<AirSourceHeatPumpDataType, IndexedDoubles>();
            foreach (AirSourceHeatPumpDataType airSourceHeatPumpDataType in airSourceHeatPumpDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)airSourceHeatPump, airSourceHeatPumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(airSourceHeatPumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[airSourceHeatPumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)airSourceHeatPump);

            SystemAirSourceHeatPumpResult result = new SystemAirSourceHeatPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
