using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.DaysShade> DaysShades(this TBD.Building building)
        {
            dynamic @dynamic = building?.GetShadeDays();
            if(dynamic == null)
            {
                return null;
            }

            return (@dynamic as IEnumerable)?.Cast<TBD.DaysShade>()?.ToList();
        }
    }
}