using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceChillerResult ToSAM_SystemWaterSourceChillerResult(this WaterSourceChiller waterSourceChiller, int start, int end, params WaterSourceChillerDataType[] waterSourceChillerDataTypes)
        {
            if (waterSourceChiller == null)
            {
                return null;
            }

            IEnumerable<WaterSourceChillerDataType> waterSourceChillerDataTypes_Temp = waterSourceChillerDataTypes == null || waterSourceChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterSourceChillerDataType)).Cast<WaterSourceChillerDataType>() : waterSourceChillerDataTypes;

            Dictionary<WaterSourceChillerDataType, IndexedDoubles> dictionary = new Dictionary<WaterSourceChillerDataType, IndexedDoubles>();
            foreach (WaterSourceChillerDataType waterSourceChillerDataType in waterSourceChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)waterSourceChiller, waterSourceChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterSourceChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterSourceChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)waterSourceChiller);

            SystemWaterSourceChillerResult result = new SystemWaterSourceChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

