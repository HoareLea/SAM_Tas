using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public abstract class UKBRElements<T> : UKBRElement, IEnumerable<T>  where T:UKBRElement
    {
        protected UKBRElements(XDocument xDocument)
            : base(xDocument)
        {
            xElement = xDocument.Root;
        }

        protected UKBRElements(XElement xElement)
            :base(xElement)
        {

        }
        public IEnumerator<T> GetEnumerator()
        {
            return Query.Enumerator<T>(xElement);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<T>(xElement);
        }
    }
}
