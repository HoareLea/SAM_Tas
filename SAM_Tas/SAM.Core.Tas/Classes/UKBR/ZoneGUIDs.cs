using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class ZoneGUIDs : UKBRElement, IEnumerable<ZoneGUID>
    {
        public override string UKBRName => "ZoneGUIDs";

        public ZoneGUIDs(XElement xElement)
            : base(xElement)
        {

        }

        public ZoneGUID ZoneGUID(Guid gUID)
        {
            return this.ToList().Find(x => x.GUID == gUID);
        }

        public IEnumerator<ZoneGUID> GetEnumerator()
        {
            return Query.Enumerator<ZoneGUID>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<ZoneGUID>(xElement);
        }
    }
}
