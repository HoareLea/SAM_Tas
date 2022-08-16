using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACSetup : UKBRElement
    {
        public override string UKBRName => "HVACSetup";

        public HVACSetup(XElement xElement)
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

        public string Description
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("Description"));
            }
        }

        public bool HasSimulated
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("HasSimulated"));
            }
        }
    }
}
