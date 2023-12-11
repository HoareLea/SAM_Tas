using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemChilledBeamResult : SystemIndexedResult, ISystemEquipmentResult
    {
        public SystemChilledBeamResult(string uniqueId, string name, string source, Dictionary<ChilledBeamDataType, IndexedDoubles> dictionary)
            : base(uniqueId, name, source, Query.Dictionary(dictionary))
        {
        }

        public SystemChilledBeamResult(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemChilledBeamResult(SystemChilledBeamResult systemChilledBeamResult)
            : base(systemChilledBeamResult)
        {

        }
    }
}
