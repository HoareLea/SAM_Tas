using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemSpace : SAMObject, ISystemObject
    {
        private double area;
        private double volume;

        public SystemSpace(string name, double area, double volume) 
            :base(name)
        { 
            this.area = area;
            this.volume = volume;
        }

        public SystemSpace(JObject jObject)
            : base(jObject) 
        {
            FromJObject(jObject);
        }

        public SystemSpace(SystemSpace systemSpace)
            : base (systemSpace) 
        {
            if(systemSpace != null)
            {
                area = systemSpace.area;
                volume = systemSpace.volume;
            }
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

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
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

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if(result == null)
            {
                return null;
            }

            if(!double.IsNaN(area))
            {
                result.Add("Area", area);
            }

            if (!double.IsNaN(volume))
            {
                result.Add("Volume", volume);
            }

            return result;
        }
    }
}
