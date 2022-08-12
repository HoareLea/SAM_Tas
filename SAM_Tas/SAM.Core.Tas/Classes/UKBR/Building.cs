using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Building : UKBRElement
    {
        public override string UKBRName => "Building";

        public Building(XElement xElement)
            : base(xElement)
        {

        }

        public LightSetups LightSetups
        {
            get
            {
                return new LightSetups(xElement);
            }
        }

        public SourceSets SourceSets
        {
            get
            {
                return new SourceSets(xElement);
            }
        }

        public Zone GetZone(System.Guid guid)
        {
            SourceSets sourceSets = SourceSets;
            if(sourceSets == null)
            {
                return null;
            }

            foreach(SourceSet sourceSet in sourceSets)
            {
                Zones zones = sourceSet?.Zones;
                if(zones == null)
                {
                    continue; 
                }

                foreach(Zone zone in zones)
                {
                    if(zone.GUID == guid)
                    {
                        return zone;
                    }
                }
            }

            return null;
        }

        public List<Zone> GetZones(IEnumerable<System.Guid> guids)
        {
            if(guids == null)
            {
                return null;
            }

            SourceSets sourceSets = SourceSets;
            if (sourceSets == null)
            {
                return null;
            }

            List<Zone> result = new List<Zone>();
            foreach (SourceSet sourceSet in sourceSets)
            {
                Zones zones = sourceSet?.Zones;
                if (zones == null)
                {
                    continue;
                }

                foreach (Zone zone in zones)
                {
                    if (guids.Contains( zone.GUID))
                    {
                        result.Add(zone);
                    }
                }
            }

            return result;
        }

        public List<Zone> GetZones(ZoneGUIDs zoneGUIDs)
        {
            List<System.Guid> guids = zoneGUIDs?.ToList().ConvertAll(x => x.GUID);
            if(guids == null)
            {
                return null;
            }

            return GetZones(guids);
        }

        public string NCMVersion
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("NCMVersion"));
            }
        }
    }
}
