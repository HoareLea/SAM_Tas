using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightSetups : UKBRElement, IEnumerable<LightSetup>
    {
        public override string UKBRName => "LightSetups";

        public LightSetups(XElement xElement)
            : base(xElement)
        {

        }

        public LightSetup LightSetup(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public IEnumerator<LightSetup> GetEnumerator()
        {
            return Query.Enumerator<LightSetup>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<LightSetup>(xElement);
        }
    }
}
