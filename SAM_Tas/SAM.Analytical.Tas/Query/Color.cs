namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static System.Drawing.Color? Color(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null || aperturePart == AperturePart.Undefined)
            {
                return null;
            }

            ApertureType apertureType = aperture.ApertureType;

            AperturePart aperturePart_Temp = apertureType == SAM.Analytical.ApertureType.Door ? AperturePart.Frame : aperturePart;

            System.Drawing.Color color = Analytical.Query.Color(apertureType, aperturePart_Temp, aperture.Openable());
            if (aperturePart == AperturePart.Pane && aperture.TryGetValue(ApertureParameter.Color, out System.Drawing.Color color_Temp) && color_Temp != global::System.Drawing.Color.Empty)
            {
                color = color_Temp;
            }

            return color;
        }
    }
}