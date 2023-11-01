using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class ApertureConstructionCalculationResult : Result, IApertureConstructionCalculationResult
    {
        private string initialConstructionName = null;
        private double initialPaneThermalTransmittance = double.NaN;
        private double initialFrameThermalTransmittance = double.NaN;

        private string constructionName;
        private double paneThermalTransmittance;
        private double frameThermalTransmittance;

        private double calculatedPaneThermalTransmittance;
        private double calculatedFrameThermalTransmittance;

        public ApertureConstructionCalculationResult(JObject jObject)
            :base(jObject)
        {

        }

        public ApertureConstructionCalculationResult(ApertureConstructionCalculationResult apertureConstructionCalculationResult)
            :base(apertureConstructionCalculationResult)
        {
            if(apertureConstructionCalculationResult != null)
            {
                initialConstructionName = apertureConstructionCalculationResult.initialConstructionName;
                initialPaneThermalTransmittance = apertureConstructionCalculationResult.initialPaneThermalTransmittance;
                initialFrameThermalTransmittance = apertureConstructionCalculationResult.initialFrameThermalTransmittance;

                constructionName = apertureConstructionCalculationResult.constructionName;
                paneThermalTransmittance = apertureConstructionCalculationResult.paneThermalTransmittance;
                frameThermalTransmittance = apertureConstructionCalculationResult.frameThermalTransmittance;

                calculatedPaneThermalTransmittance = apertureConstructionCalculationResult.calculatedPaneThermalTransmittance;
                calculatedFrameThermalTransmittance = apertureConstructionCalculationResult.calculatedFrameThermalTransmittance;
            }
        }

        public ApertureConstructionCalculationResult(
            string source, 
            string initialConstructionName, 
            double initialPaneThermalTransmittance, 
            double initialFrameThermalTransmittance, 
            string constructionName, 
            double paneThermalTransmittance,
            double frameThermalTransmittance,
            double calculatedPaneThermalTransmittance,
            double calculatedFrameThermalTransmittance)
            : base(constructionName, source, constructionName)
        {
            this.initialConstructionName = initialConstructionName;
            this.initialPaneThermalTransmittance = initialPaneThermalTransmittance;
            this.initialFrameThermalTransmittance = initialFrameThermalTransmittance;

            this.constructionName = constructionName;
            this.paneThermalTransmittance = paneThermalTransmittance;
            this.frameThermalTransmittance = frameThermalTransmittance;

            this.calculatedPaneThermalTransmittance = calculatedPaneThermalTransmittance;
            this.calculatedFrameThermalTransmittance = calculatedFrameThermalTransmittance;
        }

        public string InitialConstructionName
        {
            get
            {
                return initialConstructionName;
            }
        }

        public double InitialPaneThermalTransmittance
        {
            get
            {
                return initialPaneThermalTransmittance;
            }
        }

        public double InitialFrameThermalTransmittance
        {
            get
            {
                return initialFrameThermalTransmittance;
            }
        }

        public string ConstructionName
        {
            get
            {
                return constructionName;
            }
        }

        public double PaneThermalTransmittance
        {
            get
            {
                return paneThermalTransmittance;
            }
        }

        public double FrameThermalTransmittance
        {
            get
            {
                return frameThermalTransmittance;
            }
        }

        public double CalculatedPaneThermalTransmittance
        {
            get
            {
                return calculatedPaneThermalTransmittance;
            }
        }

        public double CalculatedFrameThermalTransmittance
        {
            get
            {
                return calculatedFrameThermalTransmittance;
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

            if (jObject.ContainsKey("InitialPaneThermalTransmittance"))
            {
                initialPaneThermalTransmittance = jObject.Value<double>("InitialPaneThermalTransmittance");
            }

            if (jObject.ContainsKey("InitialFrameThermalTransmittance"))
            {
                initialFrameThermalTransmittance = jObject.Value<double>("InitialFrameThermalTransmittance");
            }

            if (jObject.ContainsKey("ConstructionName"))
            {
                constructionName = jObject.Value<string>("ConstructionName");
            }

            if (jObject.ContainsKey("PaneThermalTransmittance"))
            {
                paneThermalTransmittance = jObject.Value<double>("PaneThermalTransmittance");
            }

            if (jObject.ContainsKey("FrameThermalTransmittance"))
            {
                frameThermalTransmittance = jObject.Value<double>("FrameThermalTransmittance");
            }

            if (jObject.ContainsKey("CalculatedPaneThermalTransmittance"))
            {
                calculatedPaneThermalTransmittance = jObject.Value<double>("CalculatedPaneThermalTransmittance");
            }

            if (jObject.ContainsKey("CalculatedFrameThermalTransmittance"))
            {
                calculatedFrameThermalTransmittance = jObject.Value<double>("CalculatedFrameThermalTransmittance");
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

            if (!double.IsNaN(initialPaneThermalTransmittance))
            {
                result.Add("InitialPaneThermalTransmittance", initialPaneThermalTransmittance);
            }

            if (!double.IsNaN(initialFrameThermalTransmittance))
            {
                result.Add("InitialFrameThermalTransmittance", initialFrameThermalTransmittance);
            }

            if (constructionName != null)
            {
                result.Add("ConstructionName", constructionName);
            }

            if (!double.IsNaN(paneThermalTransmittance))
            {
                result.Add("PaneThermalTransmittance", paneThermalTransmittance);
            }

            if (!double.IsNaN(frameThermalTransmittance))
            {
                result.Add("FrameThermalTransmittance", frameThermalTransmittance);
            }

            if (!double.IsNaN(calculatedPaneThermalTransmittance))
            {
                result.Add("CalculatedPaneThermalTransmittance", calculatedPaneThermalTransmittance);
            }

            if (!double.IsNaN(calculatedFrameThermalTransmittance))
            {
                result.Add("CalculatedFrameThermalTransmittance", calculatedFrameThermalTransmittance);
            }

            return result;

        }

    }
}
