using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<ISystemConnection> Connect(this SystemPlantRoom systemPlantRoom, global::TPD.System system)
        {
            if (systemPlantRoom == null || system == null)
            {
                return null;
            }

            string reference_System = (system as dynamic).GUID;

            AirSystem airSystem = systemPlantRoom.Find<AirSystem>(x => x?.Reference() == reference_System);
            if(airSystem == null)
            {
                return null;
            }

            List<ISystemConnection> result = new List<ISystemConnection>();

            List<ComponentGroup> componentGroups = null;

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(system, true);
            if(systemComponents != null && systemComponents.Count != 0)
            {
                componentGroups = new List<ComponentGroup>();

                foreach (global::TPD.SystemComponent systemComponent_TPD in systemComponents)
                {
                    if (systemComponent_TPD == null)
                    {
                        continue;
                    }

                    if(systemComponent_TPD is ComponentGroup)
                    {
                        componentGroups.Add((ComponentGroup)systemComponent_TPD);
                    }

                    string reference_1 = (systemComponent_TPD as dynamic).GUID;

                    Core.Systems.ISystemComponent systemComponent_SAM_1 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_1);

                    Direction direction = Direction.In;

                    List<Duct> ducts = Query.Ducts(systemComponent_TPD, direction);
                    if (ducts == null || ducts.Count == 0)
                    {
                        continue;
                    }

                    foreach (Duct duct in ducts)
                    {
                        string reference_2 = (duct.GetUpstreamComponent() as dynamic)?.GUID;

                        Core.Systems.ISystemComponent systemComponent_SAM_2 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                        if (systemComponent_SAM_2 == null)
                        {
                            continue;
                        }

                        List<Point2D> point2Ds = Query.Point2Ds(duct);

                        ISystemConnection systemConnection = Connect(systemPlantRoom, systemComponent_SAM_1, systemComponent_SAM_2, airSystem, direction, point2Ds);
                        if (systemConnection != null)
                        {
                            systemConnection.SetReference(Query.Reference(duct));
                            systemPlantRoom.Add(systemConnection);

                            result.Add(systemConnection);
                        }
                    }
                }
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            if (componentGroups != null && componentGroups.Count != 0)
            {
                foreach(ComponentGroup componentGroup in componentGroups)
                {
                    List<Controller> controllers_Group = componentGroup.Controllers();
                    if(controllers_Group == null || controllers_Group.Count == 0)
                    {
                        continue;
                    }

                    List<Controller> controllers_Temp = null;

                    controllers_Temp = controllers_Group.FindAll(x => x.ControlType == tpdControlType.tpdControlGroup);
                    int count = controllers_Temp.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Controller controller_1 = controllers_Temp[i];
                        int chainArcCount = controller_1.GetChainArcCount();
                        if(chainArcCount == 0)
                        {
                            continue;
                        }

                        controller_1 = (controller_1.GetChainArc(1) as dynamic).GetController();

                        IReference reference_1 = controller_1.Reference();
                        ISystemController systemController_1 = systemPlantRoom.SystemController<ISystemController>(reference_1);
                        if (systemController_1 == null)
                        {
                            continue;
                        }

                        ControlArc controlArc = componentGroup.GetGroupControlArc(count - i, 1);
                        if (controlArc == null)
                        {
                            continue;
                        }

                        Controller controller_2 = controlArc.GetChainController();
                        IReference reference_2 = controller_2?.Reference();
                        ISystemController systemController_2 = systemPlantRoom.SystemController<ISystemController>(reference_2);
                        if (systemController_2 == null)
                        {
                            continue;
                        }

                        List<ControlArc> controlArcs = Query.ControlArcs(controller_2, componentGroup);
                        if (controlArcs == null || controlArcs.Count == 0)
                        {
                            continue;
                        }

                        controlArcs.Add(controlArc);

                        Dictionary<int, Point2D> dictionary = null;

                        List<Point2D> point2Ds = new List<Point2D>();

                        dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController_1 as dynamic, systemType_Control, direction: Direction.Out);
                        if(dictionary == null || dictionary.Count == 0)
                        {
                            continue;
                        }

                        int index_1 = dictionary.Keys.First();

                        point2Ds.Add(dictionary[index_1]);

                        dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController_2 as dynamic, systemType_Control, direction: Direction.In);
                        if (dictionary == null || dictionary.Count == 0)
                        {
                            continue;
                        }

                        int index_2 = dictionary.Keys.First();

                        point2Ds.Insert(0, dictionary[index_2]);

                        DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController_1, index_1, systemController_2, index_2), point2Ds?.ToArray());

                        systemPlantRoom.Connect(displaySystemConnection, airSystem);
                    }

                    controllers_Temp = controllers_Group.FindAll(x => x.ControlType != tpdControlType.tpdControlGroup);
                    foreach(Controller controller in controllers_Temp)
                    {
                        List<ISystemSensor> systemSensors = systemPlantRoom.Connect_SystemSensors(controller);
                        if (systemSensors != null)
                        {
                            foreach (ISystemSensor systemSensor in systemSensors)
                            {
                                systemPlantRoom.Connect(systemSensor, airSystem);
                            }

                        }

                        List<ControlArc> controlArcs = controlArcs = controller.ControlArcs();
                        if (controlArcs != null && controlArcs.Count != 0)
                        {
                            foreach (ControlArc controlArc in controlArcs)
                            {
                                ISystemConnection systemConnection = Connect_ControlArc(systemPlantRoom, controlArc);
                                if (systemConnection != null)
                                {
                                    systemPlantRoom.Connect(systemConnection, airSystem);
                                }
                            }
                        }
                    }
                }
            }


            List<Controller> controllers = Query.Controllers(system);
            if(controllers != null && controllers.Count != 0)
            {
                foreach(Controller controller in controllers)
                {
                    if (controller == null)
                    {
                        continue;
                    }

                    IReference reference_1 = controller.Reference();

                    ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(reference_1);
                    if(systemController == null)
                    {
                        continue;
                    }

                    List<ControlArc> controlArcs;

                    controlArcs = controller.ControlArcs();
                    if(controlArcs != null && controlArcs.Count != 0)
                    {
                        foreach (ControlArc controlArc in controlArcs)
                        {
                            ISystemConnection systemConnection = Connect_ControlArc(systemPlantRoom, controlArc);
                            if (systemConnection != null)
                            {
                                systemPlantRoom.Connect(systemConnection, airSystem);
                            }
                        }
                    }

                    controlArcs = controller.ChainControlArcs();
                    if (controlArcs != null && controlArcs.Count != 0)
                    {
                        foreach (ControlArc controlArc in controlArcs)
                        {
                            Controller controller_2 = controlArc.GetController();
                            if(controller_2 == null)
                            {
                                continue;
                            }

                            IReference reference_2 = controller_2.Reference();

                            ISystemController systemController_2 = systemPlantRoom.SystemController<ISystemController>(reference_2);
                            if (systemController_2 == null)
                            {
                                continue;
                            }

                            List<Point2D> point2Ds = Query.Point2Ds(controlArc);
                            point2Ds = point2Ds == null ? new List<Point2D>() : point2Ds;

                            Dictionary<int, Point2D> dictionary_1 = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
                            if (dictionary_1 == null || dictionary_1.Count == 0)
                            {
                                continue;
                            }

                            Dictionary<int, Point2D> dictionary_2 = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController_2 as dynamic, systemType_Control, direction: Direction.Out);
                            if (dictionary_2 == null || dictionary_2.Count == 0)
                            {
                                continue;
                            }

                            point2Ds.Add(dictionary_1.Values.First());
                            point2Ds.Insert(0, dictionary_2.Values.First());

                            DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController, dictionary_1.Keys.First(), systemController_2, dictionary_2.Keys.First()), point2Ds?.ToArray());

                            systemPlantRoom.Connect(displaySystemConnection, airSystem);
                        }
                    }

                    List<ISystemSensor> systemSensors = systemPlantRoom.Connect_SystemSensors(controller);
                    if(systemSensors != null)
                    {
                        foreach(ISystemSensor systemSensor in systemSensors)
                        {
                            systemPlantRoom.Connect(systemSensor, airSystem);
                        }

                    }
                }
            }

            return result;
        }

        private static ISystemConnection Connect_ControlArc(SystemPlantRoom systemPlantRoom, ControlArc controlArc)
        {
            if(systemPlantRoom == null || controlArc == null)
            {
                return null;
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            Controller controller = controlArc.GetController();
            if(controller == null)
            {
                return null;
            }

            ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(controller.Reference());
            if (systemController == null)
            {
                return null;
            }


            global::TPD.SystemComponent systemComponent_TPD = controlArc.GetComponent();
            if (systemComponent_TPD == null)
            {
                return null;
            }

            string reference_2 = (systemComponent_TPD as dynamic)?.GUID;

            Core.Systems.ISystemComponent systemComponent_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
            if (systemComponent_SAM == null)
            {
                return null;
            }

            List<Point2D> point2Ds = Query.Point2Ds(controlArc);

            Dictionary<int, Point2D> dictionary_1 = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.Out);
            if (dictionary_1 == null || dictionary_1.Count == 0)
            {
                return null;
            }

            Dictionary<int, Point2D> dictionary_2 = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemComponent_SAM as dynamic, systemType_Control);
            if (dictionary_2 == null || dictionary_2.Count == 0)
            {
                return null;
            }

            int port = controlArc.ControlPort;

            int index_1 = dictionary_1.Keys.ElementAt(0);
            int index_2 = dictionary_2.Keys.ElementAt(0);
            if (dictionary_2.Count > 1)
            {
                index_2 = port == 1 ? dictionary_2.Keys.Max() : dictionary_2.Keys.Min();
            }

            Point2D point2D_1 = dictionary_1[index_1];
            Point2D point2D_2 = dictionary_2[index_2];

            List<Point2D> point2Ds_Temp = point2Ds == null ? null : new List<Point2D>(point2Ds);
            if (point2Ds_Temp == null || point2Ds_Temp.Count == 0)
            {
                point2Ds_Temp = new List<Point2D>() { point2D_1, point2D_2 };
            }
            else
            {
                if (point2D_1.Distance(point2Ds_Temp.First()) + point2D_2.Distance(point2Ds_Temp.Last()) < point2D_1.Distance(point2Ds_Temp.Last()) + point2D_2.Distance(point2Ds_Temp.First()))
                {
                    point2Ds_Temp.Insert(0, point2D_1);
                    point2Ds_Temp.Add(point2D_2);
                }
                else
                {
                    point2Ds_Temp.Insert(0, point2D_2);
                    point2Ds_Temp.Add(point2D_1);
                }

            }

            return new DisplaySystemConnection(new SystemConnection(new SystemType(typeof(AirSystem)), systemController, index_1, systemComponent_SAM, index_2), point2Ds_Temp?.ToArray());

        }

        private static List<ISystemSensor> Connect_SystemSensors(this SystemPlantRoom systemPlantRoom, Controller controller)
        {
            if(systemPlantRoom == null || controller == null)
            {
                return null;
            }

            List<ISystemSensor> result = new List<ISystemSensor>();

            List<SensorArc> sensorArcs = controller.SensorArcs();
            if (sensorArcs == null || sensorArcs.Count == 0)
            {
                return result;
            }

            foreach (SensorArc sensorArc in sensorArcs)
            {
                ISystemSensor systemSensor = Connect(systemPlantRoom, sensorArc);
                if (systemSensor != null)
                {
                    result.Add(systemSensor);
                }
            }

            return result;
        }

        private static ISystemSensor Connect(SystemPlantRoom systemPlantRoom, SensorArc sensorArc)
        {
            if(systemPlantRoom == null || sensorArc == null)
            {
                return null;
            }

            Controller controller = sensorArc.GetController();
            if(controller == null)
            {
                return null;
            }

            ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(controller.Reference());
            if (systemController == null)
            {
                return null;
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            List<Point2D> point2Ds = Query.Point2Ds(sensorArc);
            point2Ds = point2Ds == null ? new List<Point2D>() : point2Ds;

            Dictionary<int, Point2D> dictionary;

            dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
            if (dictionary == null || dictionary.Count == 0)
            {
                return null;
            }

            point2Ds.Add(dictionary.Values.First());

            ISystemJSAMObject systemJSAMObject = null;

            dynamic @dynamic = sensorArc.GetComponent();
            if (@dynamic != null)
            {
                string reference_2 = (@dynamic)?.GUID;
                systemJSAMObject = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                if (systemJSAMObject != null)
                {

                    dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemJSAMObject as dynamic, systemType_Control, direction: Direction.Out);
                    if(dictionary != null && dictionary.Count != 0)
                    {
                        point2Ds.Insert(0, dictionary.Values.First());
                    }
                }
            }

            if (systemJSAMObject == null)
            {
                @dynamic = sensorArc.GetDuct();
                if (dynamic == null)
                {
                    return null;
                }

                DisplaySystemConnection displaySystemConnection = systemPlantRoom.Find<DisplaySystemConnection>(x => x.Reference() == Query.Reference(dynamic));

                SystemPolyline systemPolyline = displaySystemConnection?.SystemGeometry;
                if (systemPolyline != null)
                {
                    Polyline2D polyline2D = new Polyline2D(systemPolyline);

                    point2Ds.Insert(0, polyline2D.Closest(point2Ds[0], true));
                }

                systemJSAMObject = displaySystemConnection;
            }

            if (systemJSAMObject == null)
            {
                return null;
            }

            DisplaySystemSensor result = new DisplaySystemSensor(new SystemSensor(), point2Ds?.ToArray());

            systemPlantRoom.Add(result);

            systemPlantRoom.Connect(result, systemJSAMObject as dynamic);
            systemPlantRoom.Connect(result, systemController);

            return result;
        }

        public static ISystemConnection Connect(this SystemPlantRoom systemPlantRoom, Core.Systems.ISystemComponent systemComponent_1, Core.Systems.ISystemComponent systemComponent_2, Core.Systems.ISystem system, Direction direction, IEnumerable<Point2D> point2Ds = null)
        {
            if(systemPlantRoom == null || systemComponent_1 == null || systemComponent_2 == null || system == null)
            {
                return null;
            }

            if (!Geometry.Systems.Query.TryGetIndexes(systemPlantRoom, systemComponent_1, systemComponent_2, out int index_1, out int index_2, new SystemType(system), direction))
            {
                return null;
            }

            if (!(systemComponent_1 is IDisplaySystemObject<SystemGeometryInstance>) || !(systemComponent_2 is IDisplaySystemObject<SystemGeometryInstance>))
            {
                if(systemPlantRoom.Connect(systemComponent_1, systemComponent_2, out ISystemConnection systemConnection, system, index_1, index_2))
                {
                    return systemConnection;
                }

                return null;
            }

            Point2D point2D_1 = (systemComponent_1 as dynamic).SystemGeometry.GetPoint2D(index_1);
            if (point2D_1 == null)
            {
                return null;
            }

            Point2D point2D_2 = (systemComponent_2 as dynamic).SystemGeometry.GetPoint2D(index_2);
            if (point2D_2 == null)
            {
                return null;
            }

            List<Point2D> point2Ds_Temp = point2Ds == null ? null : new List<Point2D>(point2Ds);
            if (point2Ds_Temp == null || point2Ds_Temp.Count == 0)
            {
                point2Ds_Temp = new List<Point2D>() { point2D_1, point2D_2 };
            }
            else
            {
                if (point2D_1.Distance(point2Ds_Temp.First()) + point2D_2.Distance(point2Ds_Temp.Last()) < point2D_1.Distance(point2Ds_Temp.Last()) + point2D_2.Distance(point2Ds_Temp.First()))
                {
                    point2Ds_Temp.Insert(0, point2D_1);
                    point2Ds_Temp.Add(point2D_2);
                }
                else
                {
                    point2Ds_Temp.Insert(0, point2D_2);
                    point2Ds_Temp.Add(point2D_1);
                }

            }

            if (point2Ds_Temp == null || point2Ds_Temp.Count < 2)
            {
                return null;
            }

            DisplaySystemConnection result = new DisplaySystemConnection(new SystemConnection(new SystemType(system), systemComponent_1, index_1, systemComponent_2, index_2), point2Ds_Temp?.ToArray());

            return systemPlantRoom.Connect(result, system) ? result : null;
        }

        public static List<ISystemConnection> Connect(this SystemPlantRoom systemPlantRoom, ComponentGroup componentGroup)
        {
            if(systemPlantRoom == null || componentGroup == null)
            {
                return null;
            }

            List<global::TPD.SystemComponent> systemComponents = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);
            if(systemComponents == null)
            {
                return null;
            }

            List<ISystemConnection> result = new List<ISystemConnection>();

            //Connect componentGroup with the rest of the system
            foreach (global::TPD.SystemComponent systemComponent_Temp in systemComponents)
            {
                if (!(systemComponent_Temp is Junction))
                {
                    continue;
                }

                AirSystem airSystem = systemPlantRoom.System<AirSystem>(((systemComponent_Temp as dynamic)?.GetSystem() as dynamic)?.GUID as string);

                List<global::TPD.SystemComponent> systemComponents_In = Query.ConnectedSystemComponents(systemComponent_Temp, Direction.In);
                if (systemComponents_In == null || systemComponents_In.Count == 0)
                {
                    continue;
                }

                systemComponents_In.RemoveAll(x => (x as dynamic).Guid == (componentGroup as dynamic).Guid);

                List<global::TPD.SystemComponent> systemComponents_Out = Query.ConnectedSystemComponents(systemComponent_Temp, Direction.Out);
                if (systemComponents_Out == null || systemComponents_Out.Count == 0)
                {
                    continue;
                }

                systemComponents_Out.RemoveAll(x => (x as dynamic).Guid == (componentGroup as dynamic).Guid);

                foreach (global::TPD.SystemComponent systemComponent_In in systemComponents_In)
                {
                    Core.Systems.ISystemComponent systemComponent_In_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x?.Reference() == (systemComponent_In as dynamic).GUID);
                    if (systemComponent_In_SAM == null)
                    {
                        continue;
                    }

                    foreach (global::TPD.SystemComponent systemComponent_Out in systemComponents_Out)
                    {
                        Core.Systems.ISystemComponent systemComponent_Out_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x?.Reference() == (systemComponent_Out as dynamic).GUID);
                        if (systemComponent_Out_SAM == null)
                        {
                            continue;
                        }

                        List<Duct> ducts = Query.Ducts(systemComponent_In, Direction.Out, systemComponent_Out);

                        List<Point2D> point2Ds = null;
                        if(ducts != null && ducts.Count != 0)
                        {
                            point2Ds = new List<Point2D>();
                            foreach(Duct duct in ducts)
                            {
                                List<Point2D> point2Ds_Temp = Query.Point2Ds(duct);
                                if(point2Ds_Temp != null)
                                {
                                    point2Ds.AddRange(point2Ds_Temp);
                                }

                            }
                        }

                        ISystemConnection systemConnection = Connect(systemPlantRoom, systemComponent_In_SAM, systemComponent_Out_SAM, airSystem, Direction.Out, point2Ds);
                        
                        if (systemConnection != null)
                        {
                            if(ducts != null && ducts.Count != 0)
                            {
                                systemConnection.SetReference(Query.Reference(ducts[0]));
                                systemPlantRoom.Add(systemConnection);
                            }

                            result.Add(systemConnection);
                        }
                    }
                }

            }

            return result;
        }

    }
}