using System.Drawing;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static window ToT3D(this Aperture aperture, Building building)
        {
            if (aperture == null)
                return null;

            uint colour;
            if (!Core.Query.TryGetValue(aperture, "Colour", out colour))
                colour = Core.Convert.ToUint(Color.Black);

            double width = double.NaN;
            double height = double.NaN;
            double level = double.NaN;

            PlanarBoundary3D planarBoundary3D = aperture.PlanarBoundary3D;
            if(planarBoundary3D == null)
            {
                if (!Core.Query.TryGetValue(aperture, "Width", out width))
                    return null;

                if (!Core.Query.TryGetValue(aperture, "Height", out height))
                    return null;

                if (!Core.Query.TryGetValue(aperture, "Level", out level))
                    return null;
            }
            else
            {
                width = planarBoundary3D.Width();
                height = planarBoundary3D.Height();
                level = Analytical.Query.MinElevation(planarBoundary3D);
            }

            window result = building.AddWindow(aperture.Name, Query.OpeningType(aperture), colour, height, width, level);

            return result;
        }
    }
}
