using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.window> Windows(this TAS3D.Building building)
        {
            List<TAS3D.window> result = new List<TAS3D.window>();

            int index = 1;
            TAS3D.window window = building.GetWindow(index);
            while (window != null)
            {
                result.Add(window);
                index++;

                window = building.GetWindow(index);
            }

            return result;
        }
    }
}