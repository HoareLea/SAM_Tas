using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Element ToT3D(this Panel panel, Building building)
        {
            if (panel == null)
                return null;

            uint colour;
            if (!Core.Query.TryGetValue(panel, "Colour", out colour))
                return null;

            double width;
            if (!Core.Query.TryGetValue(panel, "Width", out width))
                return null;

            Element result = building.AddElement(panel.Name, colour, width);

            return result;
        }
    }
}
