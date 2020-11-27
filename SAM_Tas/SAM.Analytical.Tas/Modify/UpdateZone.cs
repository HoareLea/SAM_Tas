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

            building.RemoveInternalCondition(space.Name);

            TBD.InternalCondition internalCondition_TBD = AddInternalCondition(building, space, profileLibrary);
            if (internalCondition_TBD == null)
                return null;

            zone.AssignIC(internalCondition_TBD, true);

            return zone;
        }
    }
}