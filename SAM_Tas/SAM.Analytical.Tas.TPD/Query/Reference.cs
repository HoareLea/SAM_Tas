using SAM.Core;
using SAM.Core.Systems;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

            if(zoneComponent is global::TPD.SystemComponent)
            {
                return Reference((global::TPD.SystemComponent)zoneComponent);
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

        //public static string Reference(this PlantController plantController)
        //{
        //    if (plantController == null)
        //    {
        //        return null;
        //    }

        //    return (plantController as dynamic).GUID;
        //}

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

        public static string Reference(this ComponentGroup componentGroup)
        {
            if (componentGroup == null)
            {
                return null;
            }

            return (componentGroup as dynamic).GUID;
        }

        public static string Reference(this PlantComponentGroup plantComponentGroup)
        {
            if (plantComponentGroup == null)
            {
                return null;
            }

            return (plantComponentGroup as dynamic).GUID;
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

        public static string Reference(this Pipe pipe)
        {
            if (pipe == null)
            {
                return null;
            }

            string reference_1 = Reference(pipe.GetDownstreamComponent());
            int port_1 = pipe.GetDownstreamComponentPort();
            string reference_2 = Reference(pipe.GetUpstreamComponent());
            int port_2 = pipe.GetUpstreamComponentPort();

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

        public static string Reference(this PlantSensorArc plantSensorArc)
        {
            if (plantSensorArc == null)
            {
                return null;
            }

            List<string> values = new List<string>()
            {
                plantSensorArc.GetController()?.Reference()?.ToString(),
                plantSensorArc.SensorPort.ToString(),
                plantSensorArc.GetPipe()?.Reference(),
                plantSensorArc.GetComponent()?.Reference(),
            };

            values.RemoveAll(x => string.IsNullOrWhiteSpace(x));

            return string.Join("-", values);
        }

        public static string Reference(this PlantLabel plantLabel)
        {
            if (plantLabel == null)
            {
                return null;
            }

            string reference = Reference(plantLabel.GetComponent());
            if (string.IsNullOrWhiteSpace(reference))
            {
                if(Create.Reference(plantLabel.GetController()) is IReference reference_Temp)
                {
                    reference = reference_Temp?.ToString();
                }

                if (string.IsNullOrWhiteSpace(reference))
                {
                    reference = Reference(plantLabel.GetPipe());
                }
            }

            if(string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            List<string> values = new List<string>() { plantLabel.GetPosition().x.ToString(), plantLabel.GetPosition().y.ToString(), reference };
            values.RemoveAll(x => x == null);

            return string.Join("-", values);
        }

        public static string Reference(this global::TPD.SystemLabel systemLabel)
        {
            if (systemLabel == null)
            {
                return null;
            }

            string reference = Reference(systemLabel.GetComponent());
            if (string.IsNullOrWhiteSpace(reference))
            {
                if (Create.Reference(systemLabel.GetController()) is IReference reference_Temp)
                {
                    reference = reference_Temp?.ToString();
                }

                if (string.IsNullOrWhiteSpace(reference))
                {
                    reference = Reference(systemLabel.GetDuct());
                }
            }

            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            List<string> values = new List<string>() { systemLabel.GetPosition().x.ToString(), systemLabel.GetPosition().y.ToString(), reference };
            values.RemoveAll(x => x == null);

            return string.Join("-", values);
        }
    }
}