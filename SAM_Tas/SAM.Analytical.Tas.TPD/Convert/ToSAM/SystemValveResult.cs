using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemValveResult ToSAM_SystemValveResult(this Valve valve, int start, int end, params ValveDataType[] valveDataTypes)
        {
            if (valve == null)
            {
                return null;
            }

            IEnumerable<ValveDataType> valveDataTypes_Temp = valveDataTypes == null || valveDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ValveDataType)).Cast<ValveDataType>() : valveDataTypes;

            Dictionary<ValveDataType, IndexedDoubles> dictionary = new Dictionary<ValveDataType, IndexedDoubles>();
            foreach (ValveDataType valveDataType in valveDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)valve, valveDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(valveDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[valveDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)valve);

            SystemValveResult result = new SystemValveResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
