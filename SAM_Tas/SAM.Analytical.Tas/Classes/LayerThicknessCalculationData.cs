using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class LayerThicknessCalculationData: IConstructionCalculationData
    {
        public string ConstructionName { get; set; }
        public int LayerIndex { get; set; }
        public double ThermalTransmittance { get; set; }
        public HeatFlowDirection HeatFlowDirection { get; set; }
        public bool External { get; set; }
        public Range<double> ThicknessRange { get; set; } = new Range<double>(0.001, 1);

        public LayerThicknessCalculationData()
        {

        }
        
        public LayerThicknessCalculationData(JObject jObject)
        {
            FromJObject(jObject);
        }

        public LayerThicknessCalculationData(LayerThicknessCalculationData layerThicknessCalculationData)
        {
            if(layerThicknessCalculationData != null)
            {
                ConstructionName = layerThicknessCalculationData.ConstructionName;
                LayerIndex = layerThicknessCalculationData.LayerIndex;
                ThermalTransmittance = layerThicknessCalculationData.ThermalTransmittance;
                HeatFlowDirection = layerThicknessCalculationData.HeatFlowDirection;
                External = layerThicknessCalculationData.External;
                ThicknessRange = layerThicknessCalculationData.ThicknessRange == null ? null : new Range<double>(layerThicknessCalculationData.ThicknessRange);
            }
        }

        public LayerThicknessCalculationData(string constructionName, int layerIndex, double thermalTransmittance, HeatFlowDirection heatFlowDirection, bool external)
        {
            ConstructionName = constructionName;
            LayerIndex = layerIndex;
            ThermalTransmittance = thermalTransmittance;
            HeatFlowDirection = heatFlowDirection;
            External = external;
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("ConstructionName"))
            {
                ConstructionName = jObject.Value<string>("ConstructionName");
            }

            if (jObject.ContainsKey("LayerIndex"))
            {
                LayerIndex = jObject.Value<int>("LayerIndex");
            }

            if (jObject.ContainsKey("ThermalTransmittance"))
            {
                ThermalTransmittance = jObject.Value<double>("ThermalTransmittance");
            }

            if (jObject.ContainsKey("HeatFlowDirection"))
            {
                HeatFlowDirection = Core.Query.Enum<HeatFlowDirection>(jObject.Value<string>("HeatFlowDirection"));
            }

            if (jObject.ContainsKey("External"))
            {
                External = jObject.Value<bool>("External");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if(ConstructionName != null)
            {
                jObject.Add("ConstructionName", ConstructionName);
            }

            jObject.Add("LayerIndex", LayerIndex);

            if(!double.IsNaN(ThermalTransmittance))
            {
                jObject.Add("ThermalTransmittance", ThermalTransmittance);
            }

            if (HeatFlowDirection != HeatFlowDirection.Undefined)
            {
                jObject.Add("HeatFlowDirection", HeatFlowDirection.ToString());
            }

            jObject.Add("External", External);

            if(ThicknessRange != null)
            {
                jObject.Add("ThicknessRange", ThicknessRange.ToJObject());
            }

            return jObject;
        }
    }
}
