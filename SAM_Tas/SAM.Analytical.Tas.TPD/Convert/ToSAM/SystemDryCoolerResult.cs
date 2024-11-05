using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDryCoolerResult ToSAM_SystemDryCoolerResult(this DryCooler dryCooler, int start, int end, params DryCoolerDataType[] dryCoolerDataTypes)
        {
            if (dryCooler == null)
            {
                return null;
            }

            IEnumerable<DryCoolerDataType> dryCoolerDataTypes_Temp = dryCoolerDataTypes == null || dryCoolerDataTypes.Length == 0 ? System.Enum.GetValues(typeof(DryCoolerDataType)).Cast<DryCoolerDataType>() : dryCoolerDataTypes;

            Dictionary<DryCoolerDataType, IndexedDoubles> dictionary = new Dictionary<DryCoolerDataType, IndexedDoubles>();
            foreach (DryCoolerDataType dryCoolerDataType in dryCoolerDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)dryCooler, dryCoolerDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(dryCoolerDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[dryCoolerDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)dryCooler);

            SystemDryCoolerResult result = new SystemDryCoolerResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
