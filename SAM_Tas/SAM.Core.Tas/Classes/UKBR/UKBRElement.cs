using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public abstract class UKBRElement
    {
        protected XElement xElement;

        protected UKBRElement(XDocument xDocument)
        {
            xElement = xDocument.Root;
        }

        protected UKBRElement(XElement xElement, string name)
        {
            this.xElement = xElement.Element(name);
        }

        protected UKBRElement(XElement xElement)
        {
            this.xElement = xElement;
        }

    }
}
