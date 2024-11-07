using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSurfaceWaterExchangerResult ToSAM_SystemSurfaceWaterExchangerResult(this SurfaceWaterExchanger surfaceWaterExchanger, int start, int end, params SurfaceWaterExchangerDataType[] surfaceWaterExchangerDataTypes)
        {
            if (surfaceWaterExchanger == null)
            {
                return null;
            }

            IEnumerable<SurfaceWaterExchangerDataType> surfaceWaterExchangerDataTypes_Temp = surfaceWaterExchangerDataTypes == null || surfaceWaterExchangerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SurfaceWaterExchangerDataType)).Cast<SurfaceWaterExchangerDataType>() : surfaceWaterExchangerDataTypes;

            Dictionary<SurfaceWaterExchangerDataType, IndexedDoubles> dictionary = new Dictionary<SurfaceWaterExchangerDataType, IndexedDoubles>();
            foreach (SurfaceWaterExchangerDataType surfaceWaterExchangerDataType in surfaceWaterExchangerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)surfaceWaterExchanger, surfaceWaterExchangerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(surfaceWaterExchangerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[surfaceWaterExchangerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)surfaceWaterExchanger);

            SystemSurfaceWaterExchangerResult result = new SystemSurfaceWaterExchangerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
