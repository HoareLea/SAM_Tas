using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.ISystemComponent systemComponent, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || systemComponent == null)
            {
                return null;
            }

            if (systemComponent is SystemZone)
            {
                return Add(systemPlantRoom, (SystemZone)systemComponent, tPDDoc);
            }

            if (systemComponent is ComponentGroup)
            {
                return Add(systemPlantRoom, (ComponentGroup)systemComponent, tPDDoc);
            }

            if(systemComponent is Junction)
            {
                return Add(systemPlantRoom, (Junction)systemComponent, tPDDoc);
            }

            if(systemComponent is Exchanger)
            {
                return Add(systemPlantRoom, (Exchanger)systemComponent, tPDDoc);
            }

            if (systemComponent is Fan)
            {
                return Add(systemPlantRoom, (Fan)systemComponent, tPDDoc);
            }

            if (systemComponent is HeatingCoil)
            {
                return Add(systemPlantRoom, (HeatingCoil)systemComponent, tPDDoc);
            }

            if (systemComponent is CoolingCoil)
            {
                return Add(systemPlantRoom, (CoolingCoil)systemComponent, tPDDoc);
            }

            if (systemComponent is Damper)
            {
                return Add(systemPlantRoom, (Damper)systemComponent, tPDDoc);
            }

            //List<System.Type> types = Core.Query.Types(systemComponent, @"C:\Users\jakub\GitHub\HoareLea\SAM_Tas\references_buildonly\Interop.TPD.dll"); 

            return null;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SystemZone systemZone, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || systemZone == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemSpace systemSpace = systemZone.ToSAM();
            systemPlantRoom.Add(systemSpace);

            SystemSpaceResult systemSpaceResult = systemZone.ToSAM_SpaceSystemResult(systemPlantRoom, start, end);
            systemPlantRoom.Add(systemSpaceResult);

            systemPlantRoom.Connect(systemSpaceResult, systemSpace);

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            List<ZoneComponent> zoneComponents =  systemZone.ZoneComponents<ZoneComponent>();
            foreach (ZoneComponent zoneComponent in zoneComponents)
            {
                ISystemSpaceComponent systemSpaceComponent = zoneComponent?.ToSAM();
                if(systemSpaceComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Add(systemSpaceComponent);

                result.Add(systemSpaceComponent);

                ISystemComponentResult systemComponentResult = zoneComponent.ToSAM_SystemComponentResult(start, end);
                if (systemComponentResult == null)
                {
                    continue;
                }

                systemPlantRoom.Add(systemComponentResult);

                systemPlantRoom.Connect(systemComponentResult, systemSpaceComponent);

                result.Add(systemComponentResult);
            }

            foreach(ISystemJSAMObject systemJSAMObject in result)
            {
                ISystemSpaceComponent systemSpaceComponent = systemJSAMObject as ISystemSpaceComponent;
                if(systemSpaceComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Connect(systemSpaceComponent, systemSpace);
            }

            result.Add(systemSpace);
            result.Add(systemSpaceResult);

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Junction junction, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || junction == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemAirJunction systemAirJunction = junction.ToSAM();
            systemPlantRoom.Add(systemAirJunction);

            SystemAirJunctionResult systemAirJunctionResult = junction.ToSAM_SystemAirJunctionResult(start, end);
            systemPlantRoom.Add(systemAirJunctionResult);

            systemPlantRoom.Connect(systemAirJunctionResult, systemAirJunction);

            return new List<ISystemJSAMObject>() { systemAirJunction, systemAirJunctionResult };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Exchanger exchanger, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || exchanger == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemExchanger systemExchanger = exchanger.ToSAM();
            systemPlantRoom.Add(systemExchanger);

            SystemExchangerResult systemExchangerResult = exchanger.ToSAM_SystemExchangerResult(start, end);
            systemPlantRoom.Add(systemExchangerResult);

            systemPlantRoom.Connect(systemExchangerResult, systemExchanger);

            return new List<ISystemJSAMObject>() { systemExchanger, systemExchangerResult };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Fan fan, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || fan == null || tPDDoc == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemFan systemFan = fan.ToSAM();
            systemPlantRoom.Add(systemFan);

            SystemFanResult systemFanResult = fan.ToSAM_SystemFanResult(start, end);
            systemPlantRoom.Add(systemFanResult);

            systemPlantRoom.Connect(systemFanResult, systemFan);

            return new List<ISystemJSAMObject>() { systemFan, systemFanResult };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, CoolingCoil coolingCoil, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || coolingCoil == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemCoolingCoil systemCoolingCoil = coolingCoil.ToSAM();
            systemPlantRoom.Add(systemCoolingCoil);

            SystemCoolingCoilResult systemCoolingCoilResult = coolingCoil.ToSAM_SystemCoolingCoilResult(start, end);
            systemPlantRoom.Add(systemCoolingCoilResult);

            systemPlantRoom.Connect(systemCoolingCoilResult, systemCoolingCoil);

            return new List<ISystemJSAMObject>() { systemCoolingCoil, systemCoolingCoilResult };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, HeatingCoil heatingCoil, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || heatingCoil == null)
            {
                return null;
            }

            int start = tPDDoc.StartHour();
            int end = tPDDoc.EndHour();

            SystemHeatingCoil systemHeatingCoil = heatingCoil.ToSAM();
            systemPlantRoom.Add(systemHeatingCoil);

            SystemHeatingCoilResult systemHeatingCoilResult = heatingCoil.ToSAM_SystemHeatingCoilResult(start, end);
            systemPlantRoom.Add(systemHeatingCoilResult);

            systemPlantRoom.Connect(systemHeatingCoilResult, systemHeatingCoil);

            return new List<ISystemJSAMObject>() { systemHeatingCoil, systemHeatingCoilResult };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Damper damper, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || damper == null)
            {
                return null;
            }

            SystemDamper systemDamper = damper.ToSAM();
            systemPlantRoom.Add(systemDamper);

            return new List<ISystemJSAMObject>() { systemDamper };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, ComponentGroup componentGroup, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || componentGroup == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);
            if(systemComponents != null)
            {
                foreach(global::TPD.SystemComponent systemComponent_Temp in systemComponents)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = Add(systemPlantRoom, systemComponent_Temp, tPDDoc);
                    if(systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            AirSystemGroup airSystemGroup = componentGroup.ToSAM();
            systemPlantRoom.Add(airSystemGroup);

            foreach(ISystemJSAMObject systemJSAMObject in result)
            {
                Core.Systems.ISystemComponent systemComponent = systemJSAMObject as Core.Systems.ISystemComponent;
                if(systemComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Connect(airSystemGroup, systemComponent);
            }

            result.Add(airSystemGroup);

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.System system, TPDDoc tPDDoc)
        {
            if (systemPlantRoom == null || system == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            AirSystem airSystem = system.ToSAM();
            if(airSystem == null)
            {
                return null;
            }

            systemPlantRoom.Add(airSystem);

            List<global::TPD.SystemComponent> systemComponents = system.SystemComponents<global::TPD.SystemComponent>();
            if (systemComponents != null)
            {
                foreach (global::TPD.SystemComponent systemComponent in systemComponents)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(systemComponent, tPDDoc);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            foreach(ISystemJSAMObject systemJSAMObject in result)
            {
                Core.Systems.ISystemComponent systemComponent = systemJSAMObject as Core.Systems.ISystemComponent;
                if (systemComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Connect(airSystem, systemComponent);
            }

            result.Add(airSystem);

            Connect(systemPlantRoom, system);

            return result;
        }
    }
}