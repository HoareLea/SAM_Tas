using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class SourceSets : UKBRElement, IEnumerable<SourceSet>
    {
        public override string UKBRName => "SourceSets";

        public SourceSets(XElement xElement)
            : base(xElement)
        {

        }

        public SourceSet SourceSet(System.Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }

        public IEnumerator<SourceSet> GetEnumerator()
        {
            return Query.Enumerator<SourceSet>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<SourceSet>(xElement);
        }
    }
}
