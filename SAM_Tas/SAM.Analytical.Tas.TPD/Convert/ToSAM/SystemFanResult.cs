using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFanResult ToSAM_SystemFanResult(this global::TPD.Fan fan, int start, int end, params FanDataType[] fanDataTypes)
        {
            if (fan == null)
            {
                return null;
            }

            IEnumerable<FanDataType> fanDataTypes_Temp = fanDataTypes == null || fanDataTypes.Length == 0 ? System.Enum.GetValues(typeof(FanDataType)).Cast<FanDataType>() : fanDataTypes;

            Dictionary<FanDataType, IndexedDoubles> dictionary = new Dictionary<FanDataType, IndexedDoubles>();
            foreach (FanDataType fanDataType in fanDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)fan, fanDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(fanDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[fanDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)fan);

            SystemFanResult result = new SystemFanResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
