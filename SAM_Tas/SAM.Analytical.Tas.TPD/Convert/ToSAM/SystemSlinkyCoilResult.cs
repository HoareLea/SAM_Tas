using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSlinkyCoilResult ToSAM_SystemSlinkyCoilResult(this SlinkyCoil slinkyCoil, int start, int end, params SlinkyCoilDataType[] slinkyCoilDataTypes)
        {
            if (slinkyCoil == null)
            {
                return null;
            }

            IEnumerable<SlinkyCoilDataType> slinkyCoilDataTypes_Temp = slinkyCoilDataTypes == null || slinkyCoilDataTypes.Length == 0 ? System.Enum.GetValues(typeof(SlinkyCoilDataType)).Cast<SlinkyCoilDataType>() : slinkyCoilDataTypes;

            Dictionary<SlinkyCoilDataType, IndexedDoubles> dictionary = new Dictionary<SlinkyCoilDataType, IndexedDoubles>();
            foreach (SlinkyCoilDataType slinkyCoilDataType in slinkyCoilDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)slinkyCoil, slinkyCoilDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(slinkyCoilDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[slinkyCoilDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)slinkyCoil);

            SystemSlinkyCoilResult result = new SystemSlinkyCoilResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
