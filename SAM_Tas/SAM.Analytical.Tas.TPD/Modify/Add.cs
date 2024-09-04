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
            if(systemPlantRoom == null || controller == null || tPDDoc == null)
            {
                return null;
            }

            ISystemController systemController = controller.ToSAM();
            if(systemController == null)
            {
                return null;
            }

            List<ISystemJSAMObject> result = new List<ISystemJSAMObject>();
            result.Add(systemController);

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
                    systemComponentResult = componentConversionSettings.IncludeResults ? optimiser.ToSAM_SystemMixingBoxResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour) : null;
                    break;

                case 0:
                    systemComponent = optimiser.ToSAM_SystemEconomiser();
                    systemComponentResult = componentConversionSettings.IncludeResults ? optimiser.ToSAM_SystemEconomiserResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour) : null;
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
                SystemSpaceResult systemSpaceResult = systemZone.ToSAM_SpaceSystemResult(systemPlantRoom, componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                    ISystemComponentResult systemComponentResult = zoneComponent.ToSAM_SystemComponentResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemAirJunctionResult systemAirJunctionResult = junction.ToSAM_SystemAirJunctionResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemDXCoilResult systemDXCoilResult = dXCoil.ToSAM_SystemDXCoilResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemSteamHumidifierResult systemSteamHumidifierResult = steamHumidifier.ToSAM_SystemSteamHumidifierResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                    systemComponentResult = componentConversionSettings.IncludeResults ? sprayHumidifier.ToSAM_SystemSprayHumidifierResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour) : null;
                    break;

                case 1:
                    systemComponent = sprayHumidifier.ToSAM_SystemDirectEvaporativeCooler();
                    systemComponentResult = componentConversionSettings.IncludeResults ? sprayHumidifier.ToSAM_SystemDirectEvaporativeCoolerResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour) : null;
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
                SystemExchangerResult systemExchangerResult = exchanger.ToSAM_SystemExchangerResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemDesiccantWheelResult systemDesiccantWheelResult = desiccantWheel.ToSAM_SystemDesiccantWheelResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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

                SystemFanResult systemFanResult = fan.ToSAM_SystemFanResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemCoolingCoilResult systemCoolingCoilResult = coolingCoil.ToSAM_SystemCoolingCoilResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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
                SystemHeatingCoilResult systemHeatingCoilResult = heatingCoil.ToSAM_SystemHeatingCoilResult(componentConversionSettings.StartHour, componentConversionSettings.EndHour);
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

            if(transform2D != null)
            {
                for (int i = 0; i < result.Count; i++)
                {
                    IDisplaySystemObject displaySystemObject = result[i] as IDisplaySystemObject;

                    if(displaySystemObject == null)
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
            if(controllers != null)
            {
                foreach(Controller controller in controllers)
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
                if(systemJSAMObject is ISystemGroup)
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

    }
}