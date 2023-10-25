using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class LayerThicknessCalculationResult :Result
    {
        private string constructionName;
        private int layerIndex;
        private double thermalTransmittance;
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
                constructionName = layerThicknessCalculationResult.ConstructionName;
                layerIndex = layerThicknessCalculationResult.LayerIndex;
                thermalTransmittance = layerThicknessCalculationResult.ThermalTransmittance;
                thickness = layerThicknessCalculationResult.Thickness;
            }
        }

        public LayerThicknessCalculationResult(string source, string constructionName, int layerIndex, double thickness, double thermalTransmittance)
            : base(constructionName, source, constructionName)
        {
            this.constructionName = constructionName;
            this.layerIndex = layerIndex;
            this.thickness = thickness;
            this.thermalTransmittance = thermalTransmittance;
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

        public double ThermalTransmittance
        {
            get
            {
                return thermalTransmittance;
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

            if (jObject.ContainsKey("ThermalTransmittance"))
            {
                thermalTransmittance = jObject.Value<double>("ThermalTransmittance");
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

            if(!double.IsNaN(thermalTransmittance))
            {
                result.Add("ThermalTransmittance", thermalTransmittance);
            }

            if (!double.IsNaN(thickness))
            {
                result.Add("Thickness", thickness);
            }

            return result;

        }

    }
}
