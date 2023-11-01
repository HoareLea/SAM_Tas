using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class ApertureConstructionCalculationResult : Result, IApertureConstructionCalculationResult
    {
        private ApertureType apertureType = ApertureType.Undefined;
        
        private string initialApertureConstructionName = null;
        private double initialPaneThermalTransmittance = double.NaN;
        private double initialFrameThermalTransmittance = double.NaN;

        private string apertureConstructionName;
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
                apertureType = apertureConstructionCalculationResult.apertureType;

                initialApertureConstructionName = apertureConstructionCalculationResult.initialApertureConstructionName;
                initialPaneThermalTransmittance = apertureConstructionCalculationResult.initialPaneThermalTransmittance;
                initialFrameThermalTransmittance = apertureConstructionCalculationResult.initialFrameThermalTransmittance;

                apertureConstructionName = apertureConstructionCalculationResult.apertureConstructionName;
                paneThermalTransmittance = apertureConstructionCalculationResult.paneThermalTransmittance;
                frameThermalTransmittance = apertureConstructionCalculationResult.frameThermalTransmittance;

                calculatedPaneThermalTransmittance = apertureConstructionCalculationResult.calculatedPaneThermalTransmittance;
                calculatedFrameThermalTransmittance = apertureConstructionCalculationResult.calculatedFrameThermalTransmittance;
            }
        }

        public ApertureConstructionCalculationResult(
            string source, 
            ApertureType apertureType,
            string initialApertureConstructionName, 
            double initialPaneThermalTransmittance, 
            double initialFrameThermalTransmittance, 
            string apertureConstructionName, 
            double paneThermalTransmittance,
            double frameThermalTransmittance,
            double calculatedPaneThermalTransmittance,
            double calculatedFrameThermalTransmittance)
            : base(apertureConstructionName, source, apertureConstructionName)
        {
            this.apertureType = apertureType;

            this.initialApertureConstructionName = initialApertureConstructionName;
            this.initialPaneThermalTransmittance = initialPaneThermalTransmittance;
            this.initialFrameThermalTransmittance = initialFrameThermalTransmittance;

            this.apertureConstructionName = apertureConstructionName;
            this.paneThermalTransmittance = paneThermalTransmittance;
            this.frameThermalTransmittance = frameThermalTransmittance;

            this.calculatedPaneThermalTransmittance = calculatedPaneThermalTransmittance;
            this.calculatedFrameThermalTransmittance = calculatedFrameThermalTransmittance;
        }

        public ApertureType ApertureType
        {
            get
            {
                return apertureType;
            }
        }

        public string InitialApertureConstructionName
        {
            get
            {
                return initialApertureConstructionName;
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

        public string ApertureConstructionName
        {
            get
            {
                return apertureConstructionName;
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

            if (jObject.ContainsKey("ApertureType"))
            {
                apertureType = Core.Query.Enum<ApertureType>(jObject.Value<string>("ApertureType"));
            }

            if (jObject.ContainsKey("InitialApertureConstructionName"))
            {
                initialApertureConstructionName = jObject.Value<string>("InitialApertureConstructionName");
            }

            if (jObject.ContainsKey("InitialPaneThermalTransmittance"))
            {
                initialPaneThermalTransmittance = jObject.Value<double>("InitialPaneThermalTransmittance");
            }

            if (jObject.ContainsKey("InitialFrameThermalTransmittance"))
            {
                initialFrameThermalTransmittance = jObject.Value<double>("InitialFrameThermalTransmittance");
            }

            if (jObject.ContainsKey("ApertureConstructionName"))
            {
                apertureConstructionName = jObject.Value<string>("ApertureConstructionName");
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

            if(apertureType != ApertureType.Undefined)
            {
                result.Add("ApertureType", apertureType.ToString());
            }

            if (initialApertureConstructionName != null)
            {
                result.Add("InitialApertureConstructionName", initialApertureConstructionName);
            }

            if (!double.IsNaN(initialPaneThermalTransmittance))
            {
                result.Add("InitialPaneThermalTransmittance", initialPaneThermalTransmittance);
            }

            if (!double.IsNaN(initialFrameThermalTransmittance))
            {
                result.Add("InitialFrameThermalTransmittance", initialFrameThermalTransmittance);
            }

            if (apertureConstructionName != null)
            {
                result.Add("ApertureConstructionName", apertureConstructionName);
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
