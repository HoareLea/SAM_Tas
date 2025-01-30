using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLoadComponentResult ToSAM_SystemLoadComponentResult(this LoadComponent loadComponent, int start, int end, params LoadComponentDataType[] loadComponentDataTypes)
        {
            if (loadComponent == null)
            {
                return null;
            }

            IEnumerable<LoadComponentDataType> loadComponentDataTypes_Temp = loadComponentDataTypes == null || loadComponentDataTypes.Length == 0 ? System.Enum.GetValues(typeof(LoadComponentDataType)).Cast<LoadComponentDataType>() : loadComponentDataTypes;

            Dictionary<LoadComponentDataType, IndexedDoubles> dictionary = new Dictionary<LoadComponentDataType, IndexedDoubles>();
            foreach (LoadComponentDataType loadComponentDataType in loadComponentDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)loadComponent, loadComponentDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(loadComponentDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[loadComponentDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)loadComponent);

            SystemLoadComponentResult result = new SystemLoadComponentResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
