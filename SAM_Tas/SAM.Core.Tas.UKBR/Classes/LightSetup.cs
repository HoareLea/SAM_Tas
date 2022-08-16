using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class LightSetup : UKBRElement
    {
        public override string UKBRName => "LightSetup";

        public LightSetup(XElement xElement)
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

        public ZoneGUIDs ZoneGUIDs
        {
            get
            {
                if(xElement == null)
                {
                    return null;
                }

                return new ZoneGUIDs(xElement);
            }
        }

        public LightingDetail LightingDetail(Guid gUID)
        {
            ZoneGUIDs zoneGUIDs = ZoneGUIDs;
            if(zoneGUIDs == null)
            {
                return null;
            }

            int index = 0;
            foreach(ZoneGUID zoneGUID in zoneGUIDs)
            {
                if(zoneGUID == null || zoneGUID.GUID != gUID)
                {
                    index++;
                    continue;
                }

                return LightingDetails.LightingDetail(index);
            }

            return null;
        }

        public LightingDetails LightingDetails
        {
            get
            {
                if(xElement == null)
                {
                    return null;
                }

                return new LightingDetails(xElement);
            }
        }
    }
}
