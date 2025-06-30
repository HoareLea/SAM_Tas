using TPD;
using System;
using System.Collections.Generic;
using SAM.Core.Systems;
using SAM.Analytical.Systems;
using SAM.Geometry.Systems;
using System.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static Dictionary<Guid, Controller> Controllers(this SystemPlantRoom systemPlantRoom, global::TPD.System system, AirSystem airSystem, Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent, Dictionary<Guid, Duct> dictionary_Duct, bool inludeGroupedControllers = true)
        {
            if (systemPlantRoom == null || airSystem == null || dictionary_SystemComponent == null)
            {
                return null;
            }

            List<Tuple<Controller, IDisplaySystemController>> tuples = new List<Tuple<Controller, IDisplaySystemController>>();

            List<Tuple<Guid, int, IDisplaySystemController>> tuples_GroupIndex = new List<Tuple<Guid, int, IDisplaySystemController>>();

            #region Create all Controllers
            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(airSystem);
            if (displaySystemControllers != null && displaySystemControllers.Count != 0)
            {
                Dictionary<Guid, HashSet<int>> dictionary_AirSystemGroup = new Dictionary<Guid, HashSet<int>>();

                foreach (IDisplaySystemController displaySystemController in displaySystemControllers)
                {
                    if (!inludeGroupedControllers)
                    {
                        AirSystemGroup airSystemGroup = systemPlantRoom.GetRelatedObjects<AirSystemGroup>(displaySystemController)?.FirstOrDefault();
                        if (airSystemGroup != null)
                        {
                            if (!dictionary_AirSystemGroup.TryGetValue(airSystemGroup.Guid, out HashSet<int> groupIndexes))
                            {
                                groupIndexes = new HashSet<int>();
                                dictionary_AirSystemGroup[airSystemGroup.Guid] = groupIndexes;
                            }

                            if (((SystemController)displaySystemController).TryGetValue(SAM.Analytical.Systems.SystemControllerParameter.GroupIndex, out int groupIndex))
                            {
                                tuples_GroupIndex.Add(new Tuple<Guid, int, IDisplaySystemController>(airSystemGroup.Guid, groupIndex, displaySystemController));

                                if (groupIndexes.Contains(groupIndex))
                                {
                                    continue;
                                }
                                else
                                {
                                    groupIndexes.Add(groupIndex);
                                }
                            }
                        }
                    }

                    Controller controller = displaySystemController.ToTPD(system);
                    if (controller == null)
                    {
                        continue;
                    }

                    tuples.Add(new Tuple<Controller, IDisplaySystemController>(controller, displaySystemController));
                }
            }
            #endregion

            if (tuples == null || tuples.Count == 0)
            {
                return null;
            }

            #region Create Controller to Controller connection

            List<Tuple<Guid, Guid>> tuples_Controllers = new List<Tuple<Guid, Guid>>();

            Func<IDisplaySystemController, Controller> getController = new Func<IDisplaySystemController, Controller>(displaySystemController =>
            {
                Tuple<Guid, int, IDisplaySystemController> tuple_Temp = tuples_GroupIndex?.Find(x => x.Item3.Guid == displaySystemController.Guid);
                if (tuple_Temp == null)
                {
                    return null;
                }

                List<Tuple<Guid, int, IDisplaySystemController>> tuples_GroupIndex_Temp = tuples_GroupIndex?.FindAll(x => x.Item1 == tuple_Temp.Item1 && x.Item2 == tuple_Temp.Item2);
                foreach (Tuple<Guid, int, IDisplaySystemController> tuple_GroupIndex_Temp in tuples_GroupIndex_Temp)
                {
                    Controller controller = tuples.Find(x => x.Item2.Guid == tuple_GroupIndex_Temp.Item3.Guid)?.Item1;
                    if (controller != null)
                    {
                        return controller;
                    }
                }

                return null;
            });

            foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
            {
                IDisplaySystemController displaySystemController = tuple.Item2;

                if (!(displaySystemController is SystemNormalController))
                {
                    List<IDisplaySystemController> displaySystemControllers_Connected = systemPlantRoom.GetRelatedObjects<IDisplaySystemController>(displaySystemController);
                    if (displaySystemControllers_Connected != null)
                    {
                        foreach (IDisplaySystemController displaySystemController_Connected in displaySystemControllers_Connected)
                        {
                            Controller controller_Connected = tuples.Find(x => x.Item2.Guid == displaySystemController_Connected.Guid)?.Item1;
                            if (controller_Connected == null)
                            {
                                controller_Connected = getController.Invoke(displaySystemController_Connected);
                            }

                            if (controller_Connected == null)
                            {
                                continue;
                            }

                            if (tuples_Controllers.Find(x => (x.Item1 == displaySystemController.Guid && x.Item2 == displaySystemController_Connected.Guid) || (x.Item2 == displaySystemController.Guid && x.Item1 == displaySystemController_Connected.Guid)) != null)
                            {
                                continue;
                            }

                            tuples_Controllers.Add(new Tuple<Guid, Guid>(displaySystemController.Guid, displaySystemController_Connected.Guid));

                            ControlArc controlArc = tuple.Item1.AddChainArc(controller_Connected);

                            List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(displaySystemController, displaySystemController_Connected, new SystemType(airSystem));
                            if (systemConnections != null && systemConnections.Count > 0)
                            {
                                DisplaySystemConnection displaySystemConnection = systemConnections.Find(x => x is DisplaySystemConnection) as DisplaySystemConnection;
                                if (displaySystemConnection != null)
                                {
                                    SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                                    List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                    for (int i = 1; i < point2Ds.Count - 1; i++)
                                    {
                                        controlArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create Controller to SystemComponent connection
            if (dictionary_SystemComponent != null)
            {
                foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<Core.Systems.SystemComponent> systemComponents_Connected = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(displaySystemController);
                    if (systemComponents_Connected != null)
                    {
                        foreach (Core.Systems.SystemComponent systemComponent_Connected in systemComponents_Connected)
                        {
                            if (!dictionary_SystemComponent.TryGetValue(systemComponent_Connected.Guid, out global::TPD.ISystemComponent systemComponent_TPD) || systemComponent_TPD == null)
                            {
                                continue;
                            }

                            ControlArc controlArc = tuple.Item1.AddControlArc((global::TPD.SystemComponent)systemComponent_TPD);

                            List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(tuple.Item2, systemComponent_Connected, new SystemType(airSystem));
                            if (systemConnections != null && systemConnections.Count > 0)
                            {
                                DisplaySystemConnection displaySystemConnection = systemConnections.Find(x => x is DisplaySystemConnection) as DisplaySystemConnection;
                                if (displaySystemConnection != null)
                                {
                                    SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                                    List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                    for (int i = 1; i < point2Ds.Count - 1; i++)
                                    {
                                        controlArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                    }
                                }

                                if (systemComponent_Connected is SystemDXCoil)
                                {
                                    ISystemConnection systemConnection = displaySystemConnection == null ? systemConnections[0] : displaySystemConnection;
                                    if (systemConnection != null && systemConnection.TryGetIndex(new Core.ObjectReference(systemComponent_Connected), out int index))
                                    {
                                        controlArc.ControlPort = index - 2;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create Controller to Duct sensor connection
            if (dictionary_Duct != null)
            {
                foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(displaySystemController);
                    if (systemSensors_Connected != null)
                    {

                        Dictionary<string, SensorArc> dictionary = new Dictionary<string, SensorArc>();
                        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                        {
                            SensorArc sensorArc = null;

                            ISystemConnection systemConnection = systemPlantRoom.GetRelatedObjects<ISystemConnection>(systemSensor_Connected)?.FirstOrDefault();
                            if (systemConnection != null)
                            {
                                if (!dictionary_Duct.TryGetValue(systemConnection.Guid, out Duct duct) || duct == null)
                                {
                                    continue;
                                }

                                sensorArc = tuple.Item1.AddSensorArc(duct);
                            }

                            if (sensorArc == null)
                            {
                                continue;
                            }

                            if (systemSensor_Connected is DisplaySystemSensor)
                            {
                                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    sensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }

                            dictionary[systemSensor_Connected.Guid.ToString()] = sensorArc;
                        }

                        if (dictionary != null && dictionary.Count != 0)
                        {
                            string reference;
                            SensorArc sensorArc_Temp;

                            reference = null;

                            if (displaySystemController is SystemNormalController)
                            {
                                reference = ((SystemNormalController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemOutdoorController)
                            {
                                reference = ((SystemOutdoorController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemDifferenceController)
                            {
                                reference = ((SystemDifferenceController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemPassthroughController)
                            {
                                reference = ((SystemPassthroughController)displaySystemController).SensorReference;
                            }

                            if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out sensorArc_Temp))
                            {
                                tuple.Item1.SensorArc1 = sensorArc_Temp;
                            }

                            reference = null;

                            if (displaySystemController is SystemDifferenceController)
                            {
                                reference = ((SystemDifferenceController)displaySystemController).SecondarySensorReference;
                                if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out sensorArc_Temp))
                                {
                                    tuple.Item1.SensorArc2 = sensorArc_Temp;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create Controller to Component sensor connection
            if (dictionary_Duct != null)
            {
                Func<Core.Systems.SystemComponent, global::TPD.ISystemComponent> getSystemComponent = new Func<Core.Systems.SystemComponent, global::TPD.ISystemComponent>(systemComponent =>
                {
                    AirSystemGroup airSystemGroup = systemPlantRoom.GetRelatedObjects<AirSystemGroup>(systemComponent)?.FirstOrDefault();
                    if (airSystemGroup == null)
                    {
                        return null;
                    }

                    if (!systemComponent.TryGetValue(Systems.SystemControllerParameter.GroupIndex, out int groupIndex))
                    {
                        groupIndex = -1;
                    }

                    List<Core.Systems.SystemComponent> systemComponents = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(airSystemGroup);
                    if (systemComponents == null || systemComponents.Count == 0)
                    {
                        return null;
                    }

                    foreach (Core.Systems.SystemComponent systemComponent_Temp in systemComponents)
                    {
                        if (systemComponent_Temp == null)
                        {
                            continue;
                        }

                        if (!systemComponent_Temp.TryGetValue(Systems.SystemControllerParameter.GroupIndex, out int groupIndex_Temp))
                        {
                            if (!systemComponent_Temp.GetType().IsAssignableFrom(systemComponent.GetType()) && !systemComponent.GetType().IsAssignableFrom(systemComponent_Temp.GetType()))
                            {
                                continue;
                            }

                            groupIndex_Temp = -1;
                        }

                        if (groupIndex != groupIndex_Temp)
                        {
                            continue;
                        }

                        if (dictionary_SystemComponent.TryGetValue(systemComponent_Temp.Guid, out global::TPD.ISystemComponent systemComponent_TPD) && systemComponent_TPD != null)
                        {
                            return systemComponent_TPD;
                        }

                    }

                    return null;
                });

                foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(displaySystemController);
                    if (systemSensors_Connected != null)
                    {

                        Dictionary<string, SensorArc> dictionary = new Dictionary<string, SensorArc>();
                        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                        {
                            List<Core.Systems.SystemComponent> systemComponents = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(systemSensor_Connected);
                            if (systemComponents == null || systemComponents.Count == 0)
                            {
                                continue;
                            }

                            SensorArc sensorArc = null;

                            foreach (Core.Systems.SystemComponent systemComponent in systemComponents)
                            {
                                if (!dictionary_SystemComponent.TryGetValue(systemComponent.Guid, out global::TPD.ISystemComponent systemComponent_TPD) || systemComponent_TPD == null)
                                {
                                    systemComponent_TPD = getSystemComponent.Invoke(systemComponent);
                                }

                                if (systemComponent_TPD == null)
                                {
                                    continue;
                                }

                                int count = ((dynamic)systemComponent_TPD).GetSensorPortCount();
                                if (count < 1)
                                {
                                    continue;
                                }

                                sensorArc = tuple.Item1.AddSensorArcToComponent((global::TPD.SystemComponent)systemComponent_TPD, 1);
                                break;
                            }

                            if (sensorArc == null)
                            {
                                continue;
                            }

                            if (systemSensor_Connected is DisplaySystemSensor)
                            {
                                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    sensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }

                            dictionary[systemSensor_Connected.Guid.ToString()] = sensorArc;
                        }

                        if (dictionary != null && dictionary.Count != 0)
                        {
                            string reference;
                            SensorArc sensorArc_Temp;

                            reference = null;

                            if (displaySystemController is SystemNormalController)
                            {
                                reference = ((SystemNormalController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemOutdoorController)
                            {
                                reference = ((SystemOutdoorController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemDifferenceController)
                            {
                                reference = ((SystemDifferenceController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemPassthroughController)
                            {
                                reference = ((SystemPassthroughController)displaySystemController).SensorReference;
                            }

                            if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out sensorArc_Temp))
                            {
                                tuple.Item1.SensorArc1 = sensorArc_Temp;
                            }

                            reference = null;

                            if (displaySystemController is SystemDifferenceController)
                            {
                                reference = ((SystemDifferenceController)displaySystemController).SecondarySensorReference;
                                if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out sensorArc_Temp))
                                {
                                    tuple.Item1.SensorArc2 = sensorArc_Temp;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Assign controller type
            foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
            {
                IDisplaySystemController displaySystemController = tuple.Item2;
                Controller controller = tuple.Item1;

                controller?.SetControlType(displaySystemController);
            }
            #endregion

            Dictionary<Guid, Controller> result = new Dictionary<Guid, Controller>();
            foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
            {
                result[tuple.Item2.Guid] = tuple.Item1;
            }

            return result;
        }

    }
}
