using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Building : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "Building";
            }
        }

        public Building(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public LightSetups LightSetups
        {
            get
            {
                return new LightSetups(xElement);
            }
        }
    }
}
