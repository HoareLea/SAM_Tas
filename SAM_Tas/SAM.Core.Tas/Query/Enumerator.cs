using System.Collections.Generic;
using System.Xml.Linq;
using System.Linq;
using System;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static IEnumerator<T> Enumerator<T>(this XElement xElement) where T: UKBRElement
        {
            if(xElement == null)
            {
                return new List<T>().GetEnumerator();
            }


            List<XElement> xElements = xElement.Elements()?.ToList();
            if (xElements == null)
            {
                return new List<T>().GetEnumerator();
            }

            return xElements.ConvertAll(x => Activator.CreateInstance(typeof(T), xElement) as T).GetEnumerator();
        }
    }
}