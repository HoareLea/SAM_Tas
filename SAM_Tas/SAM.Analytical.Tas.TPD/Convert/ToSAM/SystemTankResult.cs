using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemTankResult ToSAM_SystemTankResult(this Tank tank, int start, int end, params TankDataType[] tankDataTypes)
        {
            if (tank == null)
            {
                return null;
            }

            IEnumerable<TankDataType> tankDataTypes_Temp = tankDataTypes == null || tankDataTypes.Length == 0 ? System.Enum.GetValues(typeof(TankDataType)).Cast<TankDataType>() : tankDataTypes;

            Dictionary<TankDataType, IndexedDoubles> dictionary = new Dictionary<TankDataType, IndexedDoubles>();
            foreach (TankDataType tankDataType in tankDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)tank, tankDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(tankDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[tankDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)tank);

            SystemTankResult result = new SystemTankResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
