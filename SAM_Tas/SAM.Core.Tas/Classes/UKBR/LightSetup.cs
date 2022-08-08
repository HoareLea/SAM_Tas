using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightSetup : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "LightSetup";
            }
        }

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
            int aIndex = ZoneGUIDs.ToZoneGUIDList().FindIndex(x => x.GUID == gUID);
            if (aIndex >= 0)
                return LightingDetails.LightingDetail(aIndex);

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
