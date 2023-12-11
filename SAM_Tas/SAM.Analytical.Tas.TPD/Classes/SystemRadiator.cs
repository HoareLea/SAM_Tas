using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemRadiator : SystemEquipment
    {
        public double Efficiency { get; set; }
        public double Duty { get; set; }

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
            if (systemRadiator != null)
            {
                Efficiency = systemRadiator.Efficiency;
                Duty = systemRadiator.Duty;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if (jObject.ContainsKey("Efficiency"))
            {
                Efficiency = jObject.Value<double>("Efficiency");
            }

            if (jObject.ContainsKey("Duty"))
            {
                Duty = jObject.Value<double>("Duty");
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

            if (!double.IsNaN(Efficiency))
            {
                result.Add("Efficiency", Efficiency);
            }

            if (!double.IsNaN(Duty))
            {
                result.Add("Duty", Duty);
            }

            return result;
        }
    }
}
