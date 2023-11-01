using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class ConstructionCalculationResult : Result, IConstructionCalculationResult
    {
        private string initialConstructionName = null;
        private double initialThermalTransmittance = double.NaN;

        private string constructionName;
        private double thermalTransmittance;
        private double calculatedThermalTransmittance;

        public ConstructionCalculationResult(JObject jObject)
            :base(jObject)
        {

        }

        public ConstructionCalculationResult(ConstructionCalculationResult constructionCalculationResult)
            :base(constructionCalculationResult)
        {
            if(constructionCalculationResult != null)
            {
                initialConstructionName = constructionCalculationResult.initialConstructionName;
                initialThermalTransmittance = constructionCalculationResult.initialThermalTransmittance;

                constructionName = constructionCalculationResult.constructionName;
                thermalTransmittance = constructionCalculationResult.thermalTransmittance;
                calculatedThermalTransmittance = constructionCalculationResult.calculatedThermalTransmittance;
            }
        }

        public ConstructionCalculationResult(string source, string constructionName, double thermalTransmittance, double calculatedThermalTransmittance)
            : base(constructionName, source, constructionName)
        {
            this.constructionName = constructionName;
            this.thermalTransmittance = thermalTransmittance;
            this.calculatedThermalTransmittance = calculatedThermalTransmittance;
        }

        public ConstructionCalculationResult(string source, string initialConstructionName, double initialThermalTransmittance, string constructionName, double thermalTransmittance, double calculatedThermalTransmittance)
            : base(constructionName, source, constructionName)
        {
            this.initialConstructionName = initialConstructionName;
            this.initialThermalTransmittance = initialThermalTransmittance;
            
            this.constructionName = constructionName;
            this.thermalTransmittance = thermalTransmittance;
            this.calculatedThermalTransmittance = calculatedThermalTransmittance;
        }

        public string InitialConstructionName
        {
            get
            {
                return initialConstructionName;
            }
        }

        public double InitialThermalTransmittance
        {
            get
            {
                return initialThermalTransmittance;
            }
        }

        public string ConstructionName
        {
            get
            {
                return constructionName;
            }
        }

        public double ThermalTransmittance
        {
            get
            {
                return thermalTransmittance;
            }
        }

        public double CalculatedThermalTransmittance
        {
            get
            {
                return calculatedThermalTransmittance;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(!base.FromJObject(jObject))
            {
                return false;
            }

            if (jObject.ContainsKey("InitialConstructionName"))
            {
                initialConstructionName = jObject.Value<string>("InitialConstructionName");
            }

            if (jObject.ContainsKey("InitialThermalTransmittance"))
            {
                initialThermalTransmittance = jObject.Value<double>("InitialThermalTransmittance");
            }

            if (jObject.ContainsKey("ConstructionName"))
            {
                constructionName = jObject.Value<string>("ConstructionName");
            }

            if (jObject.ContainsKey("ThermalTransmittance"))
            {
                thermalTransmittance = jObject.Value<double>("ThermalTransmittance");
            }

            if (jObject.ContainsKey("CalculatedThermalTransmittance"))
            {
                calculatedThermalTransmittance = jObject.Value<double>("CalculatedThermalTransmittance");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null )
            {
                return result;
            }

            if (initialConstructionName != null)
            {
                result.Add("InitialConstructionName", initialConstructionName);
            }

            if (!double.IsNaN(InitialThermalTransmittance))
            {
                result.Add("InitialThermalTransmittance", initialThermalTransmittance);
            }

            if (constructionName != null)
            {
                result.Add("ConstructionName", constructionName);
            }

            if (!double.IsNaN(thermalTransmittance))
            {
                result.Add("ThermalTransmittance", thermalTransmittance);
            }

            if (!double.IsNaN(calculatedThermalTransmittance))
            {
                result.Add("CalculatedThermalTransmittance", calculatedThermalTransmittance);
            }

            return result;

        }

    }
}
