using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Space Match(this TAS3D.Zone zone, IEnumerable<Space> spaces)
        {
            if (spaces == null || zone == null)
                return null;

            foreach (Space space in spaces)
            {
                if (zone.name.Equals(space.Name))
                    return space;
            }

            return null;
        }

        public static Space Match(this Core.Tas.UKBR.Zone zone, IEnumerable<Space> spaces)
        {
            if(zone == null || spaces == null)
            {
                return null;
            }

            foreach(Space space in spaces)
            {
                if(space == null)
                {
                    continue;
                }

                if(!space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) || string.IsNullOrWhiteSpace(zoneGuid))
                {
                    continue;
                }

                if(!global::System.Guid.TryParse(zoneGuid, out System.Guid guid))
                {
                    continue;
                }

                if(zone.GUID == guid)
                {
                    return space;
                }
            }

            foreach (Space space in spaces)
            {
                if(space?.Name == zone.Name)
                {
                    return space;
                }
            }

            return null;
        }

        public static TBD.zone Match(this IEnumerable<TBD.zone> zones, string name, bool caseSensitive = true, bool trim = false)
        {
            if (zones == null || zones.Count() == 0)
                return null;

            if (string.IsNullOrWhiteSpace(name))
                return null;

            string name_Temp = name;

            if (trim)
                name_Temp = name_Temp.Trim();

            if (!caseSensitive)
                name_Temp = name_Temp.ToUpper();

            foreach (TBD.zone zone in zones)
            {
                string name_Zone = zone?.name;
                if (string.IsNullOrWhiteSpace(name_Zone))
                    continue;

                if (trim)
                    name_Zone = name_Zone.Trim();

                if (!caseSensitive)
                    name_Zone = name_Zone.ToUpper();

                if (name_Zone.Equals(name_Temp))
                    return zone;
            }

            return null;
        }

        public static TBD.zone Match(this Space space, IEnumerable<TBD.zone> zones)
        {
            if (space == null || zones == null)
            {
                return null;
            }

            TBD.zone result = null;
            if (space.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) && !string.IsNullOrWhiteSpace(zoneGuid))
            {
                result = zones.ToList().Find(x => x.GUID == zoneGuid);
            }

            if (result == null)
            {
                result = zones.ToList().Find(x => x.name == space.Name);
            }

            return result;
        }

        public static Construction Match(this TAS3D.Element element, IEnumerable<Construction> constructions)
        {
            if (constructions == null || element == null)
                return null;

            string name = Name(element);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<Construction> constructions_Temp = constructions.ToList();
            constructions_Temp.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Name));

            foreach (Construction construction in constructions_Temp)
            {
                if (name.Equals(construction.Name.Trim()))
                    return construction;
            }

            foreach(Construction construction in constructions_Temp)
            {
                if (name.EndsWith(string.Format(": {0}", construction.Name.Trim())))
                    return construction;
            }

            return null;
        }

        public static ApertureConstruction Match(this TAS3D.window window, IEnumerable<ApertureConstruction> apertureConstructions)
        {
            if (apertureConstructions == null || window == null)
                return null;

            string name = Name(window);
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<ApertureConstruction> apertureConstructions_Temp = apertureConstructions.ToList();
            apertureConstructions_Temp.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Name));

            foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
            {
                if (name.Equals(apertureConstruction.Name.Trim()))
                    return apertureConstruction;
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions_Temp)
            {
                if (name.EndsWith(string.Format(": {0}", apertureConstruction.Name.Trim())))
                    return apertureConstruction;
            }

            return null;
        }
    }
}