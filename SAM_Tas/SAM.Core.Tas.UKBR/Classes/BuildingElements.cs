using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class BuildingElements : UKBRElements<BuildingElement>
    {
        public override string UKBRName => "BuildingElements";

        public BuildingElements(XElement xElement)
            : base(xElement)
        {

        }

        public BuildingElement BuildingElement(global::System.Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }
    }
}
