using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSteamHumidifierResult ToSAM_SystemSteamHumidifierResult(this SteamHumidifier steamHumidifier, int start, int end, params SteamHumidifierDataType[] steamHumidifierDataTypes)
        {
            if (steamHumidifier == null)
            {
                return null;
            }

            IEnumerable<SteamHumidifierDataType> steamHumidifierDataTypes_Temp = steamHumidifierDataTypes == null || steamHumidifierDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SteamHumidifierDataType)).Cast<SteamHumidifierDataType>() : steamHumidifierDataTypes;

            Dictionary<SteamHumidifierDataType, IndexedDoubles> dictionary = new Dictionary<SteamHumidifierDataType, IndexedDoubles>();
            foreach (SteamHumidifierDataType steamHumidifierDataType in steamHumidifierDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)steamHumidifier, steamHumidifierDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(steamHumidifierDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[steamHumidifierDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)steamHumidifier);

            SystemSteamHumidifierResult result = new SystemSteamHumidifierResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
