using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class HVACZoneGroups : UKBRElement, IEnumerable<HVACZoneGroup>
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

        public IEnumerator<HVACZoneGroup> GetEnumerator()
        {
            return Query.Enumerator<HVACZoneGroup>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<HVACZoneGroup>(xElement);
        }
    }
}
