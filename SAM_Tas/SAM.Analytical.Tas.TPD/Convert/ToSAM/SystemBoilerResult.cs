using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemBoilerResult ToSAM_SystemBoilerResult(this BoilerPlant boilerPlant, int start, int end, params BoilerDataType[] boilerDataTypes)
        {
            if (boilerPlant == null)
            {
                return null;
            }

            IEnumerable<BoilerDataType> boilerDataTypes_Temp = boilerDataTypes == null || boilerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(BoilerDataType)).Cast<BoilerDataType>() : boilerDataTypes;

            Dictionary<BoilerDataType, IndexedDoubles> dictionary = new Dictionary<BoilerDataType, IndexedDoubles>();
            foreach (BoilerDataType boilerDataType in boilerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)boilerPlant, boilerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(boilerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[boilerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)boilerPlant);

            SystemBoilerResult result = new SystemBoilerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }

        public static SystemBoilerResult ToSAM_SystemBoilerResult(this MultiBoiler multiBoiler, int start, int end, params BoilerDataType[] boilerDataTypes)
        {
            if (multiBoiler == null)
            {
                return null;
            }

            IEnumerable<BoilerDataType> boilerDataTypes_Temp = boilerDataTypes == null || boilerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(BoilerDataType)).Cast<BoilerDataType>() : boilerDataTypes;

            Dictionary<BoilerDataType, IndexedDoubles> dictionary = new Dictionary<BoilerDataType, IndexedDoubles>();
            foreach (BoilerDataType boilerDataType in boilerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)multiBoiler, boilerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(boilerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[boilerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)multiBoiler);

            SystemBoilerResult result = new SystemBoilerResult(reference, string.Empty, Query.Source(), dictionary);

            return result;
        }
    }
}
