using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class GlazingCalculationResult : Result
    {
        private double totalSolarEnergyTransmittance;
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
                totalSolarEnergyTransmittance = solarTransmittanceCalculationResult.totalSolarEnergyTransmittance;
                lightTransmittance = solarTransmittanceCalculationResult.lightTransmittance;
                thermalTransmittance = solarTransmittanceCalculationResult.thermalTransmittance;
            }
        }

        public GlazingCalculationResult(System.Guid constructionGuid, string source, double totalSolarEnergyTransmittance, double lightTransmittance, double thermalTransmittance)
            : base(typeof(GlazingCalculationResult).ToString(), source, constructionGuid.ToString())
        {
            this.totalSolarEnergyTransmittance = totalSolarEnergyTransmittance;
            this.thermalTransmittance = thermalTransmittance;
            this.lightTransmittance = lightTransmittance;
        }

        public double TotalSolarEnergyTransmittance
        {
            get
            {
                return totalSolarEnergyTransmittance;
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

        public double Factor
        {
            get
            {
                double? value = Query.Factor(TotalSolarEnergyTransmittance, LightTransmittance);
                if(value == null || !value.HasValue)
                {
                    return double.NaN;
                }

                return value.Value;
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

            if(jObject.ContainsKey("TotalSolarEnergyTransmittance"))
            {
                totalSolarEnergyTransmittance = jObject.Value<double>("TotalSolarEnergyTransmittance");
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

            if (!double.IsNaN(TotalSolarEnergyTransmittance))
            {
                jObject.Add("TotalSolarEnergyTransmittance", totalSolarEnergyTransmittance);
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
