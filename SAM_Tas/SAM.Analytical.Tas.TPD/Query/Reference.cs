using SAM.Core;
using SAM.Core.Systems;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static string Reference(this ISystemObject systemObject)
        { 
            if(systemObject == null)
            {
                return null;
            }

            IParameterizedSAMObject parameterizedSAMObject = systemObject as IParameterizedSAMObject;
            if(parameterizedSAMObject == null)
            {
                return null;
            }

            if(!parameterizedSAMObject.TryGetValue(SystemObjectParameter.Reference, out string result))
            {
                return null;
            }

            return result;

        }

        public static string Reference(this ZoneComponent zoneComponent)
        {
            if(zoneComponent == null)
            {
                return null;
            }

            SystemZone systemZone = (zoneComponent as dynamic).GetZone();
            if(systemZone == null)
            {
                return null;
            }

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if(zoneLoad == null)
            {
                return null;
            }


            List<ZoneComponent> zoneComponents = systemZone.ZoneComponents<ZoneComponent>();

            int index = 0;
            foreach(ZoneComponent zoneComponent_Temp in zoneComponents)
            {
                if(zoneComponent == zoneComponent_Temp)
                {
                    break;
                }

                index++;
            }

            return string.Format("[{0}]::[{1}]", zoneLoad.GUID, index);
        }
    }
}