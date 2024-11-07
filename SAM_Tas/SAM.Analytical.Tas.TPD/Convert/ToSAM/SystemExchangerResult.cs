using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemExchangerResult ToSAM_SystemExchangerResult(this Exchanger exchanger, int start, int end, params ExchangerDataType[] exchangerDataTypes)
        {
            if (exchanger == null)
            {
                return null;
            }

            IEnumerable<ExchangerDataType> exchangerDataTypes_Temp = exchangerDataTypes == null || exchangerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ExchangerDataType)).Cast<ExchangerDataType>() : exchangerDataTypes;

            Dictionary<ExchangerDataType, IndexedDoubles> dictionary = new Dictionary<ExchangerDataType, IndexedDoubles>();
            foreach (ExchangerDataType exchangerDataType in exchangerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)exchanger, exchangerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(exchangerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[exchangerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)exchanger);

            SystemExchangerResult result = new SystemExchangerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}