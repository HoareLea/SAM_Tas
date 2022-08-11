using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class SourceSet : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "SourceSet";
            }
        }

        public SourceSet(XElement xElement)
            : base(xElement)
        {

        }

        public Guid GUID
        {
            get
            {
                return new Guid(xElement.Attribute("GUID").Value);
            }
        }

        public Zones Zones
        {
            get
            {
                return new Zones(xElement);
            }
        }
    }
}
