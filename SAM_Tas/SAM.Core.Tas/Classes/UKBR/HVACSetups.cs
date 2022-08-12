using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class HVACSetups : UKBRElement, IEnumerable<HVACSetup>
    {
        public override string UKBRName => "HVACSetups";

        public HVACSetups(XElement xElement)
            : base(xElement)
        {

        }

        public HVACSetup HVACSetup(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public IEnumerator<HVACSetup> GetEnumerator()
        {
            return Query.Enumerator<HVACSetup>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<HVACSetup>(xElement);
        }
    }
}
