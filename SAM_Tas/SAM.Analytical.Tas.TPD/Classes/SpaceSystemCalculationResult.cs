using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SpaceSystemCalculationResult : Result
    {
        private double heatingDuty;
        private double coolingDuty;
        private double designFlowRate;
        private Dictionary<ResultDataType, IndexedDoubles> dictionary;

        public SpaceSystemCalculationResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        

        public SpaceSystemCalculationResult(SpaceSystemCalculationResult spaceSystemCalculationResult)
            : base(spaceSystemCalculationResult)
        {

        }

        public SpaceSystemCalculationResult(System.Guid spaceGuid, string source)
            : base(typeof(SpaceSystemCalculationResult).ToString(), source, spaceGuid.ToString())
        {

        }

        public override bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return result;
            }

            return result;
        }

        public JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            return jObject;
        }
    }
}
