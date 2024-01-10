using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoilUnitResult ToSAM_SystemDXCoilUnitResult(this DXCoilUnit dXCoilUnit, int start, int end, params DXCoilUnitDataType[] dXCoilUnitDataTypes)
        {
            if (dXCoilUnit == null)
            {
                return null;
            }

            IEnumerable<DXCoilUnitDataType> dXCoilUnitDataTypes_Temp = dXCoilUnitDataTypes == null || dXCoilUnitDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DXCoilUnitDataType)).Cast<DXCoilUnitDataType>() : dXCoilUnitDataTypes;


            Dictionary<DXCoilUnitDataType, IndexedDoubles> dictionary = new Dictionary<DXCoilUnitDataType, IndexedDoubles>();
            foreach (DXCoilUnitDataType dXCoilUnitDataType in dXCoilUnitDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((ZoneComponent)dXCoilUnit, dXCoilUnitDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(dXCoilUnitDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[dXCoilUnitDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((ZoneComponent)dXCoilUnit);

            SystemDXCoilUnitResult result = new SystemDXCoilUnitResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
