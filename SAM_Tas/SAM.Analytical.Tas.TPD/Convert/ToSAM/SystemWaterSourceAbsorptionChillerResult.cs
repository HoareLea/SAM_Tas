using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceAbsorptionChillerResult ToSAM_SystemWaterSourceAbsorptionChillerResult(this AbsorptionChiller absorptionChiller, int start, int end, params WaterSourceAbsorptionChillerDataType[] waterSourceAbsorptionChillerDataTypes)
        {
            if (absorptionChiller == null)
            {
                return null;
            }

            IEnumerable<WaterSourceAbsorptionChillerDataType> waterSourceAbsorptionChillerDataTypes_Temp = waterSourceAbsorptionChillerDataTypes == null || waterSourceAbsorptionChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WaterSourceAbsorptionChillerDataType)).Cast<WaterSourceAbsorptionChillerDataType>() : waterSourceAbsorptionChillerDataTypes;

            Dictionary<WaterSourceAbsorptionChillerDataType, IndexedDoubles> dictionary = new Dictionary<WaterSourceAbsorptionChillerDataType, IndexedDoubles>();
            foreach (WaterSourceAbsorptionChillerDataType waterSourceAbsorptionChillerDataType in waterSourceAbsorptionChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)absorptionChiller, waterSourceAbsorptionChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(waterSourceAbsorptionChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[waterSourceAbsorptionChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)absorptionChiller);

            SystemWaterSourceAbsorptionChillerResult result = new SystemWaterSourceAbsorptionChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}