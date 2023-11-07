using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void SetColor(this buildingElement buildingElement, Aperture aperture, AperturePart aperturePart)
        {
            if(buildingElement == null || aperture == null || aperturePart == AperturePart.Undefined)
            {
                return;
            }

            System.Drawing.Color? color = Query.Color(aperture, aperturePart);
            if(color == null || !color.HasValue)
            {
                return;
            }

            buildingElement.colour = Core.Convert.ToUint(color.Value);
        }

        public static void SetColor(this TAS3D.window window, Aperture aperture, AperturePart aperturePart)
        {
            if (window == null || aperture == null || aperturePart == AperturePart.Undefined)
            {
                return;
            }

            System.Drawing.Color? color = Query.Color(aperture, aperturePart);
            if (color == null || !color.HasValue)
            {
                return;
            }

            window.colour = Core.Convert.ToUint(color.Value);
        }
    }
}