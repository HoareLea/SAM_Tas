using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class Zones : UKBRElement, IEnumerable<Zone>
    {
        public static string UKBRName
        {
            get
            {
                return "Zones";
            }
        }

        public Zones(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public Zone Zone(string name)
        {
            return this.ToList().Find(x => x.Name == name);
        }

        public Zone Zone(Guid guid)
        {
            return this.ToList().Find(x => x.GUID == guid);
        }

        public IEnumerator<Zone> GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.Zone.UKBRName)?.ToList();
            if (xElements == null)
            {
                return new List<Zone>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new Zone(x)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            List<XElement> xElements = xElement?.Elements(Tas.Zone.UKBRName)?.ToList();
            if(xElements == null)
            {
                return new List<Zone>().GetEnumerator();
            }

            return xElements.ConvertAll(x => new Zone(x)).GetEnumerator();
        }
    }
}
