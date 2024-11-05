using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemVerticalBoreholeResult ToSAM_SystemVerticalBoreholeResult(this GroundSource groundSource, int start, int end, params VerticalBoreholeDataType[] verticalBoreholeDataTypes)
        {
            if (groundSource == null)
            {
                return null;
            }

            IEnumerable<VerticalBoreholeDataType> verticalBoreholeDataTypes_Temp = verticalBoreholeDataTypes == null || verticalBoreholeDataTypes.Length == 0 ? System.Enum.GetValues(typeof(VerticalBoreholeDataType)).Cast<VerticalBoreholeDataType>() : verticalBoreholeDataTypes;

            Dictionary<VerticalBoreholeDataType, IndexedDoubles> dictionary = new Dictionary<VerticalBoreholeDataType, IndexedDoubles>();
            foreach (VerticalBoreholeDataType verticalBoreholeDataType in verticalBoreholeDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)groundSource, verticalBoreholeDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(verticalBoreholeDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[verticalBoreholeDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)groundSource);

            SystemVerticalBoreholeResult result = new SystemVerticalBoreholeResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
