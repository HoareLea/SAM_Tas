using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDesiccantWheelResult ToSAM_SystemDesiccantWheelResult(this DesiccantWheel desiccantWheel, int start, int end, params DesiccantWheelDataType[] desiccantWheelDataTypes)
        {
            if (desiccantWheel == null)
            {
                return null;
            }

            IEnumerable<DesiccantWheelDataType> desiccantWheelDataType_Temp = desiccantWheelDataTypes == null || desiccantWheelDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DesiccantWheelDataType)).Cast<DesiccantWheelDataType>() : desiccantWheelDataTypes;

            Dictionary<DesiccantWheelDataType, IndexedDoubles> dictionary = new Dictionary<DesiccantWheelDataType, IndexedDoubles>();
            foreach (DesiccantWheelDataType desiccantWheelDataType in desiccantWheelDataType_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)desiccantWheel, desiccantWheelDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(desiccantWheelDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[desiccantWheelDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)desiccantWheel);

            SystemDesiccantWheelResult result = new SystemDesiccantWheelResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
