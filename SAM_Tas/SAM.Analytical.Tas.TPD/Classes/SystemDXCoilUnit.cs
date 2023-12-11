using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemDXCoilUnit : SystemEquipment
    {
        public double CoolingDuty { get; set; }
        public double HeatingDuty { get; set; }
        public double DesignFlowRate { get; set; }
        public double OverallEfficiency { get; set; }

        public SystemDXCoilUnit(string name) 
            :base(name)
        { 
        }

        public SystemDXCoilUnit(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemDXCoilUnit(SystemDXCoilUnit systemDXCoilUnit)
            : base(systemDXCoilUnit)
        {
            if (systemDXCoilUnit != null)
            {
                CoolingDuty = systemDXCoilUnit.CoolingDuty;
                HeatingDuty = systemDXCoilUnit.HeatingDuty;
                DesignFlowRate = systemDXCoilUnit.DesignFlowRate;
                OverallEfficiency = systemDXCoilUnit.OverallEfficiency;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if (jObject.ContainsKey("CoolingDuty"))
            {
                CoolingDuty = jObject.Value<double>("CoolingDuty");
            }

            if (jObject.ContainsKey("HeatingDuty"))
            {
                HeatingDuty = jObject.Value<double>("HeatingDuty");
            }

            if (jObject.ContainsKey("DesignFlowRate"))
            {
                DesignFlowRate = jObject.Value<double>("DesignFlowRate");
            }

            if (jObject.ContainsKey("OverallEfficiency"))
            {
                OverallEfficiency = jObject.Value<double>("OverallEfficiency");
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

            if (!double.IsNaN(CoolingDuty))
            {
                result.Add("CoolingDuty", CoolingDuty);
            }

            if (!double.IsNaN(HeatingDuty))
            {
                result.Add("HeatingDuty", HeatingDuty);
            }

            if (!double.IsNaN(DesignFlowRate))
            {
                result.Add("DesignFlowRate", DesignFlowRate);
            }

            if (!double.IsNaN(OverallEfficiency))
            {
                result.Add("OverallEfficiency", OverallEfficiency);
            }

            return result;
        }
    }
}
