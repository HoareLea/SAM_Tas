using System.Collections.Generic;

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

            TBD.InternalCondition internalCondition_TBD = AddInternalCondition(building, space, profileLibrary);
            if (internalCondition_TBD == null)
                return null;

            zone.AssignIC(internalCondition_TBD, true);

            List<string> values = new List<string>();

            //TODO: Update [Id] to [Element Id]
            string id;
            if (space.TryGetValue("Element Id", out id))
            {
                if (!string.IsNullOrWhiteSpace(id))
                    values.Add(string.Format("[Id]={0}", id));
            }

            //TODO: Update [LevelName] to [Level Name]
            string levelName = null;
            if(space.TryGetValue(SpaceParameter.LevelName, out levelName))
            {
                if (!string.IsNullOrWhiteSpace(levelName))
                    values.Add(string.Format("[LevelName]={0}", levelName));
            }

            if (values != null && values.Count > 0)
                zone.description = string.Join("; ", values);

            return zone;
        }
    }
}