using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACZoneGroup : UKBRElement
    {
        public override string UKBRName => "ZoneGroup";

        public HVACZoneGroup(XElement xElement)
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

        public bool IsSystem
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("IsSystem"));
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
