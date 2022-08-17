using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class System : UKBRElement
    {
        public override string UKBRName => "System";

        public System(XElement xElement)
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

        public string Type
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("__TYPE__"));
            }
        }

        public global::System.Guid GUID
        {
            get
            {
                return Query.Value<global::System.Guid>(xElement?.Attribute("GUID"));
            }
        }

        public global::System.Guid TPDxGUID
        {
            get
            {
                return Query.Value<global::System.Guid>(xElement?.Attribute("TPDxGUID"));
            }
        }

        public bool ExcludeFromExport
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("ExcludeFromExport"));
            }
        }

        public string Description
        {
            get
            {
                return Query.Value<string>(xElement?.Attribute("Description"));
            }
        }

        public int Multiplicity
        {
            get
            {
                return Query.Value<int>(xElement?.Attribute("Multiplicity"));
            }
        }

        public bool FreshAirIsHeated
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("FreshAirIsHeated"));
            }
        }
    }
}
