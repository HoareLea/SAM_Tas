using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoilResult ToSAM_SystemDXCoilResult(this DXCoil dXCoil, int start, int end, params DXCoilDataType[] dXCoilDataTypes)
        {
            if (dXCoil == null)
            {
                return null;
            }

            IEnumerable<DXCoilDataType> dxCoilDataTypes_Temp = dXCoilDataTypes == null || dXCoilDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DXCoilDataType)).Cast<DXCoilDataType>() : dXCoilDataTypes;

            Dictionary<DXCoilDataType, IndexedDoubles> dictionary = new Dictionary<DXCoilDataType, IndexedDoubles>();
            foreach (DXCoilDataType dXCoilDataType in dxCoilDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)dXCoil, dXCoilDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(dXCoilDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[dXCoilDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)dXCoil);

            SystemDXCoilResult result = new SystemDXCoilResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
