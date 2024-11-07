using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirSourceDirectAbsorptionChillerResult ToSAM_SystemAirSourceDirectAbsorptionChillerResult(this Chiller chiller, int start, int end, params AirSourceDirectAbsorptionChillerDataType[] airSourceDirectAbsorptionChillerDataTypes)
        {
            if (chiller == null)
            {
                return null;
            }

            IEnumerable<AirSourceDirectAbsorptionChillerDataType> airSourceDirectAbsorptionChillerDataTypes_Temp = airSourceDirectAbsorptionChillerDataTypes == null || airSourceDirectAbsorptionChillerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(AirSourceDirectAbsorptionChillerDataType)).Cast<AirSourceDirectAbsorptionChillerDataType>() : airSourceDirectAbsorptionChillerDataTypes;

            Dictionary<AirSourceDirectAbsorptionChillerDataType, IndexedDoubles> dictionary = new Dictionary<AirSourceDirectAbsorptionChillerDataType, IndexedDoubles>();
            foreach (AirSourceDirectAbsorptionChillerDataType airSourceDirectAbsorptionChillerDataType in airSourceDirectAbsorptionChillerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)chiller, airSourceDirectAbsorptionChillerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(airSourceDirectAbsorptionChillerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[airSourceDirectAbsorptionChillerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)chiller);

            SystemAirSourceDirectAbsorptionChillerResult result = new SystemAirSourceDirectAbsorptionChillerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}

