using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCHPResult ToSAM_SystemCHPResult(this CHP cHP, int start, int end, params CHPDataType[] cHPDataTypes)
        {
            if (cHP == null)
            {
                return null;
            }

            IEnumerable<CHPDataType> cHPDataTypes_Temp = cHPDataTypes == null || cHPDataTypes.Length == 0 ? System.Enum.GetValues(typeof(CHPDataType)).Cast<CHPDataType>() : cHPDataTypes;

            Dictionary<CHPDataType, IndexedDoubles> dictionary = new Dictionary<CHPDataType, IndexedDoubles>();
            foreach (CHPDataType cHPDataType in cHPDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)cHP, cHPDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(cHPDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[cHPDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)cHP);

            SystemCHPResult result = new SystemCHPResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
