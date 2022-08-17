using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class ZoneGroups : UKBRElements<ZoneGroup>
    {
        public override string UKBRName => "ZoneGroups";

        public ZoneGroups(XElement xElement)
            : base(xElement)
        {

        }

        public ZoneGroup ZoneGroup(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }
    }
}
