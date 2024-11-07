using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPumpResult ToSAM_SystemPumpResult(this Pump pump, int start, int end, params PumpDataType[] pumpDataTypes)
        {
            if (pump == null)
            {
                return null;
            }

            IEnumerable<PumpDataType> pumpDataTypes_Temp = pumpDataTypes == null || pumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(PumpDataType)).Cast<PumpDataType>() : pumpDataTypes;

            Dictionary<PumpDataType, IndexedDoubles> dictionary = new Dictionary<PumpDataType, IndexedDoubles>();
            foreach (PumpDataType pumpDataType in pumpDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)pump, pumpDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(pumpDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[pumpDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)pump);

            SystemPumpResult result = new SystemPumpResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
