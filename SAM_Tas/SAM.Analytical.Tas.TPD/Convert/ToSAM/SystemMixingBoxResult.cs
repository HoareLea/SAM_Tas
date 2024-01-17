using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMixingBoxResult ToSAM_SystemMixingBoxResult(this Optimiser optimiser, int start, int end, params MixingBoxDataType[] mixingBoxDataTypes)
        {
            if (optimiser == null)
            {
                return null;
            }

            IEnumerable<MixingBoxDataType> mixingBoxDataTypes_Temp = mixingBoxDataTypes == null || mixingBoxDataTypes.Length == 0 ? System.Enum.GetValues(typeof(MixingBoxDataType)).Cast<MixingBoxDataType>() : mixingBoxDataTypes;

            Dictionary<MixingBoxDataType, IndexedDoubles> dictionary = new Dictionary<MixingBoxDataType, IndexedDoubles>();
            foreach (MixingBoxDataType mixingBoxDataType in mixingBoxDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)optimiser, mixingBoxDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(mixingBoxDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[mixingBoxDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)optimiser);

            SystemMixingBoxResult result = new SystemMixingBoxResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
