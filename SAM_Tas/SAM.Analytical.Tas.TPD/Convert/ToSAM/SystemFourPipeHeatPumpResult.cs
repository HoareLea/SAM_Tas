using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFourPipeHeatPumpResult ToSAM_SystemFourPipeHeatPumpResult(this FourPipeHeatPump fourPipeHeatPump, int start, int end, params FourPipeHeatPumpDataType[] fourPipeHeatPumpDataTypes)
        {
            if (fourPipeHeatPump == null)
            {
                return null;
            }

            IEnumerable<FourPipeHeatPumpDataType> fourPipeHeatPumpDataTypes_Temp = fourPipeHeatPumpDataTypes == null || fourPipeHeatPumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(FourPipeHeatPumpDataType)).Cast<FourPipeHeatPumpDataType>() : fourPipeHeatPumpDataTypes;

            Dictionary<FourPipeHeatPumpDataType, IndexedDoubles> dictionary = new Dictionary<FourPipeHeatPumpDataType, IndexedDoubles>();
            foreach (FourPipeHeatPumpDataType fourPipeHeatPumpDataType in fourPipeHeatPumpDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)fourPipeHeatPump, fourPipeHeatPumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(fourPipeHeatPumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[fourPipeHeatPumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)fourPipeHeatPump);

            SystemFourPipeHeatPumpResult result = new SystemFourPipeHeatPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
