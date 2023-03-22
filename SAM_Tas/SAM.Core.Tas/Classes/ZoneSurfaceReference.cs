using Newtonsoft.Json.Linq;

namespace SAM.Core.Tas
{
    public class ZoneSurfaceReference : IJSAMObject
    {
        public int SurfaceNumber { get; private set; } = -1;

        public string ZoneGuid { get; private set; } = null;

        public ZoneSurfaceReference(int surfaceNumber, string zoneGuid)
        {
            SurfaceNumber = surfaceNumber;
            ZoneGuid = zoneGuid;
        }

        public ZoneSurfaceReference(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ZoneSurfaceReference(ZoneSurfaceReference zoneSurfaceReference)
        {
            if(zoneSurfaceReference != null)
            {
                SurfaceNumber = zoneSurfaceReference.SurfaceNumber;
                ZoneGuid = zoneSurfaceReference.ZoneGuid;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject.ContainsKey("SurfaceNumber"))
            {
                SurfaceNumber = jObject.Value<int>("SurfaceNumber");
            }

            if (jObject.ContainsKey("ZoneGuid"))
            {
                ZoneGuid = jObject.Value<string>("ZoneGuid");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject result =  new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if(SurfaceNumber != -1)
            {
                result.Add("SurfaceNumber", SurfaceNumber);
            }

            if (ZoneGuid != null)
            {
                result.Add("ZoneGuid", ZoneGuid);
            }

            return result;
        }
    }
}
