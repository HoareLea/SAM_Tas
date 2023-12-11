using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemRadiator : SystemEquipment
    {
        public SystemRadiator(string name) 
            :base(name)
        { 
        }

        public SystemRadiator(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemRadiator(SystemRadiator systemRadiator)
            : base(systemRadiator)
        {

        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null)
            {
                return null;
            }

            return result;
        }
    }
}
