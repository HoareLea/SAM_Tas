using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class ThermalTransmittanceCalculationResult : Result, IThermalTransmittanceCalculationResult
    {
        private double lightTransmittance;
        private double lightReflectance;
        private double directSolarEnergyTransmittance;
        private double directSolarEnergyReflectance;
        private double directSolarEnergyAbosrtptance;
        private double totalSolarEnergyTransmittance;
        private double pilkingtonShortWavelengthCoefficient;
        private double pilkingtonLongWavelengthCoefficient;
        private ThermalTransmittances thermalTransmittances;

        public ThermalTransmittanceCalculationResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        
        public ThermalTransmittanceCalculationResult(ThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult)
            : base(thermalTransmittanceCalculationResult)
        {
            if(thermalTransmittanceCalculationResult != null)
            {
                lightTransmittance = thermalTransmittanceCalculationResult.lightTransmittance;
                lightReflectance = thermalTransmittanceCalculationResult.lightReflectance;
                directSolarEnergyTransmittance = thermalTransmittanceCalculationResult.directSolarEnergyTransmittance;
                directSolarEnergyReflectance = thermalTransmittanceCalculationResult.directSolarEnergyReflectance;
                directSolarEnergyAbosrtptance = thermalTransmittanceCalculationResult.directSolarEnergyAbosrtptance;
                totalSolarEnergyTransmittance = thermalTransmittanceCalculationResult.totalSolarEnergyTransmittance;
                pilkingtonShortWavelengthCoefficient = thermalTransmittanceCalculationResult.pilkingtonShortWavelengthCoefficient;
                pilkingtonLongWavelengthCoefficient = thermalTransmittanceCalculationResult.pilkingtonLongWavelengthCoefficient;

                thermalTransmittances = thermalTransmittanceCalculationResult.thermalTransmittances == null ? null : new ThermalTransmittances(thermalTransmittanceCalculationResult.thermalTransmittances);
            }
        }

        public ThermalTransmittanceCalculationResult(
            System.Guid constructionGuid, 
            string source,
            double lightTransmittance,
            double lightReflectance,
            double directSolarEnergyTransmittance,
            double directSolarEnergyReflectance,
            double directSolarEnergyAbosrtptance,
            double totalSolarEnergyTransmittance, 
            double pilkingtonShortWavelengthCoefficient,
            double pilkingtonLongWavelengthCoefficient,
            ThermalTransmittances thermalTransmittances)
            : base(typeof(ThermalTransmittanceCalculationResult).ToString(), source, constructionGuid.ToString())
        {
            this.lightTransmittance = lightTransmittance;
            this.lightReflectance = lightReflectance;
            this.directSolarEnergyTransmittance = directSolarEnergyTransmittance;
            this.directSolarEnergyReflectance = directSolarEnergyReflectance;
            this.directSolarEnergyAbosrtptance = directSolarEnergyAbosrtptance;
            this.totalSolarEnergyTransmittance = totalSolarEnergyTransmittance;
            this.pilkingtonShortWavelengthCoefficient = pilkingtonShortWavelengthCoefficient;
            this.pilkingtonLongWavelengthCoefficient = pilkingtonLongWavelengthCoefficient;
            this.thermalTransmittances = thermalTransmittances == null ? null : new ThermalTransmittances(thermalTransmittances);
        }

        public double LightTransmittance
        {
            get
            {
                return lightTransmittance;
            }
        }

        public double LightReflectance
        {
            get
            {
                return lightReflectance;
            }
        }

        public double DirectSolarEnergyTransmittance
        {
            get
            {
                return directSolarEnergyTransmittance;
            }
        }

        public double DirectSolarEnergyReflectance
        {
            get
            {
                return directSolarEnergyReflectance;
            }
        }

        public double DirectSolarEnergyAbosrtptance
        {
            get
            {
                return directSolarEnergyAbosrtptance;
            }
        }

        public double TotalSolarEnergyTransmittance
        {
            get
            {
                return totalSolarEnergyTransmittance;
            }
        }

        public double PilkingtonShortWavelengthCoefficient
        {
            get
            {
                return pilkingtonShortWavelengthCoefficient;
            }
        }

        public double PilkingtonLongWavelengthCoefficient
        {
            get
            {
                return pilkingtonLongWavelengthCoefficient;
            }
        }

        public double GetThermalTransmittance(PanelType panelType)
        {
            if(thermalTransmittances == null)
            {
                return double.NaN;
            }

            return thermalTransmittances.GetValue(panelType);
        }

        public double GetThermalTransmittance(HeatFlowDirection heatFlowDirection, bool external)
        {
            if (thermalTransmittances == null)
            {
                return double.NaN;
            }

            return thermalTransmittances.GetValue(heatFlowDirection, external);
        }

        public double GetTransparentThermalTransmittance()
        {
            if(thermalTransmittances == null)
            {
                return double.NaN;
            }

            return thermalTransmittances.GetTransparentValue();
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

            if (jObject.ContainsKey("LightTransmittance"))
            {
                lightTransmittance = jObject.Value<double>("LightTransmittance");
            }

            if (jObject.ContainsKey("LightReflectance"))
            {
                lightReflectance = jObject.Value<double>("LightReflectance");
            }

            if (jObject.ContainsKey("DirectSolarEnergyTransmittance"))
            {
                directSolarEnergyTransmittance = jObject.Value<double>("DirectSolarEnergyTransmittance");
            }

            if (jObject.ContainsKey("DirectSolarEnergyReflectance"))
            {
                directSolarEnergyReflectance = jObject.Value<double>("DirectSolarEnergyReflectance");
            }

            if (jObject.ContainsKey("DirectSolarEnergyAbosrtptance"))
            {
                directSolarEnergyAbosrtptance = jObject.Value<double>("DirectSolarEnergyAbosrtptance");
            }

            if (jObject.ContainsKey("TotalSolarEnergyTransmittance"))
            {
                totalSolarEnergyTransmittance = jObject.Value<double>("TotalSolarEnergyTransmittance");
            }

            if (jObject.ContainsKey("PilkingtonShortWavelengthCoefficient"))
            {
                pilkingtonShortWavelengthCoefficient = jObject.Value<double>("PilkingtonShortWavelengthCoefficient");
            }

            if (jObject.ContainsKey("PilkingtonLongWavelengthCoefficient"))
            {
                pilkingtonLongWavelengthCoefficient = jObject.Value<double>("PilkingtonLongWavelengthCoefficient");
            }

            if (jObject.ContainsKey("ThermalTransmittances"))
            {
                thermalTransmittances = new ThermalTransmittances(jObject.Value<JObject>("ThermalTransmittances"));
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

            if (!double.IsNaN(lightTransmittance))
            {
                jObject.Add("LightTransmittance", lightTransmittance);
            }

            if (!double.IsNaN(lightReflectance))
            {
                jObject.Add("LightReflectance", lightReflectance);
            }

            if (!double.IsNaN(directSolarEnergyTransmittance))
            {
                jObject.Add("DirectSolarEnergyTransmittance", directSolarEnergyTransmittance);
            }

            if (!double.IsNaN(directSolarEnergyReflectance))
            {
                jObject.Add("DirectSolarEnergyReflectance", directSolarEnergyReflectance);
            }

            if (!double.IsNaN(directSolarEnergyAbosrtptance))
            {
                jObject.Add("DirectSolarEnergyAbosrtptance", directSolarEnergyAbosrtptance);
            }

            if (!double.IsNaN(totalSolarEnergyTransmittance))
            {
                jObject.Add("TotalSolarEnergyTransmittance", totalSolarEnergyTransmittance);
            }

            if (!double.IsNaN(pilkingtonShortWavelengthCoefficient))
            {
                jObject.Add("PilkingtonShortWavelengthCoefficient", pilkingtonShortWavelengthCoefficient);
            }

            if (!double.IsNaN(pilkingtonLongWavelengthCoefficient))
            {
                jObject.Add("PilkingtonLongWavelengthCoefficient", pilkingtonLongWavelengthCoefficient);
            }

            if (thermalTransmittances != null)
            {
                jObject.Add("ThermalTransmittances", thermalTransmittances.ToJObject());
            }

            return jObject;
        }
    }
}
