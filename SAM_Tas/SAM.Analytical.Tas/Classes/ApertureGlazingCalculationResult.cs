using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Tas
{
    public class ApertureGlazingCalculationResult : GlazingCalculationResult
    {
        private double frameTotalSolarEnergyTransmittance;
        private double frameLightTransmittance;
        private double frameThermalTransmittance;

        public ApertureGlazingCalculationResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        

        public ApertureGlazingCalculationResult(ApertureGlazingCalculationResult apertureGlazingCalculationResult)
            : base(apertureGlazingCalculationResult)
        {
            if(apertureGlazingCalculationResult != null)
            {
                frameTotalSolarEnergyTransmittance = apertureGlazingCalculationResult.frameTotalSolarEnergyTransmittance;
                frameLightTransmittance = apertureGlazingCalculationResult.frameLightTransmittance;
                frameThermalTransmittance = apertureGlazingCalculationResult.frameThermalTransmittance;
            }
        }

        public ApertureGlazingCalculationResult(GlazingCalculationResult glazingCalculationResult, double frameTotalSolarEnergyTransmittance, double frameLightTransmittance, double frameThermalTransmittance)
            : base(glazingCalculationResult)
        {
            this.frameTotalSolarEnergyTransmittance = frameTotalSolarEnergyTransmittance;
            this.frameLightTransmittance = frameLightTransmittance;
            this.frameThermalTransmittance = frameThermalTransmittance;
        }

        public ApertureGlazingCalculationResult(System.Guid constructionGuid, string source, double totalSolarEnergyTransmittance, double lightTransmittance, double thermalTransmittance, double frameTotalSolarEnergyTransmittance, double frameLightTransmittance, double frameThermalTransmittance)
            : base(constructionGuid, source, totalSolarEnergyTransmittance, lightTransmittance, thermalTransmittance)
        {
            this.frameTotalSolarEnergyTransmittance = frameTotalSolarEnergyTransmittance;
            this.frameLightTransmittance = frameLightTransmittance;
            this.frameThermalTransmittance = frameThermalTransmittance;
        }

        public double FrameTotalSolarEnergyTransmittance
        {
            get
            {
                return frameTotalSolarEnergyTransmittance;
            }
        }

        public double FrameLightTransmittance
        {
            get
            {
                return frameLightTransmittance;
            }
        }

        public double FrameThermalTransmittance
        {
            get
            {
                return frameThermalTransmittance;
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

            if(jObject.ContainsKey("FrameTotalSolarEnergyTransmittance"))
            {
                frameTotalSolarEnergyTransmittance = jObject.Value<double>("FrameTotalSolarEnergyTransmittance");
            }

            if (jObject.ContainsKey("FrameLightTransmittance"))
            {
                frameLightTransmittance = jObject.Value<double>("FrameLightTransmittance");
            }

            if (jObject.ContainsKey("FrameThermalTransmittance"))
            {
                frameThermalTransmittance = jObject.Value<double>("FrameThermalTransmittance");
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
                jObject.Add("FrameTotalSolarEnergyTransmittance", frameTotalSolarEnergyTransmittance);
            }

            if (!double.IsNaN(frameLightTransmittance))
            {
                jObject.Add("FrameLightTransmittance", frameLightTransmittance);
            }

            if (!double.IsNaN(frameThermalTransmittance))
            {
                jObject.Add("FrameThermalTransmittance", frameThermalTransmittance);
            }

            return jObject;
        }

        /// <summary>
        /// Gets Thermal Transmittance for given pane area percentage (percent of pane in total aperture area)
        /// </summary>
        /// <param name="paneAreaPercentage">Percentage of pane area</param>
        /// <returns>Thermal Transmittance</returns>
        public double GetThermalTransmittance(double paneAreaPercentage)
        {
            if(double.IsNaN(paneAreaPercentage))
            {
                return double.NaN;
            }

            if(double.IsNaN(ThermalTransmittance) && double.IsNaN(frameThermalTransmittance))
            {
                return double.NaN;
            }

            return (ThermalTransmittance * paneAreaPercentage / 100 ) + (frameThermalTransmittance * (100 - paneAreaPercentage) / 100);
        }
    }
}
