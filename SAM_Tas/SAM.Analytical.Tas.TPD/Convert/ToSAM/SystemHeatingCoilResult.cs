using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoilResult ToSAM_SystemHeatingCoilResult(this global::TPD.HeatingCoil heatingCoil, int start, int end, params HeatingCoilDataType[] heatingCoilDataTypes)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            IEnumerable<HeatingCoilDataType> heatingCoilDataTypes_Temp = heatingCoilDataTypes == null || heatingCoilDataTypes.Length == 0 ? System.Enum.GetValues(typeof(HeatingCoilDataType)).Cast<HeatingCoilDataType>() : heatingCoilDataTypes;

            Dictionary<HeatingCoilDataType, IndexedDoubles> dictionary = new Dictionary<HeatingCoilDataType, IndexedDoubles>();
            foreach (HeatingCoilDataType heatingCoilDataType in heatingCoilDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)heatingCoil, heatingCoilDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(heatingCoilDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[heatingCoilDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((SystemComponent)heatingCoil);

            SystemHeatingCoilResult result = new SystemHeatingCoilResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
