using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DomesticHotWaterSystemCollectionResult ToSAM_DomesticHotWaterSystemCollectionResult(this DHWGroup dHWGroup, int start, int end, params DomesticHotWaterSystemCollectionDataType[] domesticHotWaterSystemCollectionDataTypes)
        {
            if (dHWGroup == null)
            {
                return null;
            }

            IEnumerable<DomesticHotWaterSystemCollectionDataType> domesticHotWaterSystemCollectionDataTypes_Temp = domesticHotWaterSystemCollectionDataTypes == null || domesticHotWaterSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DomesticHotWaterSystemCollectionDataType)).Cast<DomesticHotWaterSystemCollectionDataType>() : domesticHotWaterSystemCollectionDataTypes;

            Dictionary<DomesticHotWaterSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<DomesticHotWaterSystemCollectionDataType, IndexedDoubles>();
            foreach (DomesticHotWaterSystemCollectionDataType domesticHotWaterSystemCollectionDataType in domesticHotWaterSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)dHWGroup, domesticHotWaterSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(domesticHotWaterSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[domesticHotWaterSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)dHWGroup);

            DomesticHotWaterSystemCollectionResult result = new DomesticHotWaterSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



