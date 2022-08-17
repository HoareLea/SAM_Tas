using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class ZoneGroup : UKBRElement
    {
        public override string UKBRName => "ZoneGroup";

        public ZoneGroup(XElement xElement)
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

        public int ZoneGroupTypeIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("ZoneGroupType"));
            }
        }

        public bool bRemove
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bRemove"));
            }
        }
    }
}
