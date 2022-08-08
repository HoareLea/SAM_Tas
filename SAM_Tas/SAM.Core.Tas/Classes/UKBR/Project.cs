using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Project : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "Project";
            }
        }

        public Project(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public Building Building
        {
            get
            {
                return new Building(xElement);
            }
        }
    }
}
