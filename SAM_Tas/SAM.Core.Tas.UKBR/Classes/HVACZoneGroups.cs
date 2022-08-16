using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACZoneGroups : UKBRElements<HVACZoneGroup>
    {
        public override string UKBRName => "ZoneGroups";

        public HVACZoneGroups(XElement xElement)
            : base(xElement)
        {

        }

        public HVACZoneGroup HVACSetup(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }
    }
}
