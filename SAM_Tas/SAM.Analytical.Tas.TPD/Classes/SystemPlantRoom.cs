using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemPlantRoom : SAMObject, ISystemObject
    {
        public SystemPlantRoom(string name) 
            :base(name)
        { 
        }

        public SystemPlantRoom(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemPlantRoom(SystemPlantRoom systemPlantRoom)
            : base(systemPlantRoom)
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
