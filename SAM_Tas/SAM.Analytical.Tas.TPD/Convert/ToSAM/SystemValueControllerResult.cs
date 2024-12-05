using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemValueControllerResult ToSAM_SystemValueControllerResult(this PlantController plantController, int start, int end, params ValueControllerDataType[] valueControllerDataTypes)
        {
            if (plantController == null)
            {
                return null;
            }

            IEnumerable<ValueControllerDataType> valueControllerDataTypes_Temp = valueControllerDataTypes == null || valueControllerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ValueControllerDataType)).Cast<ValueControllerDataType>() : valueControllerDataTypes;

            Dictionary<ValueControllerDataType, IndexedDoubles> dictionary = new Dictionary<ValueControllerDataType, IndexedDoubles>();
            foreach (ValueControllerDataType valueControllerDataType in valueControllerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)plantController, valueControllerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(valueControllerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[valueControllerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)plantController);

            SystemValueControllerResult result = new SystemValueControllerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }

        public static SystemValueControllerResult ToSAM_SystemValueControllerResult(this Controller controller, int start, int end, params ValueControllerDataType[] valueControllerDataTypes)
        {
            if (controller == null)
            {
                return null;
            }

            IEnumerable<ValueControllerDataType> valueControllerDataTypes_Temp = valueControllerDataTypes == null || valueControllerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ValueControllerDataType)).Cast<ValueControllerDataType>() : valueControllerDataTypes;

            Dictionary<ValueControllerDataType, IndexedDoubles> dictionary = new Dictionary<ValueControllerDataType, IndexedDoubles>();
            foreach (ValueControllerDataType valueControllerDataType in valueControllerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((global::TPD.SystemComponent)controller, valueControllerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(valueControllerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[valueControllerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((global::TPD.SystemComponent)controller);

            SystemValueControllerResult result = new SystemValueControllerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}



