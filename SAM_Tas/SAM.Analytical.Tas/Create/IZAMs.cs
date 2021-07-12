using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static List<TBD.IZAM> IZAMs(this TBD.Building building, IEnumerable<Space> spaces)
        {
            if (building == null || spaces == null)
                return null;

            List<TBD.zone> zones = building.Zones();
            List<TBD.dayType> dayTypes = building.DayTypes();
            dayTypes.RemoveAll(x => x.name.Equals("CDD") || x.name.Equals("HDD"));

            List<TBD.IZAM> result = new List<TBD.IZAM>();
            foreach (Space space in spaces)
            {
                string name = space.Name;
                //if (string.IsNullOrWhiteSpace(name))
                //    space.TryGetValue(Analytical.Query.ParameterName_SpaceName(), out name, true);

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                name = name.Trim();

                TBD.IZAM iZAM_From = IZAM(building, space, name, true, zones, dayTypes);
                TBD.IZAM iZAM_To = IZAM(building, space, name, false, zones, dayTypes);

                if (iZAM_From != null)
                    result.Add(iZAM_From);

                if (iZAM_To != null)
                    result.Add(iZAM_To);
            }

            return result;
        }
    }
}