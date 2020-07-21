using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.shade> Shades(this TAS3D.Building building)
        {
            List<TAS3D.shade> result = new List<TAS3D.shade>();

            int index = 1;
            TAS3D.shade shade = building.GetShade(index);
            while (shade != null)
            {
                result.Add(shade);
                index++;

                shade = building.GetShade(index);
            }

            return result;
        }
    }
}