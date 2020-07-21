using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static shade ToT3D_Shade(this Panel panel, Building building)
        {
            if (panel == null)
                return null;

            uint colour;
            if (!Core.Query.TryGetValue(panel, "Colour", out colour))
                colour = Core.Convert.ToUint(System.Drawing.Color.Black);

            double width = double.NaN;
            double height = double.NaN;
            double level = double.NaN;

            PlanarBoundary3D planarBoundary3D = panel.PlanarBoundary3D;
            if (planarBoundary3D != null)
            {
                if (!Core.Query.TryGetValue(panel, "Width", out width))
                    return null;

                if (!Core.Query.TryGetValue(panel, "Height", out height))
                    return null;

                if (!Core.Query.TryGetValue(panel, "Level", out level))
                    return null;
            }
            else
            {
                width = planarBoundary3D.Width();
                height = planarBoundary3D.Height();
                level = Analytical.Query.MinElevation(panel);
            }

            shade result = building.AddShade(panel.Name, Query.OpeningType(panel), colour, height, width, level);

            return result;

        }
    }
}
