using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.ShadeGroup> ShadeGroups(this TAS3D.Building building)
        {
            List<TAS3D.ShadeGroup> result = new List<TAS3D.ShadeGroup>();

            int index = 1;
            TAS3D.ShadeGroup shadeGroup = building.GetShadeGroup(index);
            while (shadeGroup != null)
            {
                result.Add(shadeGroup);
                index++;

                shadeGroup = building.GetShadeGroup(index);
            }

            return result;
        }
    }
}