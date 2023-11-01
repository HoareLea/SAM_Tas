using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class ApertureConstructionCalculationData : IApertureConstructionCalculationData
    {
        public ApertureType ApertureType { get; set; } = ApertureType.Undefined;
        public string ApertureConstructionName { get; set; }
        public HashSet<string> ApertureConstructionNames { get; set; }
        public double PaneThermalTransmittance { get; set; }
        public double FrameThermalTransmittance { get; set; }
        public HeatFlowDirection HeatFlowDirection { get; set; }
        public bool External { get; set; }
        public Range<double> ThicknessRange { get; set; } = new Range<double>(0.001, 1);

        public ApertureConstructionCalculationData()
        {

        }
        
        public ApertureConstructionCalculationData(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ApertureConstructionCalculationData(ApertureConstructionCalculationData apertureConstructionCalculationData)
        {
            if(apertureConstructionCalculationData != null)
            {
                ApertureType = apertureConstructionCalculationData.ApertureType;
                ApertureConstructionName = apertureConstructionCalculationData.ApertureConstructionName;
                ApertureConstructionNames = apertureConstructionCalculationData.ApertureConstructionNames == null ? null : new HashSet<string>(apertureConstructionCalculationData.ApertureConstructionNames);
                PaneThermalTransmittance = apertureConstructionCalculationData.PaneThermalTransmittance;
                FrameThermalTransmittance = apertureConstructionCalculationData.FrameThermalTransmittance;
                HeatFlowDirection = apertureConstructionCalculationData.HeatFlowDirection;
                External = apertureConstructionCalculationData.External;
                ThicknessRange = apertureConstructionCalculationData.ThicknessRange == null ? null : new Range<double>(apertureConstructionCalculationData.ThicknessRange);
            }
        }

        public ApertureConstructionCalculationData(ApertureType apertureType, string apertureConstrcutionName, IEnumerable<string> apertureConstructionNames, double paneThermalTransmittance, double frameThermalTransmittance, HeatFlowDirection heatFlowDirection, bool external)
        {
            ApertureType = apertureType;
            
            ApertureConstructionName = apertureConstrcutionName;

            if(apertureConstructionNames != null)
            {
                this.ApertureConstructionNames = new HashSet<string>();
                foreach(string constructionName in apertureConstructionNames)
                {
                    this.ApertureConstructionNames.Add(constructionName);
                }
            }

            PaneThermalTransmittance = paneThermalTransmittance;
            FrameThermalTransmittance= frameThermalTransmittance;
            HeatFlowDirection = heatFlowDirection;
            External = external;
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("ApertureType"))
            {
                ApertureType = Core.Query.Enum<ApertureType>(jObject.Value<string>("ApertureType"));
            }

            if (jObject.ContainsKey("ApertureConstructionName"))
            {
                ApertureConstructionName = jObject.Value<string>("ApertureConstructionName");
            }

            if (jObject.ContainsKey("ApertureConstructionNames"))
            {
                JArray jArray = jObject.Value<JArray>("ApertureConstructionNames");
                if(jArray != null)
                {
                    ApertureConstructionNames = new HashSet<string>();
                    foreach(string apertureConstructionName in jArray)
                    {
                        ApertureConstructionNames.Add(apertureConstructionName);
                    }
                }
            }

            if (jObject.ContainsKey("PaneThermalTransmittance"))
            {
                PaneThermalTransmittance = jObject.Value<double>("PaneThermalTransmittance");
            }

            if (jObject.ContainsKey("FrameThermalTransmittance"))
            {
                FrameThermalTransmittance = jObject.Value<double>("FrameThermalTransmittance");
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

            if(ApertureConstructionName != null)
            {
                jObject.Add("ApertureConstructionName", ApertureConstructionName);
            }

            if(ApertureType != ApertureType.Undefined)
            {
                jObject.Add("ApertureType", ApertureType.ToString());
            }

            if(ApertureConstructionNames != null)
            {
                JArray jArray = new JArray();
                foreach(string apertureConstructionName in ApertureConstructionNames)
                {
                    jArray.Add(apertureConstructionName);
                }
                
                jObject.Add("ApertureConstructionNames", jArray);
            }

            if(!double.IsNaN(PaneThermalTransmittance))
            {
                jObject.Add("PaneThermalTransmittance", PaneThermalTransmittance);
            }

            if (!double.IsNaN(FrameThermalTransmittance))
            {
                jObject.Add("FrameThermalTransmittance", FrameThermalTransmittance);
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
