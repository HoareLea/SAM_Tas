using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemIceStorageChillerResult ToSAM_SystemIceStorageChillerResult(this IceStorageChiller iceStorageChiller, int start, int end, params IceStorageChillerDataType[] iceStorageChillerDataTypes)
        {
            if (iceStorageChiller == null)
            {
                return null;
            }

            IEnumerable<IceStorageChillerDataType> iceStorageChillerDataTypes_Temp = iceStorageChillerDataTypes == null || iceStorageChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(IceStorageChillerDataType)).Cast<IceStorageChillerDataType>() : iceStorageChillerDataTypes;

            Dictionary<IceStorageChillerDataType, IndexedDoubles> dictionary = new Dictionary<IceStorageChillerDataType, IndexedDoubles>();
            foreach (IceStorageChillerDataType iceStorageChillerDataType in iceStorageChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)iceStorageChiller, iceStorageChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(iceStorageChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[iceStorageChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)iceStorageChiller);

            SystemIceStorageChillerResult result = new SystemIceStorageChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

