using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightSetups : UKBRElement, IEnumerable<LightSetup>
    {
        public static string UKBRName
        {
            get
            {
                return "LightSetups";
            }
        }

        public LightSetups(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public LightSetup LightSetup(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public IEnumerator<LightSetup> GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.LightSetup.UKBRName)?.ToList();
            if (xElements == null)
            {
                return new List<LightSetup>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new LightSetup(x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.LightSetup.UKBRName)?.ToList();
            if(xElements == null)
            {
                return new List<LightSetup>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new LightSetup(x)).GetEnumerator();
        }
    }
}
