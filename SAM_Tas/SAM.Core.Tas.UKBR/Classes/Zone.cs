using System;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class Zone : UKBRElement
    {
        public override string UKBRName => "Zone";

        public Zone(XElement xElement)
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

        public bool bRemove
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bRemove"));
            }
        }

        public bool IsExternal
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("IsExternal"));
            }
        }

        public bool IsOfficeStorageSpace
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("IsOfficeStorageSpace"));
            }
        }

        public bool IsCorridorCirculationArea
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("IsCorridorCirculationArea"));
            }
        }

        public CurrentLights CurrentLights
        {
            get
            {
                return new CurrentLights(xElement);
            }
        }
    }
}
