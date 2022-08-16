using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class HVACSetups : UKBRElements<HVACSetup>
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
    }
}
