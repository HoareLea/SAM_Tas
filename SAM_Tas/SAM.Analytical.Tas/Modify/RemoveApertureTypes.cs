namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool RemoveApertureTypes(this TBD.Building building)
        {
            if (building == null)
                return false;

            TBD.ApertureType apertureType = building.GetApertureType(0);
            while (apertureType != null)
            {
                building.RemoveApertureType(0);
                apertureType = building.GetApertureType(0);
            }

            return true;
        }
    }
}