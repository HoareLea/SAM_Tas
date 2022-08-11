using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class SourceSets : UKBRElement, IEnumerable<SourceSet>
    {
        public static string UKBRName
        {
            get
            {
                return "SourceSets";
            }
        }

        public SourceSets(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public SourceSet SourceSet(System.Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }

        public IEnumerator<SourceSet> GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.SourceSet.UKBRName)?.ToList();
            if (xElements == null)
            {
                return new List<SourceSet>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new SourceSet(x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.SourceSet.UKBRName)?.ToList();
            if(xElements == null)
            {
                return new List<SourceSet>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new SourceSet(x)).GetEnumerator();
        }
    }
}
