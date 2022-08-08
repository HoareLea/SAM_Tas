using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class ZoneGUID : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "ZoneGUID";
            }
        }

        public ZoneGUID(XElement xElement)
            : base(xElement)
        {

        }

        public Guid GUID
        {
            get
            {
                return new Guid(xElement.Value);
            }
        }
    }
}
