using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public class LightingDetails : UKBRElement
    {
        public static string UKBRName
        {
            get
            {
                return "LightingDetails";
            }
        }

        public LightingDetails(XElement xElement)
            : base(xElement, UKBRName)
        {

        }

        public List<LightingDetail> ToLightingDetailList()
        {
            return xElement.Elements("LightingDetail").ToList().ConvertAll(x => new LightingDetail(x));
        }

        public LightingDetail LightingDetail(int index)
        {
            return ToLightingDetailList()[index];
        }
    }
}
