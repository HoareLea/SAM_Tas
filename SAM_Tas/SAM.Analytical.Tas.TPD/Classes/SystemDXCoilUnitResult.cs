using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemDXCoilUnitResult : SystemIndexedResult, ISystemEquipmentResult
    {
        public SystemDXCoilUnitResult(string uniqueId, string name, string source, Dictionary<DXCoilUnitDataType, IndexedDoubles> dictionary) 
            :base(uniqueId, name, source, Query.Dictionary( dictionary))
        { 
        }

        public SystemDXCoilUnitResult(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemDXCoilUnitResult(SystemDXCoilUnitResult systemDXCoilUnitResult)
            : base(systemDXCoilUnitResult)
        {

        }
    }
}
