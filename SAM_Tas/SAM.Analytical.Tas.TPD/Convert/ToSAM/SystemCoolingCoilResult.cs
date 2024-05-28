using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingCoilResult ToSAM_SystemCoolingCoilResult(this global::TPD.CoolingCoil coolingCoil, int start, int end, params CoolingCoilDataType[] coolingCoilDataTypes)
        {
            if (coolingCoil == null)
            {
                return null;
            }

            IEnumerable<CoolingCoilDataType> coolingCoilDataTypes_Temp = coolingCoilDataTypes == null || coolingCoilDataTypes.Length == 0 ? System.Enum.GetValues(typeof(CoolingCoilDataType)).Cast<CoolingCoilDataType>() : coolingCoilDataTypes;

            Dictionary<CoolingCoilDataType, IndexedDoubles> dictionary = new Dictionary<CoolingCoilDataType, IndexedDoubles>();
            foreach (CoolingCoilDataType coolingCoilDataType in coolingCoilDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)coolingCoil, coolingCoilDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(coolingCoilDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[coolingCoilDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)coolingCoil);

            SystemCoolingCoilResult result = new SystemCoolingCoilResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
