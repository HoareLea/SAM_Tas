using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class UKBRData : UKBRElement
    {
        public override string UKBRName => "UKBrData";

        public UKBRData(XDocument XDocument)
            : base(XDocument)
        {

        }

        public Project Project
        {
            get
            {
                if(xElement == null)
                {
                    return null;
                }

                return new Project(xElement);
            }
        }

        public string Version
        {
            get
            {
                return xElement?.Element("Version")?.Value;
            }
            set
            {
                XElement xElement_Temp = xElement?.Element("Version");
                if(xElement_Temp != null)
                {
                    xElement_Temp.Value = value;
                }
            }
        }
    }
}
