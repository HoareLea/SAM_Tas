using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemFanCoilUnit : SystemEquipment
    {
        public double Pressure { get; set; }
        public double CoolingDuty { get; set; }
        public double HeatingDuty { get; set; }
        public double DesignFlowRate { get; set; }
        public double HeatingEfficiency { get; set; }
        public double OverallEfficiency { get; set; }

        public SystemFanCoilUnit(string name) 
            :base(name)
        { 
        }

        public SystemFanCoilUnit(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemFanCoilUnit(SystemFanCoilUnit systemFanCoilUnit)
            : base(systemFanCoilUnit)
        {
            if(systemFanCoilUnit != null)
            {
                Pressure = systemFanCoilUnit.Pressure;
                CoolingDuty = systemFanCoilUnit.CoolingDuty;
                HeatingDuty = systemFanCoilUnit.HeatingDuty;
                DesignFlowRate  = systemFanCoilUnit.DesignFlowRate;
                HeatingEfficiency = systemFanCoilUnit.HeatingEfficiency;
                OverallEfficiency = systemFanCoilUnit.OverallEfficiency;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if(jObject.ContainsKey("Pressure"))
            {
                Pressure = jObject.Value<double>("Pressure");
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

            if (jObject.ContainsKey("HeatingEfficiency"))
            {
                HeatingEfficiency = jObject.Value<double>("HeatingEfficiency");
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

            if(!double.IsNaN(Pressure))
            {
                result.Add("Pressure", Pressure);
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

            if (!double.IsNaN(HeatingEfficiency))
            {
                result.Add("HeatingEfficiency", HeatingEfficiency);
            }

            if (!double.IsNaN(OverallEfficiency))
            {
                result.Add("OverallEfficiency", OverallEfficiency);
            }

            return result;
        }
    }
}
