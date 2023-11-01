using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class LayerThicknessCalculationResult : Result, IConstructionCalculationResult
    {
        private string constructionName;
        private int layerIndex;
        private double thermalTransmittance;
        private double calculatedThermalTransmittance;
        private double initialThermalTransmittance;
        private double thickness;

        public LayerThicknessCalculationResult(JObject jObject)
            :base(jObject)
        {

        }

        public LayerThicknessCalculationResult(LayerThicknessCalculationResult layerThicknessCalculationResult)
            :base(layerThicknessCalculationResult)
        {
            if(layerThicknessCalculationResult != null)
            {
                constructionName = layerThicknessCalculationResult.constructionName;
                layerIndex = layerThicknessCalculationResult.layerIndex;
                thermalTransmittance = layerThicknessCalculationResult.thermalTransmittance;
                calculatedThermalTransmittance = layerThicknessCalculationResult.calculatedThermalTransmittance;
                initialThermalTransmittance = layerThicknessCalculationResult.initialThermalTransmittance;
                thickness = layerThicknessCalculationResult.thickness;
            }
        }

        public LayerThicknessCalculationResult(string source, string constructionName, int layerIndex, double thickness, double initialThermalTransmittance, double thermalTransmittance, double calculatedThermalTransmittance)
            : base(constructionName, source, constructionName)
        {
            this.constructionName = constructionName;
            this.layerIndex = layerIndex;
            this.thickness = thickness;
            this.initialThermalTransmittance = initialThermalTransmittance;
            this.thermalTransmittance = thermalTransmittance;
            this.calculatedThermalTransmittance = calculatedThermalTransmittance;
        }

        public double Thickness
        {
            get
            {
                return thickness;
            }
        }

        public string ConstructionName
        {
            get
            {
                return constructionName;
            }
        }

        public int LayerIndex
        {
            get
            {
                return layerIndex;
            }
        }

        public double InitialThermalTransmittance
        {
            get
            {
                return initialThermalTransmittance;
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

            if(jObject.ContainsKey("ConstructionName"))
            {
                constructionName = jObject.Value<string>("ConstructionName");
            }

            if (jObject.ContainsKey("LayerIndex"))
            {
                layerIndex = jObject.Value<int>("LayerIndex");
            }

            if (jObject.ContainsKey("InitialThermalTransmittance"))
            {
                initialThermalTransmittance = jObject.Value<double>("InitialThermalTransmittance");
            }

            if (jObject.ContainsKey("ThermalTransmittance"))
            {
                thermalTransmittance = jObject.Value<double>("ThermalTransmittance");
            }

            if (jObject.ContainsKey("CalculatedThermalTransmittance"))
            {
                calculatedThermalTransmittance = jObject.Value<double>("CalculatedThermalTransmittance");
            }

            if (jObject.ContainsKey("Thickness"))
            {
                thickness = jObject.Value<double>("Thickness");
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

            if(constructionName != null)
            {
                result.Add("ConstructionName", constructionName);
            }

            result.Add("LayerIndex", layerIndex);

            if (!double.IsNaN(initialThermalTransmittance))
            {
                result.Add("InitialThermalTransmittance", initialThermalTransmittance);
            }

            if (!double.IsNaN(thermalTransmittance))
            {
                result.Add("ThermalTransmittance", thermalTransmittance);
            }

            if (!double.IsNaN(calculatedThermalTransmittance))
            {
                result.Add("CalculatedThermalTransmittance", calculatedThermalTransmittance);
            }

            if (!double.IsNaN(thickness))
            {
                result.Add("Thickness", thickness);
            }

            return result;

        }

    }
}
