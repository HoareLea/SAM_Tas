using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatingSystemCollectionResult ToSAM_HeatingSystemCollectionResult(this HeatingGroup heatingGroup, int start, int end, params HeatingSystemCollectionDataType[] heatingSystemCollectionDataTypes)
        {
            if (heatingGroup == null)
            {
                return null;
            }

            IEnumerable<HeatingSystemCollectionDataType> heatingSystemCollectionDataTypes_Temp = heatingSystemCollectionDataTypes == null || heatingSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(HeatingSystemCollectionDataType)).Cast<HeatingSystemCollectionDataType>() : heatingSystemCollectionDataTypes;

            Dictionary<HeatingSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<HeatingSystemCollectionDataType, IndexedDoubles>();
            foreach (HeatingSystemCollectionDataType heatingSystemCollectionDataType in heatingSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)heatingGroup, heatingSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(heatingSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[heatingSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)heatingGroup);

            HeatingSystemCollectionResult result = new HeatingSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



