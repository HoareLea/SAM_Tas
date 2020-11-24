namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.zone UpdateZone(this TBD.Building building, Space space, ProfileLibrary profileLibrary)
        {
            if (space == null || profileLibrary == null)
                return null;

            TBD.zone zone = building?.Zones()?.Zone(space.Name);
            if (zone == null)
                return null;

            TBD.InternalCondition internalCondition = AddInternalCondition(building, space, profileLibrary);
            if (internalCondition == null)
                return null;

            zone.AssignIC(internalCondition, true);

            return zone;
        }
    }
}