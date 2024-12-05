using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingSystemCollectionResult ToSAM_CoolingSystemCollectionResult(this CoolingGroup coolingGroup, int start, int end, params CoolingSystemCollectionDataType[] coolingSystemCollectionDataTypes)
        {
            if (coolingGroup == null)
            {
                return null;
            }

            IEnumerable<CoolingSystemCollectionDataType> coolingSystemCollectionDataTypes_Temp = coolingSystemCollectionDataTypes == null || coolingSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(CoolingSystemCollectionDataType)).Cast<CoolingSystemCollectionDataType>() : coolingSystemCollectionDataTypes;

            Dictionary<CoolingSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<CoolingSystemCollectionDataType, IndexedDoubles>();
            foreach (CoolingSystemCollectionDataType coolingSystemCollectionDataType in coolingSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)coolingGroup, coolingSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(coolingSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[coolingSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)coolingGroup);

            CoolingSystemCollectionResult result = new CoolingSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



