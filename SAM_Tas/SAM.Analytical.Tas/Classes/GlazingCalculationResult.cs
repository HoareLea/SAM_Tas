using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class GlazingCalculationResult : Result
    {
        private double solarTransmittance;
        private double lightTransmittance;
        private double thermalTransmittance;

        public GlazingCalculationResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        

        public GlazingCalculationResult(GlazingCalculationResult solarTransmittanceCalculationResult)
            : base(solarTransmittanceCalculationResult)
        {
            if(solarTransmittanceCalculationResult != null)
            {
                solarTransmittance = solarTransmittanceCalculationResult.solarTransmittance;
                lightTransmittance = solarTransmittanceCalculationResult.lightTransmittance;
                thermalTransmittance = solarTransmittanceCalculationResult.thermalTransmittance;
            }
        }

        public GlazingCalculationResult(System.Guid constructionGuid, string source, double solarTransmittance, double lightTransmittance, double thermalTransmittance)
            : base(typeof(GlazingCalculationResult).ToString(), source, constructionGuid.ToString())
        {
            this.solarTransmittance = solarTransmittance;
            this.thermalTransmittance = thermalTransmittance;
            this.lightTransmittance = lightTransmittance;
        }

        public double SolarTransmittance
        {
            get
            {
                return solarTransmittance;
            }
        }

        public double LightTransmittance
        {
            get
            {
                return lightTransmittance;
            }
        }

        public double ThermalTransmittance
        {
            get
            {
                return thermalTransmittance;
            }
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

            if(jObject.ContainsKey("SolarTransmittance"))
            {
                solarTransmittance = jObject.Value<double>("SolarTransmittance");
            }

            if (jObject.ContainsKey("LightTransmittance"))
            {
                lightTransmittance = jObject.Value<double>("LightTransmittance");
            }

            if (jObject.ContainsKey("ThermalTransmittance"))
            {
                thermalTransmittance = jObject.Value<double>("ThermalTransmittance");
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

            if (!double.IsNaN(solarTransmittance))
            {
                jObject.Add("SolarTransmittance", solarTransmittance);
            }

            if (!double.IsNaN(lightTransmittance))
            {
                jObject.Add("LightTransmittance", lightTransmittance);
            }

            if (!double.IsNaN(ThermalTransmittance))
            {
                jObject.Add("ThermalTransmittance", ThermalTransmittance);
            }

            return jObject;
        }
    }
}
