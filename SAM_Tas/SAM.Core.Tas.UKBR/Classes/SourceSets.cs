using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class SourceSets : UKBRElements<SourceSet>
    {
        public override string UKBRName => "SourceSets";

        public SourceSets(XElement xElement)
            : base(xElement)
        {

        }

        public SourceSet SourceSet(global::System.Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }
    }
}
