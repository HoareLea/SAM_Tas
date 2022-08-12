using System;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class SourceSet : UKBRElement
    {
        public override string UKBRName => "SourceSet";

        public SourceSet(XElement xElement)
            : base(xElement)
        {

        }

        public Guid GUID
        {
            get
            {
                return Query.Value<Guid>(xElement?.Attribute("GUID"));
            }
        }

        public Zones Zones
        {
            get
            {
                if(xElement == null)
                {
                    return null;
                }

                return new Zones(xElement);
            }
        }
    }
}
