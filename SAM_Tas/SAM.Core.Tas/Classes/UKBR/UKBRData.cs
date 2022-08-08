using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class UKBRData : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "UKBrData";
            }
        }

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
