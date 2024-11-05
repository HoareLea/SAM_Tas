using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingTowerResult ToSAM_SystemCoolingTowerResult(this CoolingTower coolingTower, int start, int end, params CoolingTowerDataType[] coolingTowerDataTypes)
        {
            if (coolingTower == null)
            {
                return null;
            }

            IEnumerable<CoolingTowerDataType> coolingTowerDataTypes_Temp = coolingTowerDataTypes == null || coolingTowerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(CoolingTowerDataType)).Cast<CoolingTowerDataType>() : coolingTowerDataTypes;

            Dictionary<CoolingTowerDataType, IndexedDoubles> dictionary = new Dictionary<CoolingTowerDataType, IndexedDoubles>();
            foreach (CoolingTowerDataType coolingTowerDataType in coolingTowerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)coolingTower, coolingTowerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(coolingTowerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[coolingTowerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)coolingTower);

            SystemCoolingTowerResult result = new SystemCoolingTowerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
