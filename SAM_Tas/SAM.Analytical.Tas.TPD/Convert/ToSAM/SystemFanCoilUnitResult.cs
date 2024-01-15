using SAM.Analytical.Systems;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFanCoilUnitResult ToSAM_SystemFanCoilUnitResult(this FanCoilUnit fanCoilUnit, int start, int end, params FanCoilUnitDataType[] fanCoilUnitDataTypes)
        {
            if (fanCoilUnit == null)
            {
                return null;
            }

            IEnumerable<FanCoilUnitDataType> fanCoilUnitDataTypes_Temp = fanCoilUnitDataTypes == null || fanCoilUnitDataTypes.Length == 0 ? Enum.GetValues(typeof(FanCoilUnitDataType)).Cast<FanCoilUnitDataType>() : fanCoilUnitDataTypes;


            Dictionary<FanCoilUnitDataType, IndexedDoubles> dictionary = new Dictionary<FanCoilUnitDataType, IndexedDoubles>();
            foreach (FanCoilUnitDataType fanCoilUnitDataType in fanCoilUnitDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((ZoneComponent)fanCoilUnit, fanCoilUnitDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(fanCoilUnitDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[fanCoilUnitDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((ZoneComponent)fanCoilUnit);


            SystemFanCoilUnitResult result = new SystemFanCoilUnitResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
