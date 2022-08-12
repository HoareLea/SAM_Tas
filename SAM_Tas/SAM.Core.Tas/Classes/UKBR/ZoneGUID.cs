using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class ZoneGUID : UKBRElement
    {
        public override string UKBRName => "ZoneGUID";


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
