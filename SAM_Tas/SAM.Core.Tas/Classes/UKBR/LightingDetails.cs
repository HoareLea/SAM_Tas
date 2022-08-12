using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightingDetails : UKBRElement, IEnumerable<LightingDetail>
    {
        public override string UKBRName => "LightingDetails";

        public LightingDetails(XElement xElement)
            : base(xElement)
        {

        }

        public LightingDetail LightingDetail(int index)
        {
            int index_Temp = 0;
            foreach (LightingDetail lightingDetail in this)
            {
                if(index == index_Temp)
                {
                    return lightingDetail;
                }

                index_Temp++;
            }

            return null;
        }

        public IEnumerator<LightingDetail> GetEnumerator()
        {
            return Query.Enumerator<LightingDetail>(xElement);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Query.Enumerator<LightingDetail>(xElement);
        }
    }
}
