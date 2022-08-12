using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Zones : UKBRElement, IEnumerable<Zone>
    {
        public override string UKBRName => "Zones";

        public Zones(XElement xElement)
            : base(xElement)
        {

        }

        public Zone Zone(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public Zone Zone(Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }

        public IEnumerator<Zone> GetEnumerator()
        {
            return Query.Enumerator<Zone>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<Zone>(xElement);
        }
    }
}
