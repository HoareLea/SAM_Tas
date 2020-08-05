namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool RemoveIZAMs(this TBD.Building building)
        {
            if (building == null)
                return false;

            TBD.IZAM iZAM = building.GetIZAM(0);
            while (iZAM != null)
            {
                building.RemoveIZAM(0);
                iZAM = building.GetIZAM(0);
            }

            return true;
        }
    }
}