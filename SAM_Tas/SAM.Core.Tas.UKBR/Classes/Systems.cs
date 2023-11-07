using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class Systems : UKBRElements<System>
    {
        public override string UKBRName => "Systems";

        public Systems(XElement xElement)
            : base(xElement)
        {

        }

        public System System(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }
    }
}
