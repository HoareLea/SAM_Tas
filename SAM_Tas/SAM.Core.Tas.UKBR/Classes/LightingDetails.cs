using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public class LightingDetails : UKBRElements<LightingDetail>
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
    }
}
