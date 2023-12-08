using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public abstract class SystemEquipment : SAMObject, ISystemEquipment
    {
        public SystemEquipment(string name) 
            :base(name)
        { 
        }

        public SystemEquipment(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemEquipment(SystemEquipment systemEquipment)
            : base(systemEquipment)
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
