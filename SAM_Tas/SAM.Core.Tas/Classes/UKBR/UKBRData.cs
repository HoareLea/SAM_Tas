using System.Xml.Linq;

namespace SAM.Core.Tas
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
                return new Project(xElement);
            }
        }

        public string Version
        {
            get
            {
                return xElement.Element("Version").Value;
            }
            set
            {
                xElement.Element("Version").Value = value;
            }
        }
    }
}
