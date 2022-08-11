using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Zone : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "Zone";
            }
        }

        public Zone(XElement xElement)
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

        public Guid GUID
        {
            get
            {
                return new Guid(xElement.Attribute("GUID").Value);
            }
        }
    }
}
