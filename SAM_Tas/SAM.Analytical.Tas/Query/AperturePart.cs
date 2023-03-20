namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static AperturePart AperturePart(this int bEType)
        {
            switch(bEType)
            {
                case 14:
                    return Analytical.AperturePart.Frame;
                case 12:
                    return Analytical.AperturePart.Pane;
                case 13:
                    return Analytical.AperturePart.Pane;
                case 15:
                    return Analytical.AperturePart.Frame;
            }

            return Analytical.AperturePart.Undefined;
        }
    }
}