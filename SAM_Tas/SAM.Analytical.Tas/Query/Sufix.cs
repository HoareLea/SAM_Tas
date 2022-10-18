namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string Sufix(this AperturePart aperturePart)
        {
            switch(aperturePart)
            {
                case AperturePart.Frame:
                    return "-frame";

                case AperturePart.Pane:
                    return "-pane";
            }

            return null;
        }
    }
}