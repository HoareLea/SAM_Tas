using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceDirectAbsorptionChillerResult ToSAM_SystemWaterSourceDirectAbsorptionChillerResult(this WaterSourceChiller waterSourceChiller, int start, int end, params WaterSourceDirectAbsorptionChillerDataType[] waterSourceDirectAbsorptionChillerDataTypes)
        {
            if (waterSourceChiller == null)
            {
                return null;
            }

            IEnumerable<WaterSourceDirectAbsorptionChillerDataType> waterSourceDirectAbsorptionChillerDataTypes_Temp = waterSourceDirectAbsorptionChillerDataTypes == null || waterSourceDirectAbsorptionChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterSourceDirectAbsorptionChillerDataType)).Cast<WaterSourceDirectAbsorptionChillerDataType>() : waterSourceDirectAbsorptionChillerDataTypes;

            Dictionary<WaterSourceDirectAbsorptionChillerDataType, IndexedDoubles> dictionary = new Dictionary<WaterSourceDirectAbsorptionChillerDataType, IndexedDoubles>();
            foreach (WaterSourceDirectAbsorptionChillerDataType waterSourceDirectAbsorptionChillerDataType in waterSourceDirectAbsorptionChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)waterSourceChiller, waterSourceDirectAbsorptionChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterSourceDirectAbsorptionChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterSourceDirectAbsorptionChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)waterSourceChiller);

            SystemWaterSourceDirectAbsorptionChillerResult result = new SystemWaterSourceDirectAbsorptionChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

