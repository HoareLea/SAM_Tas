using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemPipeLossComponentResult ToSAM_SystemPipeLossComponentResult(this PipeLossComponent pipeLossComponent, int start, int end, params PipeLossComponentDataType[] pumpDataTypes)
        {
            if (pipeLossComponent == null)
            {
                return null;
            }

            IEnumerable<PipeLossComponentDataType> pipeLossComponentDataTypes_Temp = pumpDataTypes == null || pumpDataTypes.Length == 0 ? System.Enum.GetValues(typeof(PipeLossComponentDataType)).Cast<PipeLossComponentDataType>() : pumpDataTypes;

            Dictionary<PipeLossComponentDataType, IndexedDoubles> dictionary = new Dictionary<PipeLossComponentDataType, IndexedDoubles>();
            foreach (PipeLossComponentDataType pipeLossComponentDataType in pipeLossComponentDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)pipeLossComponent, pipeLossComponentDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(pipeLossComponentDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[pipeLossComponentDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)pipeLossComponent);

            SystemPipeLossComponentResult result = new SystemPipeLossComponentResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
