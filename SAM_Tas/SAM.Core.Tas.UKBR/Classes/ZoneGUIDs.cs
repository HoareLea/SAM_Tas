using System;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class ZoneGUIDs : UKBRElements<ZoneGUID>
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
    }
}
