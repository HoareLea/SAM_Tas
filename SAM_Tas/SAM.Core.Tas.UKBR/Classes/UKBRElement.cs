using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public abstract class UKBRElement
    {
        public abstract string UKBRName { get; }
        
        protected XElement xElement;

        protected UKBRElement(XDocument xDocument)
        {
            xElement = xDocument?.Root;
        }

        protected UKBRElement(XElement xElement)
        {
            if (xElement?.Name == UKBRName)
            {
                this.xElement = xElement;
            }
            else
            {
                this.xElement = xElement?.Element(UKBRName);
            }
        }

    }
}
