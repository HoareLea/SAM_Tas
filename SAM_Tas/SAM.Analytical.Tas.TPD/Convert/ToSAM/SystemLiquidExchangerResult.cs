using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLiquidExchangerResult ToSAM_SystemLiquidExchangerResult(this HeatExchanger heatExchanger, int start, int end, params LiquidExchangerDataType[] liquidExchangerDataTypes)
        {
            if (heatExchanger == null)
            {
                return null;
            }

            IEnumerable<LiquidExchangerDataType> liquidExchangerDataTypes_Temp = liquidExchangerDataTypes == null || liquidExchangerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(LiquidExchangerDataType)).Cast<LiquidExchangerDataType>() : liquidExchangerDataTypes;

            Dictionary<LiquidExchangerDataType, IndexedDoubles> dictionary = new Dictionary<LiquidExchangerDataType, IndexedDoubles>();
            foreach (LiquidExchangerDataType liquidExchangerDataType in liquidExchangerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)heatExchanger, liquidExchangerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(liquidExchangerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[liquidExchangerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)heatExchanger);

            SystemLiquidExchangerResult result = new SystemLiquidExchangerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

