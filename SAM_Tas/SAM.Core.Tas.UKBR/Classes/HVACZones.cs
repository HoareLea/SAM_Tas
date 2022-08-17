using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACZones : UKBRElements<HVACZone>
    {
        public override string UKBRName => "Zones";

        public HVACZones(XElement xElement)
            : base(xElement)
        {

        }

        public HVACZone HVACZone(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public HVACZone HVACZone(global::System.Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }
    }
}
