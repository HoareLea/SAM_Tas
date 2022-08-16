using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
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
                if(xElement == null)
                {
                    return null;
                }

                return new Building(xElement);
            }
        }

        public ProjectFile ProjectFile
        {
            get
            {
                if (xElement == null)
                {
                    return null;
                }

                return new ProjectFile(xElement);
            }
        }

        public bool DoReImport
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("DoReImport"));
            }
        }

        public bool bModular
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bModular"));
            }
        }

        public bool bOldModular
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("bOldModular"));
            }
        }

        public bool fileWarningShown
        {
            get
            {
                return Query.Value<bool>(xElement?.Attribute("fileWarningShown"));
            }
        }
    }
}
