using System.Drawing;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static window ToT3D(this IOpening opening, Building building)
        {
            if (opening == null)
                return null;

            uint colour;
            if (!Core.Query.TryGetValue(opening, "Color", out colour))
            {
                colour = Core.Convert.ToUint(Color.Black);
            }

            Geometry.Spatial.BoundingBox3D boundingBox3D = opening.Face3D?.GetBoundingBox();

            double width = double.NaN;
            if (!Core.Query.TryGetValue(opening, "Width", out width))
            {
                if(boundingBox3D == null)
                {
                    return null;
                }

            }

            double height = double.NaN;
            double level = double.NaN;

            throw new System.NotImplementedException();


            //    double height = double.NaN;
            //double level = double.NaN;

            //PlanarBoundary3D planarBoundary3D = opening.PlanarBoundary3D;
            //if(planarBoundary3D == null)
            //{
            //    if (!Core.Query.TryGetValue(opening, "Width", out width))
            //        return null;

            //    if (!Core.Query.TryGetValue(opening, "Height", out height))
            //        return null;

            //    if (!Core.Query.TryGetValue(opening, "Level", out level))
            //        return null;
            //}
            //else
            //{
            //    width = planarBoundary3D.Width();
            //    height = planarBoundary3D.Height();
            //    level = Analytical.Query.MinElevation(planarBoundary3D);
            //}

            window result = building.AddWindow(opening.Name, Query.OpeningType(opening), colour, height, width, level);

            return result;
        }
    }
}
