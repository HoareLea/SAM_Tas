using System.Collections.Generic;

namespace SAM.Geometry.Tas
{
    public static partial class Query
    {
        public static List<TBD.Polygon> Holes(this TBD.Perimeter perimeter)
        {
            if(perimeter == null)
            {
                return null;
            }

            List<TBD.Polygon> result = new List<TBD.Polygon>();

            int index = 0;
            TBD.Polygon polygon = perimeter.GetHole(index);
            while (polygon != null)
            {
                result.Add(polygon);
                index++;

                polygon = perimeter.GetHole(index);
            }

            return result;

        }
    }
}
