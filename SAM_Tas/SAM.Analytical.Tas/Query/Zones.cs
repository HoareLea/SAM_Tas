using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.Zone> Zones(this TAS3D.Building building)
        {
            List<TAS3D.Zone> result = new List<TAS3D.Zone>();

            int index = 1;
            TAS3D.Zone zone = building.GetZone(index);
            while (zone != null)
            {
                result.Add(zone);
                index++;

                zone = building.GetZone(index);
            }

            return result;
        }

        public static List<TAS3D.Zone> Zones(this TAS3D.zoneSet zoneSet)
        {
            List<TAS3D.Zone> result = new List<TAS3D.Zone>();

            int index = 1;
            TAS3D.Zone zone = zoneSet.GetZone(index);
            while (zone != null)
            {
                result.Add(zone);
                index++;

                zone = zoneSet.GetZone(index);
            }

            return result;
        }

        public static List<TBD.zone> Zones(this TBD.Building building)
        {
            if (building == null)
                return null;

            List<TBD.zone> result = new List<TBD.zone>();

            TBD.zone aZone = building.GetZone(result.Count);
            while (aZone != null)
            {
                result.Add(aZone);
                aZone = building.GetZone(result.Count);
            }

            return result;
        }

        public static List<TBD.zone> Zones(this TBD.InternalCondition internalCondition)
        {
            if (internalCondition == null)
                return null;

            List<TBD.zone> result = new List<TBD.zone>();

            TBD.zone zone = internalCondition.GetZone(result.Count);
            while (zone != null)
            {
                result.Add(zone);
                zone = internalCondition.GetZone(result.Count);
            }

            return result;
        }

        public static List<TBD.zone> Zones(this TBD.ZoneGroup zoneGroup)
        {
            if (zoneGroup == null)
                return null;

            List<TBD.zone> result = new List<TBD.zone>();

            TBD.zone zone = zoneGroup.GetZone(result.Count);
            while (zone != null)
            {
                result.Add(zone);
                zone = zoneGroup.GetZone(result.Count);
            }

            return result;
        }
    }
}