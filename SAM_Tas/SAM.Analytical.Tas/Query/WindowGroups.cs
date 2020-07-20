using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.WindowGroup> WindowGroups(this TAS3D.Building building)
        {
            List<TAS3D.WindowGroup> result = new List<TAS3D.WindowGroup>();

            int index = 1;
            TAS3D.WindowGroup windowGroup = building.GetWindowGroup(index);
            while (windowGroup != null)
            {
                result.Add(windowGroup);
                index++;

                windowGroup = building.GetWindowGroup(index);
            }

            return result;
        }
    }
}