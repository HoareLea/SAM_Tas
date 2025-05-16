namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static string TBDZoneGroup(int index)
        {
            switch (index)
            {
                case 0:
                    return "Default";

                case 1:
                    return "HVAC";

                case 2:
                    return "Output";

                case 3:
                    return "Zone Set";
            }

            return null;
        }
    }
}