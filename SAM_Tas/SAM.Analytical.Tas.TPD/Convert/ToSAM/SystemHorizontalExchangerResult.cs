using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHorizontalExchangerResult ToSAM_SystemHorizontalExchangerResult(this HorizontalGHE horizontalGHE, int start, int end, params HorizontalExchangerDataType[] horizontalExchangerDataTypes)
        {
            if (horizontalGHE == null)
            {
                return null;
            }

            IEnumerable<HorizontalExchangerDataType> horizontalExchangerDataTypes_Temp = horizontalExchangerDataTypes == null || horizontalExchangerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(HorizontalExchangerDataType)).Cast<HorizontalExchangerDataType>() : horizontalExchangerDataTypes;

            Dictionary<HorizontalExchangerDataType, IndexedDoubles> dictionary = new Dictionary<HorizontalExchangerDataType, IndexedDoubles>();
            foreach (HorizontalExchangerDataType horizontalExchangerDataType in horizontalExchangerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)horizontalGHE, horizontalExchangerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(horizontalExchangerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[horizontalExchangerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)horizontalGHE);

            SystemHorizontalExchangerResult result = new SystemHorizontalExchangerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
