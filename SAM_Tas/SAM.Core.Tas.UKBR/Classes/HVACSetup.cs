using System.Collections.Generic;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACSetup : UKBRElement
    {
        public override string UKBRName => "HVACSetup";

        public HVACSetup(XElement xElement)
            : base(xElement)
        {

        }

        public string Name
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("Name"));
            }
        }

        public string Description
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("Description"));
            }
        }

        public bool HasSimulated
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("HasSimulated"));
            }
        }

        public string TPDPath
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("TPDPath"));
            }
        }

        public string TBD
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("TBD"));
            }
        }

        public string ProjectDirectory
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("ProjectDirectory"));
            }
        }

        public Systems Systems
        {
            get
            {
                return new Systems(xElement);
            }
        }

        public HVACZoneGroups HVACZoneGroups
        {
            get
            {
                return new HVACZoneGroups(xElement);
            }
        }

        public HVACZones HVACZones
        {
            get
            {
                return new HVACZones(xElement);
            }
        }

        public System GetSystem(HVACZone hVACZone)
        {
            if(hVACZone == null)
            {
                return null;
            }

            int index = hVACZone.SystemIndex;
            if(index <= -1 || index == Query.Invalid<int>())
            {
                return null;
            }

            return Systems?[index];
        }

        public List<HVACZoneGroup> GetHVACZoneGroups(HVACZone hVACZone)
        {
            List<int> indexes = hVACZone?.HVACZoneGroupIndexes;
            if(indexes == null)
            {
                return null;
            }

            HVACZoneGroups hVACZoneGroups = HVACZoneGroups;
            if(hVACZoneGroups == null)
            {
                return null;
            }

            List<HVACZoneGroup> result = new List<HVACZoneGroup>();
            foreach(int index in indexes)
            {
                HVACZoneGroup hVACZoneGroup = hVACZoneGroups[index];
                if(hVACZoneGroup == null)
                {
                    continue;
                }

                result.Add(hVACZoneGroup);
            }

            return result;
        }
    }
}
