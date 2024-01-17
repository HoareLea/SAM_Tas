using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSprayHumidifierResult ToSAM_SystemSprayHumidifierResult(this SprayHumidifier sprayHumidifier, int start, int end, params SprayHumidifierDataType[] sprayHumidifierDataTypes)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            IEnumerable<SprayHumidifierDataType> sprayHumidifierDataTypes_Temp = sprayHumidifierDataTypes == null || sprayHumidifierDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SprayHumidifierDataType)).Cast<SprayHumidifierDataType>() : sprayHumidifierDataTypes;

            Dictionary<SprayHumidifierDataType, IndexedDoubles> dictionary = new Dictionary<SprayHumidifierDataType, IndexedDoubles>();
            foreach (SprayHumidifierDataType sprayHumidifierDataType in sprayHumidifierDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)sprayHumidifier, sprayHumidifierDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(sprayHumidifierDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[sprayHumidifierDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)sprayHumidifier);

            SystemSprayHumidifierResult result = new SystemSprayHumidifierResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
