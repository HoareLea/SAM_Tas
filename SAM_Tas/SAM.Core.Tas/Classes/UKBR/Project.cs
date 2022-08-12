using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Project : UKBRElement
    {
        public override string UKBRName => "Project";

        public Project(XElement xElement)
            : base(xElement)
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
