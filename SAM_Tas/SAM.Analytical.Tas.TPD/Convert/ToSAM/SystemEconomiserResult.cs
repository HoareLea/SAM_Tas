using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEconomiserResult ToSAM_SystemEconomiserResult(this Optimiser optimiser, int start, int end, params EconomiserDataType[] economiserDataTypes)
        {
            if (optimiser == null)
            {
                return null;
            }

            IEnumerable<EconomiserDataType> economiserDataTypes_Temp = economiserDataTypes == null || economiserDataTypes.Length == 0 ? System.Enum.GetValues(typeof(EconomiserDataType)).Cast<EconomiserDataType>() : economiserDataTypes;

            Dictionary<EconomiserDataType, IndexedDoubles> dictionary = new Dictionary<EconomiserDataType, IndexedDoubles>();
            foreach (EconomiserDataType economiserDataType in economiserDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)optimiser, economiserDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(economiserDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[economiserDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)optimiser);

            SystemEconomiserResult result = new SystemEconomiserResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
