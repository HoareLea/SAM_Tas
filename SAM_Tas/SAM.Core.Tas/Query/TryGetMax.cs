using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static bool TryGetMax(this BuildingData buildingData, IEnumerable<string> references, tsdZoneArray tSDZoneArray, out int index, out double max)
        {
            index = -1;
            max = double.NaN;

            if (buildingData == null || references == null || references.Count() == 0)
            {
                return false;
            }

            //TODO: Currently protection that protect from crushing ...confirm with Tas hwo to get Peak data if not 365 days in simulation

            object @object = null;
            try
            {
                @object = buildingData.GetPeakZoneGroupGains(new string[] { "Name:" + string.Join(":", references) }, new int[] { (int)tSDZoneArray });
            }
            catch
            {
                return false;
            }

            if (@object is object[,])
            {
                object[,] table = (object[,])@object;
                if (table.GetLength(0) == 3)
                {
                    index = System.Convert.ToInt32(table[2, 0]);
                    max = System.Convert.ToDouble(table[1, 0]);
                    return true;
                }
            }
            return false;
        }
    }
}