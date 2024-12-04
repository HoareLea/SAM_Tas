using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
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

            IEnumerable<MultiChillerDataType> multiChillerDataTypes_Temp = multiChillerDataTypes;
            if(multiChillerDataTypes == null || multiChillerDataTypes.Length == 0)
            {
                Range<int> range = new Range<int>(multiChiller.Multiplicity + 1, 10);
                List<int> values = new List<int>();
                for(int i = range.Min; i <= range.Max; i++)
                {
                    values.Add(i);
                }

                List<MultiChillerDataType> multiChillerDataTypes_Temp_Temp = new List<MultiChillerDataType>();
                foreach (MultiChillerDataType multiChillerDataType in System.Enum.GetValues(typeof(MultiChillerDataType)))
                {
                    if(values.FindIndex(x => multiChillerDataType.ToString().EndsWith(x.ToString())) != -1)
                    {
                        continue;
                    }

                    multiChillerDataTypes_Temp_Temp.Add(multiChillerDataType);
                }

                multiChillerDataTypes_Temp = multiChillerDataTypes_Temp_Temp;
            }

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