using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static TBD.schedule Schedule(this TBD.Building building, string name, IEnumerable<int> values = null)
        {
            if (building == null || string.IsNullOrEmpty(name))
                return null;

            TBD.schedule result = building.AddSchedule();
            if (result == null)
                return null;

            result.name = name;

            if (values == null)
                return result;

            int count = System.Math.Min(24, values.Count());
            for (int i = 0; i < count; i++)
                result.values[i] = values.ElementAt(i);

            return result;
        }
    }
}