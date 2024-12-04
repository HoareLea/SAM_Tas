using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiBoilerResult ToSAM_SystemMultiBoilerResult(this MultiBoiler multiBolier, int start, int end, params MultiBoilerDataType[] multiBoilerDataTypes)
        {
            if (multiBolier == null)
            {
                return null;
            }

            IEnumerable<MultiBoilerDataType> multiBoilerDataTypes_Temp = multiBoilerDataTypes;
            if(multiBoilerDataTypes == null || multiBoilerDataTypes.Length == 0)
            {
                Range<int> range = new Range<int>(multiBolier.Multiplicity + 1, 10);
                List<int> values = new List<int>();
                for(int i = range.Min; i <= range.Max; i++)
                {
                    values.Add(i);
                }

                List<MultiBoilerDataType> multiBoilerDataTypes_Temp_Temp = new List<MultiBoilerDataType>();
                foreach (MultiBoilerDataType multiChillerDataType in System.Enum.GetValues(typeof(MultiBoilerDataType)))
                {
                    if(values.FindIndex(x => multiChillerDataType.ToString().EndsWith(x.ToString())) != -1)
                    {
                        continue;
                    }

                    multiBoilerDataTypes_Temp_Temp.Add(multiChillerDataType);
                }

                multiBoilerDataTypes_Temp = multiBoilerDataTypes_Temp_Temp;
            }

            Dictionary<MultiBoilerDataType, IndexedDoubles> dictionary = new Dictionary<MultiBoilerDataType, IndexedDoubles>();
            foreach (MultiBoilerDataType multiChillerDataType in multiBoilerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)multiBolier, multiChillerDataType, start, end);
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


            string reference = Query.Reference((PlantComponent)multiBolier);

            SystemMultiBoilerResult result = new SystemMultiBoilerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}