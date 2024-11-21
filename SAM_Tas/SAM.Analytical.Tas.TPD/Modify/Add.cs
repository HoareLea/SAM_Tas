using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Controller controller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || controller == null || tPDDoc == null)
            {
                return null;
            }

            ISystemController systemController = controller.ToSAM();
            if (systemController == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();
            result.Add(systemController);

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PlantController plantController, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || plantController == null || tPDDoc == null)
            {
                return null;
            }

            ISystemController systemController = plantController.ToSAM();
            if (systemController == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();
            if(systemPlantRoom.Add(systemController))
            {
                result.Add(systemController);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.ISystemComponent systemComponent, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || systemComponent == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            if (systemComponent is SystemZone)
            {
                return Add(systemPlantRoom, (SystemZone)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is ComponentGroup)
            {
                return Add(systemPlantRoom, (ComponentGroup)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is Junction)
            {
                return Add(systemPlantRoom, (Junction)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is Exchanger)
            {
                return Add(systemPlantRoom, (Exchanger)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is DesiccantWheel)
            {
                return Add(systemPlantRoom, (DesiccantWheel)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is global::TPD.Fan)
            {
                return Add(systemPlantRoom, (global::TPD.Fan)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is global::TPD.HeatingCoil)
            {
                return Add(systemPlantRoom, (global::TPD.HeatingCoil)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is global::TPD.CoolingCoil)
            {
                return Add(systemPlantRoom, (global::TPD.CoolingCoil)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is Damper)
            {
                return Add(systemPlantRoom, (Damper)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is SystemZone)
            {
                return Add(systemPlantRoom, (SystemZone)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is Optimiser)
            {
                return Add(systemPlantRoom, (Optimiser)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is SteamHumidifier)
            {
                return Add(systemPlantRoom, (SteamHumidifier)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is SprayHumidifier)
            {
                return Add(systemPlantRoom, (SprayHumidifier)systemComponent, tPDDoc, componentConversionSettings);
            }

            if (systemComponent is DXCoil)
            {
                return Add(systemPlantRoom, (DXCoil)systemComponent, tPDDoc, componentConversionSettings);
            }

            return null;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Optimiser optimiser, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || optimiser == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            Core.Systems.ISystemComponent systemComponent = null;
            ISystemComponentResult systemComponentResult = null;

            switch (optimiser.Flags)
            {
                case 1:
                    systemComponent = optimiser.ToSAM_SystemMixingBox();
                    systemComponentResult = componentConversionSettings.IncludeResults ? optimiser.ToSAM_SystemMixingBoxResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1) : null;
                    break;

                case 0:
                    systemComponent = optimiser.ToSAM_SystemEconomiser();
                    systemComponentResult = componentConversionSettings.IncludeResults ? optimiser.ToSAM_SystemEconomiserResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1) : null;
                    break;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            if (systemComponent != null)
            {
                systemPlantRoom.Add(systemComponent);
                result.Add(systemComponent);
            }

            if (systemComponentResult != null)
            {
                systemPlantRoom.Add(systemComponentResult);
                result.Add(systemComponentResult);
            }

            if (systemComponent != null && systemComponentResult != null)
            {
                systemPlantRoom.Connect(systemComponentResult, systemComponent);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SystemZone systemZone, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || systemZone == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            SystemSpace systemSpace = systemZone.ToSAM();
            if (systemSpace == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            systemPlantRoom.Add(systemSpace);
            result.Add(systemSpace);

            if (componentConversionSettings.IncludeResults)
            {
                SystemSpaceResult systemSpaceResult = systemZone.ToSAM_SpaceSystemResult(systemPlantRoom, componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemSpaceResult);

                systemPlantRoom.Connect(systemSpaceResult, systemSpace);
                result.Add(systemSpaceResult);
            }

            List<ZoneComponent> zoneComponents = systemZone.ZoneComponents<ZoneComponent>();
            foreach (ZoneComponent zoneComponent in zoneComponents)
            {
                ISystemSpaceComponent systemSpaceComponent = zoneComponent?.ToSAM();
                if (systemSpaceComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Add(systemSpaceComponent);

                result.Add(systemSpaceComponent);

                if (componentConversionSettings.IncludeResults)
                {
                    ISystemComponentResult systemComponentResult = zoneComponent.ToSAM_SystemComponentResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    if (systemComponentResult == null)
                    {
                        continue;
                    }

                    systemPlantRoom.Add(systemComponentResult);

                    systemPlantRoom.Connect(systemComponentResult, systemSpaceComponent);

                    result.Add(systemComponentResult);
                }
            }

            foreach (ISystemJSAMObject systemJSAMObject in result)
            {
                ISystemSpaceComponent systemSpaceComponent = systemJSAMObject as ISystemSpaceComponent;
                if (systemSpaceComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Connect(systemSpaceComponent, systemSpace);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Junction junction, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || junction == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemAirJunction systemAirJunction = junction.ToSAM();
            systemPlantRoom.Add(systemAirJunction);
            result.Add(systemAirJunction);

            if (componentConversionSettings.IncludeResults)
            {
                SystemAirJunctionResult systemAirJunctionResult = junction.ToSAM_SystemAirJunctionResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemAirJunctionResult);

                systemPlantRoom.Connect(systemAirJunctionResult, systemAirJunction);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, DXCoil dXCoil, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || dXCoil == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemDXCoil systemDXCoil = dXCoil.ToSAM();
            systemPlantRoom.Add(systemDXCoil);
            result.Add(systemDXCoil);

            if (componentConversionSettings.IncludeResults)
            {
                SystemDXCoilResult systemDXCoilResult = dXCoil.ToSAM_SystemDXCoilResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemDXCoilResult);

                systemPlantRoom.Connect(systemDXCoilResult, systemDXCoil);
                result.Add(systemDXCoilResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SteamHumidifier steamHumidifier, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || steamHumidifier == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemSteamHumidifier systemSteamHumidifier = steamHumidifier.ToSAM();
            systemPlantRoom.Add(systemSteamHumidifier);
            result.Add(systemSteamHumidifier);

            if (componentConversionSettings.IncludeResults)
            {
                SystemSteamHumidifierResult systemSteamHumidifierResult = steamHumidifier.ToSAM_SystemSteamHumidifierResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemSteamHumidifierResult);

                systemPlantRoom.Connect(systemSteamHumidifierResult, systemSteamHumidifier);
                result.Add(systemSteamHumidifierResult);
            }


            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SprayHumidifier sprayHumidifier, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || sprayHumidifier == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            Core.Systems.ISystemComponent systemComponent = null;
            ISystemComponentResult systemComponentResult = null;

            switch (sprayHumidifier.Flags)
            {
                case 0:
                    systemComponent = sprayHumidifier.ToSAM_SystemSprayHumidifier();
                    systemComponentResult = componentConversionSettings.IncludeResults ? sprayHumidifier.ToSAM_SystemSprayHumidifierResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1) : null;
                    break;

                case 1:
                    systemComponent = sprayHumidifier.ToSAM_SystemDirectEvaporativeCooler();
                    systemComponentResult = componentConversionSettings.IncludeResults ? sprayHumidifier.ToSAM_SystemDirectEvaporativeCoolerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1) : null;
                    break;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            if (systemComponent != null)
            {
                systemPlantRoom.Add(systemComponent);
                result.Add(systemComponent);
            }

            if (systemComponentResult != null)
            {
                systemPlantRoom.Add(systemComponentResult);
                result.Add(systemComponentResult);
            }

            if (systemComponent != null && systemComponentResult != null)
            {
                systemPlantRoom.Connect(systemComponentResult, systemComponent);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Exchanger exchanger, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || exchanger == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemExchanger systemExchanger = exchanger.ToSAM();
            systemPlantRoom.Add(systemExchanger);
            result.Add(systemExchanger);

            if (componentConversionSettings.IncludeResults)
            {
                SystemExchangerResult systemExchangerResult = exchanger.ToSAM_SystemExchangerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemExchangerResult);

                systemPlantRoom.Connect(systemExchangerResult, systemExchanger);

                result.Add(systemExchangerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, DesiccantWheel desiccantWheel, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || desiccantWheel == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemDesiccantWheel systemDesiccantWheel = desiccantWheel.ToSAM();
            systemPlantRoom.Add(systemDesiccantWheel);
            result.Add(systemDesiccantWheel);

            if (componentConversionSettings.IncludeResults)
            {
                SystemDesiccantWheelResult systemDesiccantWheelResult = desiccantWheel.ToSAM_SystemDesiccantWheelResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemDesiccantWheelResult);

                systemPlantRoom.Connect(systemDesiccantWheelResult, systemDesiccantWheel);
                result.Add(systemDesiccantWheelResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.Fan fan, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || fan == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemFan systemFan = fan.ToSAM();
            systemPlantRoom.Add(systemFan);
            result.Add(systemFan);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemFanResult systemFanResult = fan.ToSAM_SystemFanResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemFanResult);

                systemPlantRoom.Connect(systemFanResult, systemFan);
                result.Add(systemFanResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.CoolingCoil coolingCoil, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || coolingCoil == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemCoolingCoil systemCoolingCoil = coolingCoil.ToSAM();
            systemPlantRoom.Add(systemCoolingCoil);
            result.Add(systemCoolingCoil);

            if (componentConversionSettings.IncludeResults)
            {
                SystemCoolingCoilResult systemCoolingCoilResult = coolingCoil.ToSAM_SystemCoolingCoilResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemCoolingCoilResult);

                systemPlantRoom.Connect(systemCoolingCoilResult, systemCoolingCoil);
                result.Add(systemCoolingCoilResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.HeatingCoil heatingCoil, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || heatingCoil == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemHeatingCoil systemHeatingCoil = heatingCoil.ToSAM();
            systemPlantRoom.Add(systemHeatingCoil);
            result.Add(systemHeatingCoil);

            if (componentConversionSettings.IncludeResults)
            {
                SystemHeatingCoilResult systemHeatingCoilResult = heatingCoil.ToSAM_SystemHeatingCoilResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemHeatingCoilResult);

                systemPlantRoom.Connect(systemHeatingCoilResult, systemHeatingCoil);
                result.Add(systemHeatingCoilResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Damper damper, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || damper == null)
            {
                return null;
            }

            SystemDamper systemDamper = damper.ToSAM();
            if (systemDamper == null)
            {
                return null;
            }


            systemPlantRoom.Add(systemDamper);

            return new List<ISystemJSAMObject>() { systemDamper };
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, ComponentGroup componentGroup, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || componentGroup == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            BoundingBox2D boundingBox2D = null;

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);
            if (systemComponents != null)
            {
                foreach (global::TPD.SystemComponent systemComponent_Temp in systemComponents)
                {
                    if (systemComponent_Temp is Junction)
                    {
                        continue;
                    }

                    List<ISystemJSAMObject> systemJSAMObjects = Add(systemPlantRoom, systemComponent_Temp, tPDDoc, componentConversionSettings);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            List<Controller> controllers = componentGroup.Controllers();
            if (controllers != null)
            {
                foreach (Controller controller in controllers)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(controller, tPDDoc, componentConversionSettings);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            Transform2D transform2D = null;

            Point2D location = ((TasPosition)(componentGroup as dynamic).GetPosition())?.ToSAM();
            if (location != null)
            {
                transform2D = Transform2D.GetTranslation(location.ToVector());
            }

            if (transform2D != null)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    IDisplaySystemObject displaySystemObject = result[i] as IDisplaySystemObject;

                    if (displaySystemObject == null)
                    {
                        continue;
                    }

                    displaySystemObject.Transform(transform2D);
                    systemPlantRoom.Add(displaySystemObject as dynamic);

                    BoundingBox2D boundingBox2D_Temp = displaySystemObject.BoundingBox2D;
                    if (boundingBox2D_Temp != null)
                    {
                        if (boundingBox2D == null)
                        {
                            boundingBox2D = boundingBox2D_Temp;
                        }
                        else
                        {
                            boundingBox2D.Include(boundingBox2D_Temp);
                        }
                    }
                }
            }

            if (boundingBox2D != null)
            {
                boundingBox2D = boundingBox2D.GetBoundingBox(0.1);
            }

            List<Duct> ducts = Query.Ducts(componentGroup);
            if (ducts != null)
            {
                foreach (Duct duct in ducts)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(duct);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            AirSystemGroup airSystemGroup = componentGroup.ToSAM(boundingBox2D);
            systemPlantRoom.Add(airSystemGroup);

            foreach (ISystemJSAMObject systemJSAMObject in result)
            {
                Core.Systems.ISystemComponent systemComponent = systemJSAMObject as Core.Systems.ISystemComponent;
                if (systemComponent == null)
                {
                    continue;
                }

                systemPlantRoom.Connect(airSystemGroup, systemComponent);
            }

            result.Add(airSystemGroup);

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, global::TPD.System system, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || system == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            AirSystem airSystem = system.ToSAM();
            if (airSystem == null)
            {
                return null;
            }

            systemPlantRoom.Add(airSystem);

            List<global::TPD.SystemComponent> systemComponents = system.SystemComponents<global::TPD.SystemComponent>();
            if (systemComponents != null)
            {
                List<ComponentGroup> componentGroups = new List<ComponentGroup>();

                foreach (global::TPD.SystemComponent systemComponent in systemComponents)
                {

                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(systemComponent, tPDDoc, componentConversionSettings);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }

                    if (systemComponent is ComponentGroup)
                    {
                        componentGroups.Add((ComponentGroup)systemComponent);
                    }
                }

                foreach (ComponentGroup componentGroup in componentGroups)
                {
                    systemPlantRoom.Connect(componentGroup);
                }
            }

            List<Controller> controllers = system.Controllers();
            if (controllers != null)
            {
                foreach (Controller controller in controllers)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(controller, tPDDoc, componentConversionSettings);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            foreach (ISystemJSAMObject systemJSAMObject in result)
            {
                if (systemJSAMObject is ISystemGroup)
                {
                    systemPlantRoom.Connect(airSystem, (ISystemGroup)systemJSAMObject);
                    continue;
                }

                Core.Systems.ISystemComponent systemComponent = systemJSAMObject as Core.Systems.ISystemComponent;
                if (systemComponent == null)
                {
                    continue;
                }

                if (systemComponent is ISystemSpaceComponent)
                {
                    continue;
                }

                systemPlantRoom.Connect(airSystem, systemComponent);
            }

            result.Add(airSystem);

            List<ISystemConnection> systemConnections = Connect(systemPlantRoom, system);
            if (systemConnections != null)
            {
                result.AddRange(systemConnections);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || tPDDoc == null)
            {
                return null;
            }

            List<PlantRoom> plantRooms = tPDDoc?.EnergyCentre?.PlantRooms();
            if (plantRooms == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();
            foreach (PlantRoom plantRoom in plantRooms)
            {
                List<ISystemJSAMObject> systemJSAMObjects = null;

                systemJSAMObjects = Add(systemPlantRoom, plantRoom, tPDDoc, componentConversionSettings);
                if (systemJSAMObjects != null)
                {
                    result.AddRange(systemJSAMObjects);
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Duct duct)
        {

            if (systemPlantRoom == null || duct == null)
            {
                return null;
            }

            AirSystem airSystem = systemPlantRoom.System<AirSystem>(duct.GetSystem().GUID);
            if (airSystem == null)
            {
                return null;
            }

            SystemType systemType = new SystemType(airSystem);
            if (systemType == null)
            {
                return null;
            }

            dynamic @dynamic_1 = duct.GetUpstreamComponent();

            string guid_1 = dynamic_1.GUID;

            dynamic @dynamic_2 = duct.GetDownstreamComponent();

            string guid_2 = dynamic_2.GUID;

            Core.Systems.ISystemComponent systemComponent_1 = Query.SystemComponent<Core.Systems.ISystemComponent>(systemPlantRoom, guid_1);
            if (systemComponent_1 == null || !(systemComponent_1 is IDisplaySystemObject<SystemGeometryInstance>))
            {
                return null;
            }

            int index_1 = systemPlantRoom.FindIndex(systemComponent_1, systemType, ConnectorStatus.Unconnected, Direction.Out);
            if (index_1 == -1)
            {
                return null;
            }

            Core.Systems.ISystemComponent systemComponent_2 = Query.SystemComponent<Core.Systems.ISystemComponent>(systemPlantRoom, guid_2);
            if (systemComponent_2 == null || !(systemComponent_2 is IDisplaySystemObject<SystemGeometryInstance>))
            {
                return null;
            }

            int index_2 = systemPlantRoom.FindIndex(systemComponent_2, systemType, ConnectorStatus.Unconnected, Direction.In);
            if (index_2 == -1)
            {
                return null;
            }

            List<Point2D> point2Ds = Query.Point2Ds(duct);
            if (point2Ds == null)
            {
                point2Ds = new List<Point2D>();
            }

            Point2D point2D = null;

            point2D = (systemComponent_1 as dynamic).SystemGeometry.GetPoint2D(index_1);
            if (point2D != null)
            {
                point2Ds.Insert(0, point2D);
            }

            point2D = (systemComponent_2 as dynamic).SystemGeometry.GetPoint2D(index_2);
            if (point2D != null)
            {
                point2Ds.Add(point2D);
            }

            if (point2Ds == null || point2Ds.Count == 0)
            {
                return null;
            }

            DisplaySystemConnection result = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemComponent_1, index_1, systemComponent_2, index_2), point2Ds?.ToArray());
            if (result == null)
            {
                return null;
            }

            systemPlantRoom.Connect(result, airSystem);

            return new List<ISystemJSAMObject>() { result };
        }

        private static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || plantRoom == null)
            {
                return null;
            }

            List<List<PlantComponent>> plantComponentsList = Query.ConnectedPlantComponents(plantRoom);
            if(plantComponentsList == null || plantComponentsList.Count == 0)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            LiquidSystem liquidSystem = new LiquidSystem("Liquid System");
            systemPlantRoom.Add(liquidSystem);

            foreach (List<PlantComponent> plantComponents_Temp in plantComponentsList)
            {
                if(plantComponents_Temp == null || plantComponents_Temp.Count == 0)
                {
                    continue;
                }

                foreach (PlantComponent plantComponent in plantComponents_Temp)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = Add(systemPlantRoom, plantComponent, tPDDoc, componentConversionSettings);
                    if(systemJSAMObjects == null)
                    {
                        continue;
                    }

                    foreach(ISystemJSAMObject systemJSAMObject in systemJSAMObjects)
                    {
                        result.Add(systemJSAMObject);

                        Core.Systems.ISystemComponent systemComponent = systemJSAMObject as Core.Systems.ISystemComponent;
                        if (systemComponent == null)
                        {
                            continue;
                        }

                        systemPlantRoom.Connect(liquidSystem, systemComponent);
                    }

                }
            }

            List<PlantController> plantControllers = plantRoom.PlantControllers();
            if (plantControllers != null)
            {
                foreach (PlantController plantController in plantControllers)
                {
                    List<ISystemJSAMObject> systemJSAMObjects = systemPlantRoom.Add(plantController, tPDDoc, componentConversionSettings);
                    if (systemJSAMObjects != null)
                    {
                        result.AddRange(systemJSAMObjects);
                    }
                }
            }

            List<ISystemConnection> systemConnections = Connect(systemPlantRoom, plantRoom, liquidSystem);
            if (systemConnections != null)
            {
                result.AddRange(systemConnections);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, IPlantComponent plantComponent, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || plantComponent == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            if (plantComponent is Pump)
            {
                return Add(systemPlantRoom, (Pump)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is PlantGroup)
            {
                return Add(systemPlantRoom, (PlantGroup)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is BoilerPlant)
            {
                return Add(systemPlantRoom, (BoilerPlant)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is Chiller)
            {
                return Add(systemPlantRoom, (Chiller)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is AbsorptionChiller)
            {
                return Add(systemPlantRoom, (AbsorptionChiller)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is IceStorageChiller)
            {
                return Add(systemPlantRoom, (IceStorageChiller)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is WaterSourceChiller)
            {
                return Add(systemPlantRoom, (WaterSourceChiller)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is MultiChiller)
            {
                return Add(systemPlantRoom, (MultiChiller)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is MultiBoiler)
            {
                return Add(systemPlantRoom, (MultiBoiler)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is PlantJunction)
            {
                return Add(systemPlantRoom, (PlantJunction)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is AirSourceHeatPump)
            {
                return Add(systemPlantRoom, (AirSourceHeatPump)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is WaterToWaterHeatPump)
            {
                return Add(systemPlantRoom, (WaterToWaterHeatPump)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is HeatPump)
            {
                return Add(systemPlantRoom, (HeatPump)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is Tank)
            {
                return Add(systemPlantRoom, (Tank)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is PipeLossComponent)
            {
                return Add(systemPlantRoom, (PipeLossComponent)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is HeatExchanger)
            {
                return Add(systemPlantRoom, (HeatExchanger)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is CoolingTower)
            {
                return Add(systemPlantRoom, (CoolingTower)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is DryCooler)
            {
                return Add(systemPlantRoom, (DryCooler)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is GroundSource)
            {
                return Add(systemPlantRoom, (GroundSource)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is SlinkyCoil)
            {
                return Add(systemPlantRoom, (SlinkyCoil)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is CHP)
            {
                return Add(systemPlantRoom, (CHP)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is SurfaceWaterExchanger)
            {
                return Add(systemPlantRoom, (SurfaceWaterExchanger)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is HorizontalGHE)
            {
                return Add(systemPlantRoom, (HorizontalGHE)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is SolarPanel)
            {
                return Add(systemPlantRoom, (SolarPanel)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is PVPanel)
            {
                return Add(systemPlantRoom, (PVPanel)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is Valve)
            {
                return Add(systemPlantRoom, (Valve)plantComponent, tPDDoc, componentConversionSettings);
            }

            if (plantComponent is WindTurbine)
            {
                return Add(systemPlantRoom, (WindTurbine)plantComponent, tPDDoc, componentConversionSettings);
            }

            return null;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, WindTurbine windTurbine, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || windTurbine == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemWindTurbine systemWindTurbine = windTurbine.ToSAM();
            systemPlantRoom.Add(systemWindTurbine);
            result.Add(systemWindTurbine);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemWindTurbineResult systemWindTurbineResult = windTurbine.ToSAM_SystemWindTurbineResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemWindTurbineResult);

                systemPlantRoom.Connect(systemWindTurbineResult, systemWindTurbine);
                result.Add(systemWindTurbineResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Valve valve, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || valve == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemValve systemValve = valve.ToSAM();
            systemPlantRoom.Add(systemValve);
            result.Add(systemValve);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemValveResult systemValveResult = valve.ToSAM_SystemValveResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemValveResult);

                systemPlantRoom.Connect(systemValveResult, systemValve);
                result.Add(systemValveResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PVPanel pVPanel, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || pVPanel == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemPhotovoltaicPanel systemPhotovoltaicPanel = pVPanel.ToSAM();
            systemPlantRoom.Add(systemPhotovoltaicPanel);
            result.Add(systemPhotovoltaicPanel);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemPhotovoltaicPanelResult systemPhotovoltaicPanelResult = pVPanel.ToSAM_SystemPhotovoltaicPanelResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemPhotovoltaicPanelResult);

                systemPlantRoom.Connect(systemPhotovoltaicPanelResult, systemPhotovoltaicPanel);
                result.Add(systemPhotovoltaicPanelResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SolarPanel solarPanel, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || solarPanel == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemSolarPanel systemSolarPanel = solarPanel.ToSAM();
            systemPlantRoom.Add(systemSolarPanel);
            result.Add(systemSolarPanel);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemSolarPanelResult systemSolarPanelResult = solarPanel.ToSAM_SystemSolarPanelResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemSolarPanelResult);

                systemPlantRoom.Connect(systemSolarPanelResult, systemSolarPanel);
                result.Add(systemSolarPanelResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, HorizontalGHE horizontalGHE, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || horizontalGHE == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemHorizontalExchanger systemHorizontalExchanger = horizontalGHE.ToSAM();
            systemPlantRoom.Add(systemHorizontalExchanger);
            result.Add(systemHorizontalExchanger);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemHorizontalExchangerResult systemHorizontalExchangerResult = horizontalGHE.ToSAM_SystemHorizontalExchangerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemHorizontalExchangerResult);

                systemPlantRoom.Connect(systemHorizontalExchangerResult, systemHorizontalExchanger);
                result.Add(systemHorizontalExchangerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SurfaceWaterExchanger surfaceWaterExchanger, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || surfaceWaterExchanger == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemSurfaceWaterExchanger systemSurfaceWaterExchanger = surfaceWaterExchanger.ToSAM();
            systemPlantRoom.Add(systemSurfaceWaterExchanger);
            result.Add(systemSurfaceWaterExchanger);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemSurfaceWaterExchangerResult systemSurfaceWaterExchangerResult = surfaceWaterExchanger.ToSAM_SystemSurfaceWaterExchangerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemSurfaceWaterExchangerResult);

                systemPlantRoom.Connect(systemSurfaceWaterExchangerResult, systemSurfaceWaterExchanger);
                result.Add(systemSurfaceWaterExchangerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, CHP cHP, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || cHP == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemCHP systemCHP = cHP.ToSAM();
            systemPlantRoom.Add(systemCHP);
            result.Add(systemCHP);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemCHPResult systemCHPResult = cHP.ToSAM_SystemCHPResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemCHPResult);

                systemPlantRoom.Connect(systemCHPResult, systemCHP);
                result.Add(systemCHPResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, SlinkyCoil slinkyCoil, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || slinkyCoil == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemSlinkyCoil systemSlinkyCoil = slinkyCoil.ToSAM();
            systemPlantRoom.Add(systemSlinkyCoil);
            result.Add(systemSlinkyCoil);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemSlinkyCoilResult systemSlinkyCoilResult = slinkyCoil.ToSAM_SystemSlinkyCoilResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemSlinkyCoilResult);

                systemPlantRoom.Connect(systemSlinkyCoilResult, systemSlinkyCoil);
                result.Add(systemSlinkyCoilResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, GroundSource groundSource, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || groundSource == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemVerticalBorehole systemVerticalBorehole = groundSource.ToSAM();
            systemPlantRoom.Add(systemVerticalBorehole);
            result.Add(systemVerticalBorehole);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemVerticalBoreholeResult systemVerticalBoreholeResult = groundSource.ToSAM_SystemVerticalBoreholeResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemVerticalBoreholeResult);

                systemPlantRoom.Connect(systemVerticalBoreholeResult, systemVerticalBorehole);
                result.Add(systemVerticalBoreholeResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, DryCooler dryCooler, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || dryCooler == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemDryCooler systemDryCooler = dryCooler.ToSAM();
            systemPlantRoom.Add(systemDryCooler);
            result.Add(systemDryCooler);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemDryCoolerResult systemDryCoolerResult = dryCooler.ToSAM_SystemDryCoolerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemDryCoolerResult);

                systemPlantRoom.Connect(systemDryCoolerResult, systemDryCooler);
                result.Add(systemDryCoolerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, CoolingTower coolingTower, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || coolingTower == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemCoolingTower systemCoolingTower = coolingTower.ToSAM();
            systemPlantRoom.Add(systemCoolingTower);
            result.Add(systemCoolingTower);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemCoolingTowerResult systemCoolingTowerResult = coolingTower.ToSAM_SystemCoolingTowerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemCoolingTowerResult);

                systemPlantRoom.Connect(systemCoolingTowerResult, systemCoolingTower);
                result.Add(systemCoolingTowerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, HeatExchanger heatExchanger, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || heatExchanger == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemLiquidExchanger systemLiquidExchanger = heatExchanger.ToSAM();
            systemPlantRoom.Add(systemLiquidExchanger);
            result.Add(systemLiquidExchanger);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemLiquidExchangerResult SystemLiquidExchangerResult = heatExchanger.ToSAM_SystemLiquidExchangerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(SystemLiquidExchangerResult);

                systemPlantRoom.Connect(SystemLiquidExchangerResult, systemLiquidExchanger);
                result.Add(SystemLiquidExchangerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Pump pump, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || pump == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemPump systemPump = pump.ToSAM();
            systemPlantRoom.Add(systemPump);
            result.Add(systemPump);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemPumpResult systemPumpResult = pump.ToSAM_SystemPumpResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemPumpResult);

                systemPlantRoom.Connect(systemPumpResult, systemPump);
                result.Add(systemPumpResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PipeLossComponent pipeLossComponent, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || pipeLossComponent == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemPipeLossComponent systemPipeLossComponent = pipeLossComponent.ToSAM();
            systemPlantRoom.Add(systemPipeLossComponent);
            result.Add(systemPipeLossComponent);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemPipeLossComponentResult systemPipeLossComponentResult = pipeLossComponent.ToSAM_SystemPipeLossComponentResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemPipeLossComponentResult);

                systemPlantRoom.Connect(systemPipeLossComponentResult, systemPipeLossComponent);
                result.Add(systemPipeLossComponentResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PlantGroup plantGroup, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || plantGroup == null)
            {
                return null;
            }

            Core.Systems.ISystem system = null;
            ISystemCollection systemCollection = null;

            dynamic @dynamic = plantGroup;

            string name = @dynamic.Name;

            if (plantGroup is ElectricalGroup)
            {
                system = new ElectricalSystem(name);
                systemCollection = ((ElectricalGroup)plantGroup).ToSAM();
            }
            else if(plantGroup is RefrigerantGroup)
            {
                system = new RefrigerantSystem(name);
                systemCollection = ((RefrigerantGroup)plantGroup).ToSAM();
            }
            else if (plantGroup is HeatingGroup)
            {
                system = new Systems.HeatingSystem(name);
                systemCollection = ((HeatingGroup)plantGroup).ToSAM();
            }
            else if (plantGroup is CoolingGroup)
            {
                system = new Systems.CoolingSystem(name);
                systemCollection = ((CoolingGroup)plantGroup).ToSAM();
            }
            else if (plantGroup is DHWGroup)
            {
                system = new DomesticHotWaterSystem(name);
                systemCollection = ((DHWGroup)plantGroup).ToSAM();
            }
            else if (plantGroup is FuelGroup)
            {
                system = new FuelSystem(name);
                systemCollection = ((FuelGroup)plantGroup).ToSAM();
            }

            if (system == null && systemCollection == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            if(system != null)
            {
                if (systemPlantRoom.Add(system))
                {
                    result.Add(system);
                }
            }

            if(systemCollection != null)
            {
                if (systemPlantRoom.Add(systemCollection))
                {
                    result.Add(systemCollection);
                }
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(plantGroup);
            if (systemComponents != null)
            {
                foreach (global::TPD.SystemComponent systemComponent_Temp in systemComponents)
                {
                    string reference = systemComponent_Temp.Reference();
                    if(string.IsNullOrWhiteSpace(reference))
                    {
                        continue;
                    }

                    Core.Systems.ISystemComponent systemComponent = systemPlantRoom.GetSystemComponents<Core.Systems.ISystemComponent>().Find(x => x?.Reference() == reference);
                    if (systemComponent == null)
                    {
                        continue;
                    }

                    systemPlantRoom?.Connect(system, systemComponent);
                    systemPlantRoom?.Connect(systemCollection, systemComponent);
                    //continue;

                    //List<ISystemJSAMObject> systemJSAMObjects = Add(systemPlantRoom, systemComponent_Temp, tPDDoc, componentConversionSettings);
                    //if (systemJSAMObjects != null)
                    //{
                    //    result.AddRange(systemJSAMObjects);
                    //}
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, BoilerPlant boilerPlant, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || boilerPlant == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemBoiler systemBoiler = boilerPlant.ToSAM();
            systemPlantRoom.Add(systemBoiler);
            result.Add(systemBoiler);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemBoilerResult systemBoilerResult = boilerPlant.ToSAM_SystemBoilerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemBoilerResult);

                systemPlantRoom.Connect(systemBoilerResult, systemBoiler);
                result.Add(systemBoilerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Chiller chiller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || chiller == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemChiller systemChiller = chiller.ToSAM();
            systemPlantRoom.Add(systemChiller);
            result.Add(systemChiller);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                if (systemChiller is SystemAirSourceChiller)
                {
                    SystemAirSourceChillerResult systemAirSourceChillerResult = chiller.ToSAM_SystemAirSourceChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemAirSourceChillerResult);

                    systemPlantRoom.Connect(systemAirSourceChillerResult, systemChiller);
                    result.Add(systemAirSourceChillerResult);
                }
                else if (systemChiller is SystemAirSourceDirectAbsorptionChiller)
                {
                    SystemAirSourceDirectAbsorptionChillerResult systemAirSourceDirectAbsorptionChillerResult = chiller.ToSAM_SystemAirSourceDirectAbsorptionChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemAirSourceDirectAbsorptionChillerResult);

                    systemPlantRoom.Connect(systemAirSourceDirectAbsorptionChillerResult, systemChiller);
                    result.Add(systemAirSourceDirectAbsorptionChillerResult);
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, WaterSourceChiller waterSourceChiller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || waterSourceChiller == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemChiller systemChiller = waterSourceChiller.ToSAM();
            systemPlantRoom.Add(systemChiller);
            result.Add(systemChiller);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                if (systemChiller is SystemWaterSourceChiller)
                {
                    SystemWaterSourceChillerResult systemWaterSourceChillerResult = waterSourceChiller.ToSAM_SystemWaterSourceChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemWaterSourceChillerResult);

                    systemPlantRoom.Connect(systemWaterSourceChillerResult, systemChiller);
                    result.Add(systemWaterSourceChillerResult);
                }
                else if (systemChiller is SystemWaterSourceDirectAbsorptionChiller)
                {
                    SystemWaterSourceDirectAbsorptionChillerResult systemWaterSourceDirectAbsorptionChillerResult = waterSourceChiller.ToSAM_SystemWaterSourceDirectAbsorptionChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemWaterSourceDirectAbsorptionChillerResult);

                    systemPlantRoom.Connect(systemWaterSourceDirectAbsorptionChillerResult, systemChiller);
                    result.Add(systemWaterSourceDirectAbsorptionChillerResult);
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, AbsorptionChiller absorptionChiller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || absorptionChiller == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemChiller systemChiller = absorptionChiller.ToSAM();
            systemPlantRoom.Add(systemChiller);
            result.Add(systemChiller);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                if (systemChiller is SystemAbsorptionChiller)
                {
                    SystemAbsorptionChillerResult systemAbsorptionChillerResult = absorptionChiller.ToSAM_SystemAbsorptionChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemAbsorptionChillerResult);

                    systemPlantRoom.Connect(systemAbsorptionChillerResult, systemChiller);
                    result.Add(systemAbsorptionChillerResult);
                }
                else if (systemChiller is SystemWaterSourceAbsorptionChiller)
                {
                    SystemWaterSourceAbsorptionChillerResult systemWaterSourceAbsorptionChillerResult = absorptionChiller.ToSAM_SystemWaterSourceAbsorptionChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemWaterSourceAbsorptionChillerResult);

                    systemPlantRoom.Connect(systemWaterSourceAbsorptionChillerResult, systemChiller);
                    result.Add(systemWaterSourceAbsorptionChillerResult);
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, IceStorageChiller iceStorageChiller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || iceStorageChiller == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemChiller systemChiller = iceStorageChiller.ToSAM();
            systemPlantRoom.Add(systemChiller);
            result.Add(systemChiller);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                if (systemChiller is SystemIceStorageChiller)
                {
                    SystemIceStorageChillerResult systemIceStorageChillerResult = iceStorageChiller.ToSAM_SystemIceStorageChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemIceStorageChillerResult);

                    systemPlantRoom.Connect(systemIceStorageChillerResult, systemChiller);
                    result.Add(systemIceStorageChillerResult);
                }
                else if (systemChiller is SystemWaterSourceIceStorageChiller)
                {
                    SystemWaterSourceIceStorageChillerResult systemWaterSourceIceStorageChillerResult = iceStorageChiller.ToSAM_SystemWaterSourceIceStorageChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                    systemPlantRoom.Add(systemWaterSourceIceStorageChillerResult);

                    systemPlantRoom.Connect(systemWaterSourceIceStorageChillerResult, systemChiller);
                    result.Add(systemWaterSourceIceStorageChillerResult);
                }
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, MultiChiller multiChiller, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || multiChiller == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemMultiChiller systemMultiChiller = multiChiller.ToSAM();
            systemPlantRoom.Add(systemMultiChiller);
            result.Add(systemMultiChiller);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemMultiChillerResult systemMultiChillerResult = multiChiller.ToSAM_SystemMultiChillerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemMultiChillerResult);

                systemPlantRoom.Connect(systemMultiChillerResult, systemMultiChiller);
                result.Add(systemMultiChillerResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, MultiBoiler multiBoiler, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || multiBoiler == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemBoiler systemBoiler = multiBoiler.ToSAM();
            systemPlantRoom.Add(systemBoiler);
            result.Add(systemBoiler);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemBoilerResult systemBoilerResult = multiBoiler.ToSAM_SystemBoilerResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemBoilerResult);

                systemPlantRoom.Connect(systemBoilerResult, systemBoiler);
                result.Add(systemBoilerResult);
            }

            return result;
        }
        
        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, PlantJunction plantJunction, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || plantJunction == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemLiquidJunction systemLiquidJunction = plantJunction.ToSAM();
            systemPlantRoom.Add(systemLiquidJunction);
            result.Add(systemLiquidJunction);

            if (componentConversionSettings.IncludeResults)
            {
                SystemLiquidJunctionResult systemLiquidJunctionResult = plantJunction.ToSAM_SystemLiquidJunctionResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemLiquidJunctionResult);

                systemPlantRoom.Connect(systemLiquidJunctionResult, systemLiquidJunction);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, AirSourceHeatPump airSourceHeatPump, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || airSourceHeatPump == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemAirSourceHeatPump systemAirSourceHeatPump = airSourceHeatPump.ToSAM();
            systemPlantRoom.Add(systemAirSourceHeatPump);
            result.Add(systemAirSourceHeatPump);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemAirSourceHeatPumpResult systemAirSourceHeatPumpResult = airSourceHeatPump.ToSAM_SystemAirSourceHeatPumpResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemAirSourceHeatPumpResult);

                systemPlantRoom.Connect(systemAirSourceHeatPumpResult, systemAirSourceHeatPump);
                result.Add(systemAirSourceHeatPumpResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, WaterToWaterHeatPump waterToWaterHeatPump, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || waterToWaterHeatPump == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemWaterToWaterHeatPump systemWaterToWaterHeatPump = waterToWaterHeatPump.ToSAM();
            systemPlantRoom.Add(systemWaterToWaterHeatPump);
            result.Add(systemWaterToWaterHeatPump);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemWaterToWaterHeatPumpResult systemWaterToWaterHeatPumpResult = waterToWaterHeatPump.ToSAM_SystemWaterToWaterHeatPumpResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemWaterToWaterHeatPumpResult);

                systemPlantRoom.Connect(systemWaterToWaterHeatPumpResult, systemWaterToWaterHeatPump);
                result.Add(systemWaterToWaterHeatPumpResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, HeatPump heatPump, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || heatPump == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemWaterSourceHeatPump systemWaterToWaterHeatPump = heatPump.ToSAM();
            systemPlantRoom.Add(systemWaterToWaterHeatPump);
            result.Add(systemWaterToWaterHeatPump);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemWaterSourceHeatPumpResult systemWaterSourceHeatPumpResult = heatPump.ToSAM_SystemWaterSourceHeatPumpResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemWaterSourceHeatPumpResult);

                systemPlantRoom.Connect(systemWaterSourceHeatPumpResult, systemWaterToWaterHeatPump);
                result.Add(systemWaterSourceHeatPumpResult);
            }

            return result;
        }

        public static List<ISystemJSAMObject> Add(this SystemPlantRoom systemPlantRoom, Tank tank, TPDDoc tPDDoc, ComponentConversionSettings componentConversionSettings = null)
        {
            if (systemPlantRoom == null || tank == null || tPDDoc == null)
            {
                return null;
            }

            if (componentConversionSettings == null)
            {
                componentConversionSettings = new ComponentConversionSettings();
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();

            SystemTank systemTank = tank.ToSAM();
            systemPlantRoom.Add(systemTank);
            result.Add(systemTank);

            if (componentConversionSettings.IncludeResults)
            {
                int start = tPDDoc.StartHour();
                int end = tPDDoc.EndHour();

                SystemTankResult systemTankResult = tank.ToSAM_SystemTankResult(componentConversionSettings.StartHour + 1, componentConversionSettings.EndHour + 1);
                systemPlantRoom.Add(systemTankResult);

                systemPlantRoom.Connect(systemTankResult, systemTank);
                result.Add(systemTankResult);
            }

            return result;
        }
    }
}