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

        public static string Reference(this PlantComponent plantComponent)
        {
            if (plantComponent == null)
            {
                return null;
            }

            return (plantComponent as dynamic).GUID;
        }

        public static string Reference(this global::TPD.SystemComponent systemComponent)
        {
            if(systemComponent == null)
            {
                return null;
            }

            return (systemComponent as dynamic).GUID;
        }

        public static string Reference(this global::TPD.ISystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            return (systemComponent as dynamic).GUID;
        }

        public static string Reference(this global::TPD.ISystem system)
        {
            if (system == null)
            {
                return null;
            }

            return (system as dynamic).GUID;
        }

        public static string Reference(this global::TPD.System system)
        {
            if (system == null)
            {
                return null;
            }

            return (system as dynamic).GUID;
        }

        public static string Reference(this global::TPD.ComponentGroup componentGroup)
        {
            if (componentGroup == null)
            {
                return null;
            }

            return (componentGroup as dynamic).GUID;
        }

        public static string Reference(this Duct duct)
        {
            if(duct == null)
            {
                return null;
            }

            string reference_1 = Reference(duct.GetDownstreamComponent());
            int port_1 = duct.GetDownstreamComponentPort();
            string reference_2 = Reference(duct.GetUpstreamComponent());
            int port_2 = duct.GetUpstreamComponentPort();

            List<string> values = new List<string>() { reference_1, port_1.ToString(), reference_2, port_2.ToString() };
            values.RemoveAll(x => x == null);

            return string.Join("-", values);
        }

        public static string Reference(this SensorArc sensorArc)
        {
            if (sensorArc == null)
            {
                return null;
            }

            List<string> values = new List<string>()
            {
                sensorArc.GetController()?.Reference()?.ToString(),
                sensorArc.SensorPort.ToString(),
                sensorArc.GetDuct()?.Reference(),
                sensorArc.GetComponent()?.Reference(),
            };

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join("-", values);
        }
    }
}