using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class BuildingElement : UKBRElement
    {
        public override string UKBRName => "BuildingElement";

        public BuildingElement(XElement xElement)
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

        public double UValue
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("UValue"));
            }
        }

        public double Area
        {
            get
            {
                return Query.Value<double>(xElement?.Attribute("Area"));
            }
        }

        public bool bRemove
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bRemove"));
            }
        }

        public int BETypeIndex
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("BEType"));
            }
        }
    }
}
