using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLiquidJunctionResult ToSAM_SystemLiquidJunctionResult(this PlantJunction plantJunction, int start, int end, params LiquidJunctionDataType[] liquidJunctionDataTypes)
        {
            if (plantJunction == null)
            {
                return null;
            }

            IEnumerable<LiquidJunctionDataType> liquidJunctionDataTypes_Temp = liquidJunctionDataTypes == null || liquidJunctionDataTypes.Length == 0 ? System.Enum.GetValues(typeof(LiquidJunctionDataType)).Cast<LiquidJunctionDataType>() : liquidJunctionDataTypes;

            Dictionary<LiquidJunctionDataType, IndexedDoubles> dictionary = new Dictionary<LiquidJunctionDataType, IndexedDoubles>();
            foreach (LiquidJunctionDataType LiquidJunctionDataType in liquidJunctionDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((PlantComponent)plantJunction, LiquidJunctionDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(LiquidJunctionDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[LiquidJunctionDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((PlantComponent)plantJunction);

            SystemLiquidJunctionResult result = new SystemLiquidJunctionResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
