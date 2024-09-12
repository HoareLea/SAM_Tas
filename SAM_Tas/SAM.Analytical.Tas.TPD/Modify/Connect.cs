using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.Systems;
using System;
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

                    //List<Tuple<string, string, string, global::TPD.SystemComponent>> tuples_SystemComponent = new List<Tuple<string, string, string, global::TPD.SystemComponent>>();

                    //List<global::TPD.SystemComponent> systemComponents_ComponentGroup = Query.SystemComponents<global::TPD.SystemComponent>(componentGroup);
                    //foreach(global::TPD.SystemComponent systemComponent in systemComponents_ComponentGroup)
                    //{
                    //    List<ControlArc> controlArcs = systemComponent.ControlArcs();
                    //    if(controlArcs != null)
                    //    {
                    //        foreach(ControlArc controlArc in controlArcs)
                    //        {
                    //            Controller controller_Chain = controlArc.GetChainController();
                    //            Controller controller = controlArc.GetController();

                    //            tuples_SystemComponent.Add(new Tuple<string, string, string, global::TPD.SystemComponent>((systemComponent as dynamic)?.Name, (controller as dynamic)?.Name, (controller_Chain as dynamic)?.Name, systemComponent));
                    //        }
                    //    }
                    //}

                    //List<Tuple<string, int, int, ComponentGroup>> tuples_ComponentGroup = new List<Tuple<string, int, int, ComponentGroup>>();
                    //foreach (Controller controller in controllers_Group)
                    //{
                    //    //tuples_ComponentGroup.Add(new Tuple<string, int, int, ComponentGroup>((controller as dynamic).Name, controller.GetControlArcCount(), controller.GetChainArcCount(), (controller as dynamic).GetGroup()));
                    //    tuples_ComponentGroup.Add(new Tuple<string, int, int, ComponentGroup>((controller as dynamic).Name, controller.GetSensorArcCount(), -1, (controller as dynamic).GetGroup()));
                    //}

                    List<Controller> controllers_Temp = null;

                    controllers_Temp = controllers_Group.FindAll(x => x.ControlType != tpdControlType.tpdControlGroup);
                    for (int i = 1; i <= controllers_Temp.Count; i++)
                    {
                        Controller controller_1 = controllers_Temp[i - 1];
                        IReference reference_1 = controller_1.Reference();
                        ISystemController systemController_1 = systemPlantRoom.SystemController<ISystemController>(reference_1);
                        if (systemController_1 == null)
                        {
                            continue;
                        }

                        ControlArc controlArc = componentGroup.GetGroupControlArc(i, 1);
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

                        //Controller controller_2 = controlArc.GetController();
                        //IReference reference_2 = controller_2?.Reference();

                        List<ControlArc> controlArcs = new List<ControlArc>();

                        List<ControlArc> controlArcs_Chain = controller_2.ChainControlArcs();
                        if(controlArcs_Chain != null)
                        {
                            foreach (ControlArc controlArc_Temp in controlArcs_Chain)
                            {
                                ComponentGroup componentGroup_Temp = controlArc_Temp.GetGroup();
                                if(componentGroup_Temp != null && componentGroup_Temp.Reference() == componentGroup.Reference())
                                {
                                    controlArcs.Add(controlArc_Temp);
                                }
                            }
                        }

                        List<ControlArc> controlArcs_Temp = controller_2.ControlArcs();
                        if (controlArcs_Temp != null)
                        {
                            foreach (ControlArc controlArc_Temp in controlArcs_Temp)
                            {
                                ComponentGroup componentGroup_Temp = controlArc_Temp.GetGroup();
                                if (componentGroup_Temp != null && componentGroup_Temp.Reference() == componentGroup.Reference())
                                {
                                    controlArcs.Add(controlArc_Temp);
                                }
                            }
                        }

                        if(controlArcs == null || controlArcs.Count == 0)
                        {
                            continue;
                        }

                        controlArcs.Add(controlArc);


                        List<Point2D> point2Ds = new List<Point2D>();
                        foreach (ControlArc controlArc_Temp in controlArcs)
                        {
                            List<Point2D> point2Ds_Temp = Query.Point2Ds(controlArc_Temp);
                        }

                        HashSet<int> indexes_1 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController_1 as dynamic, systemType_Control, direction: Direction.Out);
                        if (indexes_1 == null || indexes_1.Count == 0)
                        {
                            continue;
                        }

                        HashSet<int> indexes_2 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController_2 as dynamic, systemType_Control, direction: Direction.In);
                        if (indexes_2 == null || indexes_2.Count == 0)
                        {
                            continue;
                        }


                        //TODO: Finish here

                        int index_1 = indexes_1.ElementAt(0);
                        int index_2 = indexes_2.ElementAt(0);

                        Point2D point2D_1 = (systemController_1 as dynamic).SystemGeometry.GetPoint2D(index_1);
                        if (point2D_1 == null)
                        {
                            return null;
                        }

                        point2Ds.Add(point2D_1);

                        Point2D point2D_2 = (systemController_2 as dynamic).SystemGeometry.GetPoint2D(index_2);
                        if (point2D_2 == null)
                        {
                            return null;
                        }

                        point2Ds.Insert(0, point2D_2);


                        DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController_1, index_1, systemController_2, index_2), point2Ds?.ToArray());

                        systemPlantRoom.Connect(displaySystemConnection, airSystem);
                    }

                    for (int i = 1; i <= controllers_Temp.Count; i++)
                    {
                        Controller controller = controllers_Temp[i - 1];
                        ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(controller.Reference());
                        if (systemController == null)
                        {
                            continue;
                        }

                        List<SensorArc> sensorArcs = controller.SensorArcs();
                        if (sensorArcs == null)
                        {
                            continue;
                        }

                        HashSet<int> indexes = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
                        if (indexes != null && indexes.Count != 0)
                        {
                            Point2D point2D_1 = (systemController as dynamic).SystemGeometry.GetPoint2D(indexes.ElementAt(0));
                            if (point2D_1 == null)
                            {
                                return null;
                            }

                            foreach (SensorArc sensorArc in sensorArcs)
                            {
                                List<Point2D> point2Ds = Query.Point2Ds(sensorArc);
                                point2Ds = point2Ds == null ? new List<Point2D>() : point2Ds;

                                point2Ds.Add(point2D_1);

                                ISystemJSAMObject systemJSAMObject = null;

                                dynamic @dynamic = sensorArc.GetComponent();
                                if (@dynamic != null)
                                {
                                    string reference_2 = (@dynamic)?.GUID;
                                    systemJSAMObject = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                                    if(systemJSAMObject != null)
                                    {
                                        HashSet<int> indexes_1 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemJSAMObject as dynamic, systemType_Control, direction: Direction.Out);
                                        if (indexes_1 != null && indexes_1.Count != 0)
                                        {
                                            Point2D point2D_2 = (systemJSAMObject as dynamic).SystemGeometry.GetPoint2D(indexes_1.ElementAt(0));
                                            if (point2D_2 != null)
                                            {
                                                point2Ds.Insert(0, point2D_2);
                                            }
                                        }
                                    }
                                }

                                if (systemJSAMObject == null)
                                {
                                    @dynamic = sensorArc.GetDuct();
                                    if (dynamic == null)
                                    {
                                        continue;
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
                                    continue;
                                }

                                DisplaySystemSensor displaySystemSensor = new DisplaySystemSensor(new SystemSensor(), point2Ds?.ToArray());

                                systemPlantRoom.Add(displaySystemSensor);

                                systemPlantRoom.Connect(displaySystemSensor, systemJSAMObject as dynamic);
                                systemPlantRoom.Connect(displaySystemSensor, systemController);
                                systemPlantRoom.Connect(displaySystemSensor, airSystem);
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
                            int port = controlArc.ControlPort;

                            global::TPD.SystemComponent systemComponent_TPD = controlArc.GetComponent();
                            if(systemComponent_TPD == null)
                            {
                                continue;
                            }

                            string reference_2 = (systemComponent_TPD as dynamic)?.GUID;

                            Core.Systems.ISystemComponent systemComponent_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                            if (systemComponent_SAM == null)
                            {
                                continue;
                            }

                            List<Point2D> point2Ds = Query.Point2Ds(controlArc);


                            HashSet<int> indexes_1 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.Out);
                            if (indexes_1 == null || indexes_1.Count == 0)
                            {
                                continue;
                            }

                            HashSet<int> indexes_2 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemComponent_SAM as dynamic, systemType_Control);
                            if (indexes_2 == null || indexes_2.Count == 0)
                            {
                                continue;
                            }

                            if(indexes_2.Count > 1)
                            {
                                indexes_2 = new HashSet<int>() { port == 1 ? indexes_2.Max() : indexes_2.Min() };
                            }

                            if(indexes_1.Count == 1 && indexes_2.Count == 1)
                            {
                                int index_1 = indexes_1.ElementAt(0);
                                int index_2 = indexes_2.ElementAt(0);

                                Point2D point2D_1 = (systemController as dynamic).SystemGeometry.GetPoint2D(index_1);
                                if (point2D_1 == null)
                                {
                                    return null;
                                }

                                Point2D point2D_2 = (systemComponent_SAM as dynamic).SystemGeometry.GetPoint2D(index_2);
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

                                DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController, index_1, systemComponent_SAM, index_2), point2Ds_Temp?.ToArray());

                                systemPlantRoom.Connect(displaySystemConnection, airSystem);
                            }
                        }
                    }

                    controlArcs = controller.ChainControlArcs();
                    if (controlArcs != null && controlArcs.Count != 0)
                    {
                        foreach (ControlArc controlArc in controlArcs)
                        {
                            Controller controller_2 = controlArc.GetController();
                            //if (controller_2 == null)
                            //{
                            //    ComponentGroup componentGroup = controlArc.GetGroup();
                            //    if (componentGroup == null)
                            //    {
                            //        continue;
                            //    }

                            //    int port = controlArc.ControlPort;

                            //    ControlArc controlArc_Temp = (componentGroup as dynamic).GetGroupControlArc(port + 1, 1);
                            //    if (controlArc_Temp == null)
                            //    {
                            //        continue;
                            //    }

                            //    Controller controller_Group = controlArc_Temp.GetChainController();
                            //    if(controller_Group == null)
                            //    {
                            //        controller_Group = controlArc_Temp.GetController();
                            //    }

                            //    controller_2 = controller_Group;
                            //}

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

                            HashSet<int> indexes_1 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
                            if (indexes_1 == null || indexes_1.Count == 0)
                            {
                                continue;
                            }

                            HashSet<int> indexes_2 = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController_2 as dynamic, systemType_Control, direction: Direction.Out);
                            if (indexes_2 == null || indexes_2.Count == 0)
                            {
                                continue;
                            }

                            int index_1 = indexes_1.ElementAt(0);
                            int index_2 = indexes_2.ElementAt(0);

                            Point2D point2D_1 = (systemController as dynamic).SystemGeometry.GetPoint2D(index_1);
                            if (point2D_1 == null)
                            {
                                return null;
                            }

                            point2Ds.Add(point2D_1);

                            Point2D point2D_2 = (systemController_2 as dynamic).SystemGeometry.GetPoint2D(index_2);
                            if (point2D_2 == null)
                            {
                                return null;
                            }

                            point2Ds.Insert(0, point2D_2);


                            DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController, index_1, systemController_2, index_2), point2Ds?.ToArray());

                            systemPlantRoom.Connect(displaySystemConnection, airSystem);
                        }
                    }

                    HashSet<int> indexes = Geometry.Systems.Query.FindIndexes(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
                    if(indexes != null && indexes.Count != 0)
                    {
                        Point2D point2D_1 = (systemController as dynamic).SystemGeometry.GetPoint2D(indexes.ElementAt(0));
                        if (point2D_1 == null)
                        {
                            return null;
                        }

                        List<SensorArc> sensorArcs = controller.SensorArcs();
                        if (sensorArcs != null && sensorArcs.Count != 0)
                        {
                            foreach (SensorArc sensorArc in sensorArcs)
                            {
                                List<Point2D> point2Ds = Query.Point2Ds(sensorArc);
                                point2Ds = point2Ds == null ? new List<Point2D>() : point2Ds;

                                point2Ds.Add(point2D_1);

                                ISystemJSAMObject systemJSAMObject = null;

                                dynamic @dynamic = sensorArc.GetComponent();
                                if (@dynamic != null)
                                {
                                    string reference_2 = (@dynamic)?.GUID;
                                    systemJSAMObject = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                                }

                                if (systemJSAMObject == null)
                                {
                                    @dynamic = sensorArc.GetDuct();
                                    if(dynamic == null)
                                    {
                                        continue;
                                    }

                                    string reference = Query.Reference(dynamic);
                                    DisplaySystemConnection displaySystemConnection = systemPlantRoom.Find<DisplaySystemConnection>(x => x.Reference() == reference);

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
                                    continue;
                                }

                                DisplaySystemSensor displaySystemSensor = new DisplaySystemSensor(new SystemSensor(), point2Ds?.ToArray());

                                systemPlantRoom.Add(displaySystemSensor);

                                systemPlantRoom.Connect(displaySystemSensor, systemJSAMObject as dynamic);
                                systemPlantRoom.Connect(displaySystemSensor, systemController);
                                systemPlantRoom.Connect(displaySystemSensor, airSystem);
                            }
                        }
                    }



                }
            }

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