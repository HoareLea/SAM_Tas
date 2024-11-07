using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceIceStorageChillerResult ToSAM_SystemWaterSourceIceStorageChillerResult(this IceStorageChiller iceStorageChiller, int start, int end, params WaterSourceIceStorageChillerDataType[] waterSourceIceStorageChillerDataTypes)
        {
            if (iceStorageChiller == null)
            {
                return null;
            }

            IEnumerable<WaterSourceIceStorageChillerDataType> waterSourceIceStorageChillerDataTypes_Temp = waterSourceIceStorageChillerDataTypes == null || waterSourceIceStorageChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterSourceIceStorageChillerDataType)).Cast<WaterSourceIceStorageChillerDataType>() : waterSourceIceStorageChillerDataTypes;

            Dictionary<WaterSourceIceStorageChillerDataType, IndexedDoubles> dictionary = new Dictionary<WaterSourceIceStorageChillerDataType, IndexedDoubles>();
            foreach (WaterSourceIceStorageChillerDataType waterSourceIceStorageChillerDataType in waterSourceIceStorageChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)iceStorageChiller, waterSourceIceStorageChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterSourceIceStorageChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterSourceIceStorageChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)iceStorageChiller);

            SystemWaterSourceIceStorageChillerResult result = new SystemWaterSourceIceStorageChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

