using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightSetups : UKBRElement
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

        public List<LightSetup> ToLightSetupList()
        {
            return xElement.Elements("LightSetup").ToList().ConvertAll(x => new LightSetup(x));
        }

        public LightSetup LightSetup(string name)
        {
            return ToLightSetupList().Find(x => x.Name == name);
        }
    }
}
