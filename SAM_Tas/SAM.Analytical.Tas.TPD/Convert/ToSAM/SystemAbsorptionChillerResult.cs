using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAbsorptionChillerResult ToSAM_SystemAbsorptionChillerResult(this AbsorptionChiller absorptionChiller, int start, int end, params AbsorptionChillerDataType[] absorptionChillerDataTypes)
        {
            if (absorptionChiller == null)
            {
                return null;
            }

            IEnumerable<AbsorptionChillerDataType> absorptionChillerDataTypes_Temp = absorptionChillerDataTypes == null || absorptionChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(AbsorptionChillerDataType)).Cast<AbsorptionChillerDataType>() : absorptionChillerDataTypes;

            Dictionary<AbsorptionChillerDataType, IndexedDoubles> dictionary = new Dictionary<AbsorptionChillerDataType, IndexedDoubles>();
            foreach (AbsorptionChillerDataType absorptionChillerDataType in absorptionChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)absorptionChiller, absorptionChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(absorptionChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[absorptionChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)absorptionChiller);

            SystemAbsorptionChillerResult result = new SystemAbsorptionChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



