using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ElectricalSystemCollectionResult ToSAM_ElectricalSystemCollectionResult(this ElectricalGroup electricalGroup, int start, int end, params ElectricalSystemCollectionDataType[] electricalSystemCollectionDataTypes)
        {
            if (electricalGroup == null)
            {
                return null;
            }

            IEnumerable<ElectricalSystemCollectionDataType> electricalSystemCollectionDataTypes_Temp = electricalSystemCollectionDataTypes == null || electricalSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ElectricalSystemCollectionDataType)).Cast<ElectricalSystemCollectionDataType>() : electricalSystemCollectionDataTypes;

            Dictionary<ElectricalSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<ElectricalSystemCollectionDataType, IndexedDoubles>();
            foreach (ElectricalSystemCollectionDataType electricalSystemCollectionDataType in electricalSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)electricalGroup, electricalSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(electricalSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[electricalSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)electricalGroup);

            ElectricalSystemCollectionResult result = new ElectricalSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



