using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirJunctionResult ToSAM_SystemAirJunctionResult(this Junction junction, int start, int end, params AirJunctionDataType[] airJunctionDataTypes)
        {
            if (junction == null)
            {
                return null;
            }

            IEnumerable<AirJunctionDataType> airJunctionDataTypes_Temp = airJunctionDataTypes == null || airJunctionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(AirJunctionDataType)).Cast<AirJunctionDataType>() : airJunctionDataTypes;

            Dictionary<AirJunctionDataType, IndexedDoubles> dictionary = new Dictionary<AirJunctionDataType, IndexedDoubles>();
            foreach (AirJunctionDataType airJunctionDataType in airJunctionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)junction, airJunctionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(airJunctionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[airJunctionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)junction);

            SystemAirJunctionResult result = new SystemAirJunctionResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
