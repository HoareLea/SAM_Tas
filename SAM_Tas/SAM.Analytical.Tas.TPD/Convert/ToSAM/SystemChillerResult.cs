using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChillerResult ToSAM_SystemChillerResult(this Chiller chiller, int start, int end, params ChillerDataType[] chillerDataTypes)
        {
            if (chiller == null)
            {
                return null;
            }

            IEnumerable<ChillerDataType> chillerDataTypes_Temp = chillerDataTypes == null || chillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ChillerDataType)).Cast<ChillerDataType>() : chillerDataTypes;

            Dictionary<ChillerDataType, IndexedDoubles> dictionary = new Dictionary<ChillerDataType, IndexedDoubles>();
            foreach (ChillerDataType chillerDataType in chillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)chiller, chillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(chillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[chillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)chiller);

            SystemChillerResult result = new SystemChillerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }

        public static SystemChillerResult ToSAM_SystemChillerResult(this MultiChiller multiChiller, int start, int end, params ChillerDataType[] chillerDataTypes)
        {
            if (multiChiller == null)
            {
                return null;
            }

            IEnumerable<ChillerDataType> chillerDataTypes_Temp = chillerDataTypes == null || chillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ChillerDataType)).Cast<ChillerDataType>() : chillerDataTypes;

            Dictionary<ChillerDataType, IndexedDoubles> dictionary = new Dictionary<ChillerDataType, IndexedDoubles>();
            foreach (ChillerDataType chillerDataType in chillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)multiChiller, chillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(chillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[chillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)multiChiller);

            SystemChillerResult result = new SystemChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}
