using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChilledBeamResult ToSAM_SystemChilledBeamResult(this ChilledBeam chilledBeam, int start, int end, params ChilledBeamDataType[] radiatorDataTypes)
        {
            if (chilledBeam == null)
            {
                return null;
            }

            IEnumerable<ChilledBeamDataType> chilledBeamDataTypes_Temp = radiatorDataTypes == null || radiatorDataTypes.Length == 0 ? System.Enum.GetValues(typeof(ChilledBeamDataType)).Cast<ChilledBeamDataType>() : radiatorDataTypes;

            Dictionary<ChilledBeamDataType, IndexedDoubles> dictionary = new Dictionary<ChilledBeamDataType, IndexedDoubles>();
            foreach (ChilledBeamDataType chilledBeamDataType in chilledBeamDataTypes_Temp)
            {
                IndexedDoubles indexedDoubles = Create.IndexedDoubles((ZoneComponent)chilledBeam, chilledBeamDataType, start, end);
                if (indexedDoubles == null)
                {
                    continue;
                }

                if (!dictionary.TryGetValue(chilledBeamDataType, out IndexedDoubles indexedDoubles_Temp) || indexedDoubles_Temp == null)
                {
                    dictionary[chilledBeamDataType] = indexedDoubles;
                }
                else
                {
                    indexedDoubles_Temp.Sum(indexedDoubles);
                }
            }


            string reference = Query.Reference((ZoneComponent)chilledBeam);

            SystemChilledBeamResult result = new SystemChilledBeamResult(reference, string.Empty, Query.Source(), dictionary);
            
            return result;
        }
    }
}
