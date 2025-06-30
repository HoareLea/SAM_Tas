using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using SAM.Geometry.Systems;
using System.Collections.Generic;
using System.Linq;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<ISystemConnection> Connect(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, LiquidSystem liquidSystem)
        {
            if(systemPlantRoom == null || plantRoom == null)
            {
                return null;
            }

            List<PlantComponent> plantComponents = plantRoom?.PlantComponents<PlantComponent>(true);
            if (plantComponents == null)
            {
                return null;
            }

            Dictionary<string, PlantComponent> dictionary_PlantComponent = new Dictionary<string, PlantComponent>();
            foreach (PlantComponent plantComponent in plantComponents)
            {
                string reference = plantComponent?.Reference();
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                dictionary_PlantComponent[reference] = plantComponent;
            }

            List<ISystemConnection> result = new List<ISystemConnection>();

            List<PlantComponentGroup> plantComponentGroups = null;

            HashSet<string> pipeReferences = new HashSet<string>();

            plantComponents = new List<PlantComponent>(dictionary_PlantComponent.Values);
            if (plantComponents != null && plantComponents.Count != 0)
            {
                plantComponentGroups = new List<PlantComponentGroup>();

                foreach (PlantComponent plantComponent_TPD in plantComponents)
                {
                    if (plantComponent_TPD == null)
                    {
                        continue;
                    }

                    if (plantComponent_TPD is PlantComponentGroup)
                    {
                        plantComponentGroups.Add((PlantComponentGroup)plantComponent_TPD);
                    }

                    string reference_1 = (plantComponent_TPD as dynamic).GUID;

                    Core.Systems.ISystemComponent systemComponent_SAM_1 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_1);

                    List<LiquidSystem> liquidSystems_1 = systemPlantRoom.GetSystems<LiquidSystem>(systemComponent_SAM_1);
                    if (liquidSystems_1 == null || liquidSystems_1.Count == 0)
                    {
                        continue;
                    }

                    foreach (Direction direction in new Direction[] { Direction.In, Direction.Out })
                    {
                        List<Pipe> pipes = Query.Pipes(plantComponent_TPD, direction);
                        if (pipes == null || pipes.Count == 0)
                        {
                            continue;
                        }

                        foreach (Pipe pipe in pipes)
                        {
                            string pipeReference = Query.Reference(pipe);
                            if(pipeReferences.Contains(pipeReference))
                            {
                                continue;
                            }

                            pipeReferences.Add(pipeReference);

                            string reference_2 = (direction == Direction.In ? pipe.GetUpstreamComponent() : pipe.GetDownstreamComponent() as dynamic)?.GUID;

                            Core.Systems.ISystemComponent systemComponent_SAM_2 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                            if (systemComponent_SAM_2 == null)
                            {
                                continue;
                            }

                            List<LiquidSystem> liquidSystems_2 = systemPlantRoom.GetSystems<LiquidSystem>(systemComponent_SAM_2);
                            if (liquidSystems_2 == null || liquidSystems_2.Count == 0)
                            {
                                continue;
                            }

                            List<Point2D> point2Ds = Query.Point2Ds(pipe);

                            int connectionIndex_1 = direction == Direction.In ? pipe.GetDownstreamComponentPort() : pipe.GetUpstreamComponentPort();
                            //if (systemComponent_SAM_1 is SystemWaterSourceHeatPump)
                            //{
                            //    if(connectionIndex_1 == 2)
                            //    {
                            //        connectionIndex_1 += 2;
                            //    }
                            //}

                            int connectionIndex_2 = direction == Direction.In ? pipe.GetUpstreamComponentPort() : pipe.GetDownstreamComponentPort();
                            //if (systemComponent_SAM_2 is SystemWaterSourceHeatPump)
                            //{
                            //    if (connectionIndex_2 == 2)
                            //    {
                            //        connectionIndex_2 += 2;
                            //    }
                            //}

                            ISystemConnection systemConnection = Connect(
                                systemPlantRoom,
                                systemComponent_SAM_1,
                                connectionIndex_1,
                                systemComponent_SAM_2,
                                connectionIndex_2,
                                liquidSystem,
                                direction,
                                point2Ds);


                            if (systemConnection != null)
                            {
                                if (systemConnection is SAMObject)
                                {
                                    string fluidTypeName = pipe.GetFluid()?.Name;
                                    if (string.IsNullOrWhiteSpace(fluidTypeName))
                                    {
                                        ((SAMObject)systemConnection).SetValue(SystemConnectionParameter.FluidTypeName, fluidTypeName);
                                    }
                                }

                                systemConnection.SetReference(pipeReference);
                                systemPlantRoom.Add(systemConnection);

                                result.Add(systemConnection);
                            }
                        }
                    }
                }
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            if (plantComponentGroups != null && plantComponentGroups.Count != 0)
            {
                foreach (PlantComponentGroup plantComponentGroup in plantComponentGroups)
                {
                    List<PlantController> plantControllers_Group = plantComponentGroup.PlantControllers();
                    if (plantControllers_Group == null || plantControllers_Group.Count == 0)
                    {
                        continue;
                    }

                    List<PlantController> plantControllers_Temp = null;

                    plantControllers_Temp = plantControllers_Group.FindAll(x => x.ControlType == tpdControlType.tpdControlGroup);
                    int count = plantControllers_Temp.Count;
                    for (int i = 0; i < count; i++)
                    {
                        PlantController plantController_1 = plantControllers_Temp[i];
                        int chainArcCount = plantController_1.GetChainArcCount();
                        if (chainArcCount == 0)
                        {
                            continue;
                        }

                        plantController_1 = (plantController_1.GetChainArc(1) as dynamic).GetController();

                        IReference reference_1 = plantController_1.Reference();
                        ISystemController systemController_1 = systemPlantRoom.SystemController<ISystemController>(reference_1);
                        if (systemController_1 == null)
                        {
                            continue;
                        }

                        PlantControlArc plantControlArc = plantComponentGroup.GetGroupControlArc(count - i, 1);
                        if (plantControlArc == null)
                        {
                            continue;
                        }

                        PlantController plantController_2 = plantControlArc.GetChainController();
                        IReference reference_2 = plantController_2?.Reference();
                        ISystemController systemController_2 = systemPlantRoom.SystemController<ISystemController>(reference_2);
                        if (systemController_2 == null)
                        {
                            continue;
                        }

                        List<PlantControlArc> plantControlArcs = Query.PlantControlArcs(plantController_2, plantComponentGroup);
                        if (plantControlArcs == null || plantControlArcs.Count == 0)
                        {
                            continue;
                        }

                        plantControlArcs.Add(plantControlArc);

                        Dictionary<int, Point2D> dictionary = null;

                        List<Point2D> point2Ds = new List<Point2D>();

                        dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController_1 as dynamic, systemType_Control, direction: Direction.Out);
                        if (dictionary == null || dictionary.Count == 0)
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

                        DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(liquidSystem), systemController_1, index_1, systemController_2, index_2), point2Ds?.ToArray());

                        systemPlantRoom.Connect(displaySystemConnection, liquidSystem);
                    }

                    plantControllers_Temp = plantControllers_Group.FindAll(x => x.ControlType != tpdControlType.tpdControlGroup);
                    foreach (PlantController plantController in plantControllers_Temp)
                    {
                        List<ISystemSensor> systemSensors = systemPlantRoom.Connect_SystemSensors(plantController);
                        if (systemSensors != null)
                        {
                            foreach (ISystemSensor systemSensor in systemSensors)
                            {
                                systemPlantRoom.Connect(systemSensor, liquidSystem);
                            }

                        }

                        List<PlantControlArc> plantControlArcs = plantControlArcs = plantController.PlantControlArcs();
                        if (plantControlArcs != null && plantControlArcs.Count != 0)
                        {
                            foreach (PlantControlArc plantControlArc in plantControlArcs)
                            {
                                ISystemConnection systemConnection = Connect_PlantControlArc(systemPlantRoom, plantControlArc);
                                if (systemConnection != null)
                                {
                                    systemPlantRoom.Connect(systemConnection, liquidSystem);
                                }
                            }
                        }
                    }
                }
            }

            List<PlantController> plantControllers = Query.PlantControllers(plantRoom);
            if (plantControllers != null && plantControllers.Count != 0)
            {
                foreach (PlantController plantController in plantControllers)
                {
                    if (plantController == null)
                    {
                        continue;
                    }

                    IReference reference_1 = plantController.Reference();

                    ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(reference_1);
                    if (systemController == null)
                    {
                        continue;
                    }

                    List<PlantControlArc> plantControlArcs;

                    plantControlArcs = plantController.PlantControlArcs();
                    if (plantControlArcs != null && plantControlArcs.Count != 0)
                    {
                        foreach (PlantControlArc plantControlArc in plantControlArcs)
                        {
                            ISystemConnection systemConnection = Connect_PlantControlArc(systemPlantRoom, plantControlArc);
                            if (systemConnection != null)
                            {
                                systemPlantRoom.Connect(systemConnection, liquidSystem);
                            }
                        }
                    }

                    plantControlArcs = plantController.ChainPlantControlArcs();
                    if (plantControlArcs != null && plantControlArcs.Count != 0)
                    {
                        foreach (PlantControlArc plantControlArc in plantControlArcs)
                        {
                            PlantController plantController_2 = plantControlArc.GetController();
                            if (plantController_2 == null)
                            {
                                continue;
                            }

                            IReference reference_2 = plantController_2.Reference();

                            ISystemController systemController_2 = systemPlantRoom.SystemController<ISystemController>(reference_2);
                            if (systemController_2 == null)
                            {
                                continue;
                            }

                            List<Point2D> point2Ds = Query.Point2Ds(plantControlArc);
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

                            int controlPort = plantControlArc.ControlPort + 1;

                            int index_1 = dictionary_1.ContainsKey(controlPort) ? controlPort : dictionary_1.Keys.First();
                            int index_2 = dictionary_2.Keys.First();

                            point2Ds.Add(dictionary_1[index_1]);
                            point2Ds.Insert(0, dictionary_2[index_2]);

                            DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(liquidSystem), systemController, index_1, systemController_2, index_2), point2Ds?.ToArray());

                            systemPlantRoom.Connect(displaySystemConnection, liquidSystem);
                        }
                    }

                    List<ISystemSensor> systemSensors = systemPlantRoom.Connect_SystemSensors(plantController);
                    if (systemSensors != null)
                    {
                        foreach (ISystemSensor systemSensor in systemSensors)
                        {
                            systemPlantRoom.Connect(systemSensor, liquidSystem);
                        }

                    }
                }
            }

            return result;
        }
        
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

                HashSet<string> references_Duct = new HashSet<string>();

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

                    foreach (Direction direction in new Direction[] { Direction.In, Direction.Out })
                    {
                        List<Duct> ducts = Query.Ducts(systemComponent_TPD, direction);
                        if (ducts == null || ducts.Count == 0)
                        {
                            continue;
                        }

                        foreach (Duct duct in ducts)
                        {
                            string reference_Duct = duct?.Reference();
                            if(string.IsNullOrWhiteSpace(reference_Duct) || references_Duct.Contains(reference_Duct))
                            {
                                continue;
                            }

                            references_Duct.Add(reference_Duct);

                            string reference_2 = ((direction == Direction.Out ? duct.GetDownstreamComponent() : duct.GetUpstreamComponent()) as dynamic)?.GUID;

                            Core.Systems.ISystemComponent systemComponent_SAM_2 = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                            if (systemComponent_SAM_2 == null)
                            {
                                continue;
                            }

                            List<Point2D> point2Ds = Query.Point2Ds(duct);

                            ISystemConnection systemConnection = Connect(
                                systemPlantRoom, 
                                systemComponent_SAM_1, 
                                direction == Direction.Out ? duct.GetUpstreamComponentPort() : duct.GetDownstreamComponentPort(), 
                                systemComponent_SAM_2, 
                                direction == Direction.Out ? duct.GetDownstreamComponentPort() : duct.GetUpstreamComponentPort(), 
                                airSystem, 
                                direction, 
                                point2Ds);

                            if (systemConnection != null)
                            {
                                systemConnection.SetReference(reference_Duct);
                                systemPlantRoom.Add(systemConnection);

                                result.Add(systemConnection);
                            }
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

                            //ComponentGroup componentGroup = controller.GetGroup();
                            //if (componentGroup != null)
                            //{
                            //    Point2D location = ((TasPosition)(componentGroup as dynamic).GetPosition())?.ToSAM();
                            //    if (location != null)
                            //    {
                            //        Transform2D transform2D = Transform2D.GetTranslation(location.ToVector());
                            //        if (transform2D != null)
                            //        {
                            //            point2Ds.ForEach(x => x.Transform(transform2D));
                            //        }
                            //    }
                            //}

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

                            int controlPort = controlArc.ControlPort + 1;

                            int index_1 = dictionary_1.ContainsKey(controlPort) ? controlPort : dictionary_1.Keys.First();
                            int index_2 = dictionary_2.Keys.First();

                            point2Ds.Add(dictionary_1[index_1]);
                            point2Ds.Insert(0, dictionary_2[index_2]);

                            DisplaySystemConnection displaySystemConnection = new DisplaySystemConnection(new SystemConnection(new SystemType(airSystem), systemController, index_1, systemController_2, index_2), point2Ds?.ToArray());

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

        private static ISystemConnection Connect_PlantControlArc(SystemPlantRoom systemPlantRoom, PlantControlArc plantControlArc)
        {
            if (systemPlantRoom == null || plantControlArc == null)
            {
                return null;
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            PlantController plantController = plantControlArc.GetController();
            if (plantController == null)
            {
                return null;
            }

            ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(plantController.Reference());
            if (systemController == null)
            {
                return null;
            }


            PlantComponent plantComponent_TPD = plantControlArc.GetComponent();
            if (plantComponent_TPD == null)
            {
                return null;
            }

            string reference_2 = (plantComponent_TPD as dynamic)?.GUID;

            Core.Systems.ISystemComponent systemComponent_SAM = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
            if (systemComponent_SAM == null)
            {
                return null;
            }

            List<Point2D> point2Ds = Query.Point2Ds(plantControlArc);

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

            int port = plantControlArc.ControlPort;

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

            return new DisplaySystemConnection(new SystemConnection(new SystemType(typeof(LiquidSystem)), systemController, index_1, systemComponent_SAM, index_2), point2Ds_Temp?.ToArray());

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
                if (systemSensor == null)
                {
                    continue;
                }

                result.Add(systemSensor);
            }

            SystemSensorController systemSensorController = systemPlantRoom.SystemController<SystemSensorController>(controller.Reference());
            if (systemSensorController != null)
            {
                string reference = null;

                reference = controller.SensorArc1.Reference();
                if (reference != null)
                {
                    ISystemSensor systemSensor = systemPlantRoom.GetSystemObject<ISystemSensor>(x => x.Reference() == reference);
                    if(systemSensor != null)
                    {
                        systemSensorController.SensorReference = systemSensor.Guid.ToString();
                        systemPlantRoom.Add(systemSensorController);
                    }
                }

                reference = controller.SensorArc2.Reference();
                if (reference != null && systemSensorController is SystemDifferenceController)
                {
                    ISystemSensor systemSensor = systemPlantRoom.GetSystemObject<ISystemSensor>(x => x.Reference() == reference);
                    if (systemSensor != null)
                    {
                        ((SystemDifferenceController)systemSensorController).SecondarySensorReference = systemSensor.Guid.ToString();
                        systemPlantRoom.Add(systemSensorController);
                    }
                }
            }

            return result;
        }

        private static List<ISystemSensor> Connect_SystemSensors(this SystemPlantRoom systemPlantRoom, PlantController plantController)
        {
            if (systemPlantRoom == null || plantController == null)
            {
                return null;
            }

            List<ISystemSensor> result = new List<ISystemSensor>();

            List<PlantSensorArc> plantSensorArcs = plantController.PlantSensorArcs();
            if (plantSensorArcs == null || plantSensorArcs.Count == 0)
            {
                return result;
            }

            foreach (PlantSensorArc plantSensorArc in plantSensorArcs)
            {
                ISystemSensor systemSensor = Connect(systemPlantRoom, plantSensorArc);
                if (systemSensor != null)
                {
                    result.Add(systemSensor);
                }
            }

            SystemSensorController systemSensorController = systemPlantRoom.SystemController<SystemSensorController>(plantController.Reference());
            if (systemSensorController != null)
            {
                string reference;

                reference = plantController.SensorArc1.Reference();
                if (reference != null)
                {
                    ISystemSensor systemSensor = systemPlantRoom.GetSystemObject<ISystemSensor>(x => x.Reference() == reference);
                    if (systemSensor != null)
                    {
                        systemSensorController.SensorReference = systemSensor.Guid.ToString();
                        systemPlantRoom.Add(systemSensorController);
                    }
                }

                reference = plantController.SensorArc2.Reference();
                if (reference != null && systemSensorController is SystemLiquidDifferenceController)
                {
                    ISystemSensor systemSensor = systemPlantRoom.GetSystemObject<ISystemSensor>(x => x.Reference() == reference);
                    if (systemSensor != null)
                    {
                        ((SystemLiquidDifferenceController)systemSensorController).SecondarySensorReference = systemSensor.Guid.ToString();
                        systemPlantRoom.Add(systemSensorController);
                    }
                }
            }

            return result;
        }

        private static ISystemSensor Connect(SystemPlantRoom systemPlantRoom, SensorArc sensorArc)
        {
            if (systemPlantRoom == null || sensorArc == null)
            {
                return null;
            }

            Controller controller = sensorArc.GetController();
            if (controller == null)
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
                    if (dictionary != null && dictionary.Count != 0)
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

                //List<DisplaySystemConnection> displaySystemConnections = systemPlantRoom.GetSystemObjects<DisplaySystemConnection>();
                //List<string> references = displaySystemConnections.ConvertAll(x => x.Reference());

                DisplaySystemConnection displaySystemConnection = systemPlantRoom.Find<DisplaySystemConnection>(x => x.Reference() == Query.Reference(dynamic));

                SystemPolyline systemPolyline = displaySystemConnection?.SystemGeometry;
                if (systemPolyline != null)
                {
                    Polyline2D polyline2D = new Polyline2D(systemPolyline);

                    point2Ds.Insert(0, polyline2D.Closest(point2Ds[0], true));
                }

                //if (componentGroup != null)
                //{
                //    Transform2D transform2D = null;

                //    Point2D location = ((TasPosition)(componentGroup as dynamic).GetPosition())?.ToSAM();
                //    if (location != null)
                //    {
                //        transform2D = Transform2D.GetTranslation(location.ToVector());
                //    }

                //    if (transform2D != null)
                //    {
                //        for (int i = 1; i < point2Ds.Count - 1; i++)
                //        {
                //            point2Ds[i].Transform(transform2D);
                //        }
                //    }
                //}

                systemJSAMObject = displaySystemConnection;
            }

            if (systemJSAMObject == null)
            {
                return null;
            }



            DisplaySystemSensor result = new DisplaySystemSensor(new SystemSensor(), point2Ds?.ToArray());
            SetReference(result, sensorArc.Reference());

            systemPlantRoom.Add(result);

            systemPlantRoom.Connect(result, systemJSAMObject as dynamic);
            systemPlantRoom.Connect(result, systemController);

            ComponentGroup componentGroup = controller.GetGroup();
            if (componentGroup != null)
            {
                AirSystemGroup airSystemGroup = systemPlantRoom.GetRelatedObjects<AirSystemGroup>(systemController).Find(x => x.Reference() == componentGroup.Reference());
                if (airSystemGroup != null)
                {
                    systemPlantRoom.Connect(airSystemGroup, result);
                }
            }

            return result;
        }
        
        private static ISystemSensor Connect(SystemPlantRoom systemPlantRoom, PlantSensorArc plantSensorArc)
        {
            if (systemPlantRoom == null || plantSensorArc == null)
            {
                return null;
            }

            PlantController plantController = plantSensorArc.GetController();
            if (plantController == null)
            {
                return null;
            }

            ISystemController systemController = systemPlantRoom.SystemController<ISystemController>(plantController.Reference());
            if (systemController == null)
            {
                return null;
            }

            SystemType systemType_Control = new SystemType(typeof(IControlSystem));

            List<Point2D> point2Ds = Query.Point2Ds(plantSensorArc);
            point2Ds = point2Ds == null ? new List<Point2D>() : point2Ds;

            Dictionary<int, Point2D> dictionary;

            dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemController as dynamic, systemType_Control, direction: Direction.In);
            if (dictionary == null || dictionary.Count == 0)
            {
                return null;
            }

            point2Ds.Add(dictionary.Values.First());

            ISystemJSAMObject systemJSAMObject = null;

            dynamic @dynamic = plantSensorArc.GetComponent();
            if (@dynamic != null)
            {
                string reference_2 = (@dynamic)?.GUID;
                systemJSAMObject = systemPlantRoom.Find<Core.Systems.ISystemComponent>(x => x.Reference() == reference_2);
                if (systemJSAMObject != null)
                {

                    dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemJSAMObject as dynamic, systemType_Control, direction: Direction.Out);
                    if(dictionary == null || dictionary.Count == 0)
                    {
                        dictionary = Geometry.Systems.Query.Point2DDictionary(systemPlantRoom, systemJSAMObject as dynamic, systemType_Control, direction: Direction.Undefined);
                    }

                    if (dictionary != null && dictionary.Count != 0)
                    {
                        point2Ds.Insert(0, dictionary.Values.First());
                    }
                }
            }

            if (systemJSAMObject == null)
            {
                @dynamic = plantSensorArc.GetPipe();
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
            SetReference(result, plantSensorArc.Reference());

            systemPlantRoom.Add(result);

            systemPlantRoom.Connect(result, systemJSAMObject as dynamic);
            systemPlantRoom.Connect(result, systemController);

            return result;
        }

        public static ISystemConnection Connect(this SystemPlantRoom systemPlantRoom, Core.Systems.ISystemComponent systemComponent_1, int connectionIndex_1, Core.Systems.ISystemComponent systemComponent_2, int connectionIndex_2, Core.Systems.ISystem system, Direction direction, IEnumerable<Point2D> point2Ds = null)
        {
            if(systemPlantRoom == null || systemComponent_1 == null || systemComponent_2 == null || system == null)
            {
                return null;
            }

            if (!Geometry.Systems.Query.TryGetIndexes(systemPlantRoom, systemComponent_1, connectionIndex_1, systemComponent_2, connectionIndex_2, out int index_1, out int index_2, new SystemType(system), direction))
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
                List<Point2D> point2Ds_Check_1 = new List<Point2D>();
                point2Ds_Check_1.Add(point2D_2);
                point2Ds_Check_1.AddRange(point2Ds_Temp);
                point2Ds_Check_1.Add(point2D_1);

                Polyline2D polyline2D_1 = new Polyline2D(point2Ds_Check_1);

                List<Point2D> point2Ds_Check_2 = new List<Point2D>();
                point2Ds_Check_2.Add(point2D_1);
                point2Ds_Check_2.AddRange(point2Ds_Temp);
                point2Ds_Check_2.Add(point2D_2);

                Polyline2D polyline2D_2 = new Polyline2D(point2Ds_Check_2);

                //Func<Polyline2D, bool> selfIntersect = new Func<Polyline2D, bool>(x =>
                //{
                //    List<Segment2D> segment2Ds = x?.Segment2Ds();
                //    if (segment2Ds == null || segment2Ds.Count == 0)
                //    {
                //        return false;
                //    }

                //    for (int i = 0; i < segment2Ds.Count; i++)
                //    {
                //        for (int j = i + 1; j < segment2Ds.Count - 1; j++)
                //        {
                //            Point2D point2D = segment2Ds[i].Intersection(segment2Ds[j]);
                //            if (point2D == null)
                //            {
                //                continue;
                //            }

                //            if (segment2Ds[i][0].AlmostSimilar(point2D) || segment2Ds[i][1].AlmostSimilar(point2D))
                //            {
                //                continue;
                //            }

                //            return true;
                //        }
                //    }

                //    return false;
                //});

                if (polyline2D_2.GetLength() < polyline2D_1.GetLength() && !polyline2D_2.SelfIntersect())
                {
                    point2Ds_Temp = polyline2D_2.Points;
                }
                else
                {
                    point2Ds_Temp = polyline2D_1.Points;
                }


                //point2Ds_Temp.Insert(0, point2D_2);
                //point2Ds_Temp.Add(point2D_1);

                //if (point2D_1.Distance(point2Ds_Temp.First()) + point2D_2.Distance(point2Ds_Temp.Last()) < point2D_1.Distance(point2Ds_Temp.Last()) + point2D_2.Distance(point2Ds_Temp.First()))
                //{
                //    point2Ds_Temp.Insert(0, point2D_1);
                //    point2Ds_Temp.Add(point2D_2);
                //}
                //else
                //{
                //    point2Ds_Temp.Insert(0, point2D_2);
                //    point2Ds_Temp.Add(point2D_1);
                //}
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
                            foreach(Duct duct_Temp in ducts)
                            {
                                List<Point2D> point2Ds_Temp = Query.Point2Ds(duct_Temp);
                                if(point2Ds_Temp != null)
                                {
                                    point2Ds.AddRange(point2Ds_Temp);
                                }

                            }
                        }

                        Direction direction = (systemComponent_In as dynamic).GetGroup()?.Guid == (componentGroup as dynamic).Guid ? Direction.In : Direction.Out;
                        Duct duct = direction == Direction.In ? (componentGroup as dynamic).GetOutputDuct(1, 1) : (componentGroup as dynamic).GetInputDuct(1, 1);

                        ISystemConnection systemConnection = Connect(systemPlantRoom, systemComponent_In_SAM, -1, systemComponent_Out_SAM, -1, airSystem, Direction.Out, point2Ds);
                        
                        if (systemConnection != null)
                        {
                            if(duct != null)
                            {
                                systemConnection.SetReference(Query.Reference(duct));
                            }

                            systemPlantRoom.Add(systemConnection);
                            result.Add(systemConnection);
                        }
                    }
                }

            }

            return result;
        }

    }
}