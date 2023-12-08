using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemSpaceResult : SystemIndexedResult
    {
        private double area;
        private double volume;

        private double heatingDuty;
        private double coolingDuty;
        private double designFlowRate;

        public SystemSpaceResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        

        public SystemSpaceResult(SystemSpaceResult spaceSystemResult)
            : base(spaceSystemResult)
        {
            if(spaceSystemResult != null)
            {
                area= spaceSystemResult.area;
                volume= spaceSystemResult.volume;

                heatingDuty= spaceSystemResult.heatingDuty;
                coolingDuty = spaceSystemResult.coolingDuty;
                designFlowRate= spaceSystemResult.designFlowRate;
            }
        }

        public SystemSpaceResult(string uniqueId, string name, string source, double area, double volume, double heatingDuty, double coolingDuty, double designFlowRate, Dictionary<string, IndexedDoubles> dictionary)
            : base(name, source, uniqueId, dictionary)
        {
            this.area = area;
            this.volume = volume;
            
            this.heatingDuty = heatingDuty;
            this.coolingDuty = coolingDuty;
            this.designFlowRate = designFlowRate;
        }

        public double Area
        {
            get
            {
                return area;
            }
        }

        public double Volume
        {
            get
            {
                return volume;
            }
        }

        public double HeatingDuty
        {
            get
            {
                return heatingDuty;
            }
        }

        public double CoolingDuty
        {
            get
            {
                return coolingDuty;
            }
        }

        public double DesignFlowRate
        {
            get
            {
                return designFlowRate;
            }
        }
        
        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return result;
            }

            if(jObject.ContainsKey("Area"))
            {
                area = jObject.Value<double>("Area");
            }

            if (jObject.ContainsKey("Volume"))
            {
                volume = jObject.Value<double>("Volume");
            }

            if (jObject.ContainsKey("HeatingDuty"))
            {
                heatingDuty = jObject.Value<double>("HeatingDuty");
            }

            if (jObject.ContainsKey("CoolingDuty"))
            {
                coolingDuty = jObject.Value<double>("CoolingDuty");
            }

            if (jObject.ContainsKey("DesignFlowRate"))
            {
                designFlowRate = jObject.Value<double>("DesignFlowRate");
            }

            return result;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            if(double.IsNaN(area))
            {
                jObject.Add("Area", area);
            }

            if (double.IsNaN(volume))
            {
                jObject.Add("Volume", volume);
            }

            if (double.IsNaN(heatingDuty))
            {
                jObject.Add("HeatingDuty", heatingDuty);
            }

            if (double.IsNaN(coolingDuty))
            {
                jObject.Add("CoolingDuty", coolingDuty);
            }

            if (double.IsNaN(designFlowRate))
            {
                jObject.Add("DesignFlowRate", designFlowRate);
            }

            return jObject;
        }
    }
}
