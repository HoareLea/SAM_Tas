using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FuelSystemCollectionResult ToSAM_FuelSystemCollectionResult(this FuelGroup fuelGroup, int start, int end, params FuelSystemCollectionDataType[] fuelSystemCollectionDataTypes)
        {
            if (fuelGroup == null)
            {
                return null;
            }

            IEnumerable<FuelSystemCollectionDataType> fuelSystemCollectionDataTypes_Temp = fuelSystemCollectionDataTypes == null || fuelSystemCollectionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(FuelSystemCollectionDataType)).Cast<FuelSystemCollectionDataType>() : fuelSystemCollectionDataTypes;

            Dictionary<FuelSystemCollectionDataType, IndexedDoubles> dictionary = new Dictionary<FuelSystemCollectionDataType, IndexedDoubles>();
            foreach (FuelSystemCollectionDataType fuelSystemCollectionDataType in fuelSystemCollectionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)fuelGroup, fuelSystemCollectionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(fuelSystemCollectionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[fuelSystemCollectionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)fuelGroup);

            FuelSystemCollectionResult result = new FuelSystemCollectionResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



