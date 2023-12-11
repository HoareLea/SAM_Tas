using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemRadiatorResult : SystemIndexedResult, ISystemEquipmentResult
    {
        public SystemRadiatorResult(string uniqueId, string name, string source, IndexedDoubles heatingLoads) 
            :base(uniqueId, name, source, Query.Dictionary(RadiatorDataType.HeatingLoad, heatingLoads))
        { 
        }

        public SystemRadiatorResult(string uniqueId, string name, string source, Dictionary<RadiatorDataType, IndexedDoubles> dictionary)
            : base(uniqueId, name, source, Query.Dictionary(dictionary))
        {
        }

        public SystemRadiatorResult(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemRadiatorResult(SystemRadiatorResult systemRadiatorResult)
            : base(systemRadiatorResult)
        {

        }
    }
}
