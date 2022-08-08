using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class ZoneGUIDs : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "ZoneGUIDs";
            }
        }

        public ZoneGUIDs(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public List<ZoneGUID> ToZoneGUIDList()
        {
            return xElement.Elements("ZoneGUID").ToList().ConvertAll(x => new ZoneGUID(x));
        }

        public ZoneGUID ZoneGUID(Guid gUID)
        {
            return ToZoneGUIDList().Find(x => x.GUID == gUID);
        }
    }
}
