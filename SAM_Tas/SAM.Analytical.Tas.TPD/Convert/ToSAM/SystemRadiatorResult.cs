using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemRadiatorResult ToSAM_SystemRadiatorResult(this Radiator radiator, int start, int end, params RadiatorDataType[] radiatorDataTypes)
        {
            if (radiator == null)
            {
                return null;
            }

            IEnumerable<RadiatorDataType> radiatorDataTypes_Temp = radiatorDataTypes == null || radiatorDataTypes.Length == 0 ? System.Enum.GetValues(typeof(RadiatorDataType)).Cast<RadiatorDataType>() : radiatorDataTypes;


            Dictionary<RadiatorDataType, IndexedDoubles> dictionary = new Dictionary<RadiatorDataType, IndexedDoubles>();
            foreach (RadiatorDataType radiatorDataType in radiatorDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((ZoneComponent)radiator, radiatorDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(radiatorDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[radiatorDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((ZoneComponent)radiator);


            SystemRadiatorResult result = new SystemRadiatorResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
