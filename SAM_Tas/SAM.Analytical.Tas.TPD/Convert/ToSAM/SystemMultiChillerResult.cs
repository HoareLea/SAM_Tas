using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiChillerResult ToSAM_SystemMultiChillerResult(this MultiChiller multiChiller, int start, int end, params MultiChillerDataType[] multiChillerDataTypes)
        {
            if (multiChiller == null)
            {
                return null;
            }

            IEnumerable<MultiChillerDataType> multiChillerDataTypes_Temp = multiChillerDataTypes == null || multiChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(MultiChillerDataType)).Cast<MultiChillerDataType>() : multiChillerDataTypes;

            Dictionary<MultiChillerDataType, IndexedDoubles> dictionary = new Dictionary<MultiChillerDataType, IndexedDoubles>();
            foreach (MultiChillerDataType multiChillerDataType in multiChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)multiChiller, multiChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(multiChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[multiChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)multiChiller);

            SystemMultiChillerResult result = new SystemMultiChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}