namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.zone UpdateZone_HDD(this TBD.Building building, TBD.zone zone, Space space, ProfileLibrary profileLibrary)
        {
            if (space == null || profileLibrary == null || zone == null || building == null)
                return null;

            TBD.InternalCondition internalCondition_TBD = AddInternalCondition_HDD(building, space, profileLibrary);
            if (internalCondition_TBD == null)
                return null;

            zone.AssignIC(internalCondition_TBD, true);

            return zone;
        }
    }
}