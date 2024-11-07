using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirSourceChillerResult ToSAM_SystemAirSourceChillerResult(this Chiller chiller, int start, int end, params AirSourceChillerDataType[] airSourceChillerDataTypes)
        {
            if (chiller == null)
            {
                return null;
            }

            IEnumerable<AirSourceChillerDataType> airSourceChillerDataTypes_Temp = airSourceChillerDataTypes == null || airSourceChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(AirSourceChillerDataType)).Cast<AirSourceChillerDataType>() : airSourceChillerDataTypes;

            Dictionary<AirSourceChillerDataType, IndexedDoubles> dictionary = new Dictionary<AirSourceChillerDataType, IndexedDoubles>();
            foreach (AirSourceChillerDataType airSourceChillerDataType in airSourceChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)chiller, airSourceChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(airSourceChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[airSourceChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)chiller);

            SystemAirSourceChillerResult result = new SystemAirSourceChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

