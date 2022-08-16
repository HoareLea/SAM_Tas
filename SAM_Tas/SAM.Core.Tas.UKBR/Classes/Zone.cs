using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class Zone : UKBRElement
    {
        public override string UKBRName => "Zone";

        public Zone(XElement xElement)
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

        public Guid GUID
        {
            get
            {
                return Query.Value<Guid>(xElement?.Attribute("GUID"));
            }
        }
    }
}
