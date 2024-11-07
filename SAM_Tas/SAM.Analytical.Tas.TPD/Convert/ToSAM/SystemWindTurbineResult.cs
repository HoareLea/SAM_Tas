using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWindTurbineResult ToSAM_SystemWindTurbineResult(this WindTurbine windTurbine, int start, int end, params WindTurbineDataType[] windTurbineDataTypes)
        {
            if (windTurbine == null)
            {
                return null;
            }

            IEnumerable<WindTurbineDataType> windTurbineDataTypes_Temp = windTurbineDataTypes == null || windTurbineDataTypes.Length == 0 ? System.Enum.GetValues(typeof(WindTurbineDataType)).Cast<WindTurbineDataType>() : windTurbineDataTypes;

            Dictionary<WindTurbineDataType, IndexedDoubles> dictionary = new Dictionary<WindTurbineDataType, IndexedDoubles>();
            foreach (WindTurbineDataType windTurbineDataType in windTurbineDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)windTurbine, windTurbineDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(windTurbineDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[windTurbineDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)windTurbine);

            SystemWindTurbineResult result = new SystemWindTurbineResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
