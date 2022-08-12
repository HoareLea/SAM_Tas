using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
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
                return xElement.Attribute("Name").Value;
            }
        }

        public ZoneGUIDs ZoneGUIDs
        {
            get
            {
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
                return new LightingDetails(xElement);
            }
        }
    }
}
