using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
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
                return xElement.Attribute("Name")?.Value;
            }
        }

        public bool IsSystem
        {
            get
            {
                XAttribute xAttribute = xElement.Attribute("IsSystem");
                if(xAttribute == null)
                {
                    return false;
                }

                return xElement.Attribute("IsSystem").Value?.ToUpper().Trim() == "TRUE";
            }
        }

        public Guid GUID
        {
            get
            {
                XAttribute xAttribute = xElement.Attribute("GUID");
                if (xAttribute == null || string.IsNullOrWhiteSpace(xAttribute.Value))
                {
                    return Guid.Empty;
                }

                return new Guid(xAttribute.Value);
            }
        }
    }
}
