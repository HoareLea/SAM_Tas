using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public class LivingAreaData : GuidCollection
    {
        public LivingAreaData()
        {

        }

        public LivingAreaData(IEnumerable<Guid> guids)
        {
            if (guids != null)
            {
                foreach (Guid guid in guids)
                {
                    this.Add(guid);
                }
            }
        }

        public List<string> ToStrings()
        {
            List<string> result = new List<string>();
            result.Add("START_LIVING_AREA_DATA");
            result.AddRange(base.ToStrings());
            result.Add("END_LIVING_AREA_DATA");
            return result;
        }
    }
}
