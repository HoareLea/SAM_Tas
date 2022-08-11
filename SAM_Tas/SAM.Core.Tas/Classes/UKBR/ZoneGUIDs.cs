using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class ZoneGUIDs : UKBRElement, IEnumerable<ZoneGUID>
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

        public ZoneGUID ZoneGUID(Guid gUID)
        {
            return this.ToList().Find(x => x.GUID == gUID);
        }

        public IEnumerator<ZoneGUID> GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.ZoneGUID.UKBRName)?.ToList();
            if (xElements == null)
            {
                return new List<ZoneGUID>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new ZoneGUID(x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.ZoneGUID.UKBRName)?.ToList();
            if (xElements == null)
            {
                return new List<ZoneGUID>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new ZoneGUID(x)).GetEnumerator();
        }
    }
}
