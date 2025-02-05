using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDirectEvaporativeCoolerResult ToSAM_SystemDirectEvaporativeCoolerResult(this SprayHumidifier sprayHumidifier, int start, int end, params DirectEvaporativeCoolerDataType[] directEvaporativeCoolerDataTypes)
        {
            if (sprayHumidifier == null)
            {
                return null;
            }

            IEnumerable<DirectEvaporativeCoolerDataType> directEvaporativeCoolerDataType_Temp = directEvaporativeCoolerDataTypes == null || directEvaporativeCoolerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DirectEvaporativeCoolerDataType)).Cast<DirectEvaporativeCoolerDataType>() : directEvaporativeCoolerDataTypes;

            Dictionary<DirectEvaporativeCoolerDataType, IndexedDoubles> dictionary = new Dictionary<DirectEvaporativeCoolerDataType, IndexedDoubles>();
            foreach (DirectEvaporativeCoolerDataType directEvaporativeCoolerDataType in directEvaporativeCoolerDataType_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((SystemComponent)sprayHumidifier, directEvaporativeCoolerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(directEvaporativeCoolerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[directEvaporativeCoolerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }

            string reference = Query.Reference((SystemComponent)sprayHumidifier);

            SystemDirectEvaporativeCoolerResult result = new SystemDirectEvaporativeCoolerResult(reference, string.Empty, Query.Source(), dictionary);

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)sprayHumidifier);
            if (collectionLink != null)
            {
                result.SetValue(AirSystemComponentParameter.ElectricalCollection, collectionLink);
            }

            return result;
        }
    }
}
