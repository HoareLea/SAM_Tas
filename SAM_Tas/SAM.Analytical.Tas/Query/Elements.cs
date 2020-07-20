using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.Element> Elements(this TAS3D.Building building)
        {
            List<TAS3D.Element> result = new List<TAS3D.Element>();

            int index = 1;
            TAS3D.Element element = building.GetElement(index);
            while (element != null)
            {
                result.Add(element);
                index++;

                element = building.GetElement(index);
            }

            return result;
        }
    }
}