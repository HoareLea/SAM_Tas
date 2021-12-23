using Newtonsoft.Json.Linq;

namespace SAM.Core.Tas
{
    public class SurfaceOutputSpec : SAMObject
    {
        public string Description { get; set; }
        public bool ApertureData { get; set; }
        public bool Condensation { get; set; }
        public bool Convection { get; set; }
        public bool SolarGain { get; set; }
        public bool Conduction { get; set; }
        public bool LongWave { get; set; }
        public bool Temperature { get; set; }

        public SurfaceOutputSpec(string name)
            : base(name)
        {

        }

        public SurfaceOutputSpec(JObject jObject)
            : base(jObject)
        {

        }

        public SurfaceOutputSpec(SurfaceOutputSpec surfaceOutputSpec)
            : base(surfaceOutputSpec)
        {
            if(surfaceOutputSpec != null)
            {
                Description = surfaceOutputSpec.Description;
                ApertureData = surfaceOutputSpec.ApertureData;
                Condensation = surfaceOutputSpec.Condensation;
                Convection = surfaceOutputSpec.Convection;
                SolarGain = surfaceOutputSpec.SolarGain;
                Conduction = surfaceOutputSpec.Conduction;
                LongWave = surfaceOutputSpec.LongWave;
                Temperature = surfaceOutputSpec.Temperature;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            if(! base.FromJObject(jObject))
            {
                return false;
            }

            if(jObject.ContainsKey("Description"))
            {
                Description = jObject.Value<string>("Description");
            }

            if (jObject.ContainsKey("ApertureData"))
            {
                ApertureData = jObject.Value<bool>("ApertureData");
            }

            if (jObject.ContainsKey("Condensation"))
            {
                Condensation = jObject.Value<bool>("Condensation");
            }

            if (jObject.ContainsKey("Conduction"))
            {
                Conduction = jObject.Value<bool>("Conduction");
            }

            if (jObject.ContainsKey("SolarGain"))
            {
                SolarGain = jObject.Value<bool>("SolarGain");
            }

            if (jObject.ContainsKey("Conduction"))
            {
                Conduction = jObject.Value<bool>("Conduction");
            }

            if (jObject.ContainsKey("LongWave"))
            {
                LongWave = jObject.Value<bool>("LongWave");
            }

            if (jObject.ContainsKey("Temperature"))
            {
                Temperature = jObject.Value<bool>("Temperature");
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject jObject =  base.ToJObject();
            if(jObject == null)
            {
                return jObject;
            }

            if(Description != null)
            {
                jObject.Add("Description", Description);
            }

            jObject.Add("ApertureData", ApertureData);
            jObject.Add("Condensation", Condensation);
            jObject.Add("Convection", Convection);
            jObject.Add("SolarGain", SolarGain);
            jObject.Add("Conduction", Conduction);
            jObject.Add("LongWave", LongWave);
            jObject.Add("Temperature", Temperature);

            return jObject;
        }
    }
}
