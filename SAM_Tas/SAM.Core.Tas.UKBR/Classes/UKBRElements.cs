using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
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

        public int Count
        {
            get
            {
                IEnumerable<XElement> xElements = xElement?.Elements();
                if(xElements == null)
                {
                    return -1;
                }

                return xElements.Count();
            }
        }

        public T this[int index]
        {
            get
            {
                IEnumerable<XElement> xElements = xElement?.Elements();
                if (xElements == null)
                {
                    return null;
                }

                int index_Temp = 0;
                foreach(XElement xElement in xElements)
                {
                    if(index_Temp == index)
                    {
                        return Activator.CreateInstance(typeof(T), xElement) as T;
                    }
                }

                return null;
            }
        }

        public List<T> Items
        {
            get
            {
                List<T> result = new List<T>();
                foreach(T t in this)
                {
                    result.Add(t);
                }

                return result;
            }
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
