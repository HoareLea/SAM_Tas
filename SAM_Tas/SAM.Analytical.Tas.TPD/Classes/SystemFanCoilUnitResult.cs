using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemFanCoilUnitResult : SystemIndexedResult, ISystemEquipmentResult
    {
        public SystemFanCoilUnitResult(string uniqueId, string name, string source, Dictionary<FanCoilUnitDataType, IndexedDoubles> dictionary) 
            :base(uniqueId, name, source, Query.Dictionary(dictionary))
        { 
        }

        public SystemFanCoilUnitResult(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemFanCoilUnitResult(SystemFanCoilUnitResult systemFanCoilUnitResult)
            : base(systemFanCoilUnitResult)
        {

        }
    }
}
