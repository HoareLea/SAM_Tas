using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static RefrigerantSystemCollectionResult ToSAM_RefrigerantSystemCollectionResult(this RefrigerantGroup refrigerantGroup, int start, int end, params RefrigerantSystemCollectionDataType[] refrigerantSystemCollectionDataTypes)
        {
            if (refrigerantGroup == null)
            {
                return null;
            }

            IEnumerable<RefrigerantSystemCollectionDataType> refrigerantSystemCollectionDataTypes_Temp = refrigerantSystemCollectionDataTypes == null || refrigerantSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(RefrigerantSystemCollectionDataType)).Cast<RefrigerantSystemCollectionDataType>() : refrigerantSystemCollectionDataTypes;

            Dictionary<RefrigerantSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<RefrigerantSystemCollectionDataType, IndexedDoubles>();
            foreach (RefrigerantSystemCollectionDataType refrigerantSystemCollectionDataType in refrigerantSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)refrigerantGroup, refrigerantSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(refrigerantSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[refrigerantSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)refrigerantGroup);

            RefrigerantSystemCollectionResult result = new RefrigerantSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



