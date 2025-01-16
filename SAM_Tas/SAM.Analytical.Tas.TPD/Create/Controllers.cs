using TPD;
using System;
using System.Collections.Generic;
using SAM.Core.Systems;
using SAM.Analytical.Systems;
using SAM.Geometry.Systems;
using System.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static List<Controller> Controllers(this SystemPlantRoom systemPlantRoom, global::TPD.System system, AirSystem airSystem, Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponent, Dictionary<Guid, Duct> dictionary_Duct)
        {
            if (dictionary_SystemComponent == null || dictionary_SystemComponent.Count == 0)
            {
                return null;
            }

            List<Tuple<Controller, IDisplaySystemController>> tuples = new List<Tuple<Controller, IDisplaySystemController>>();

            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(airSystem);
            foreach(IDisplaySystemController displaySystemController in displaySystemControllers)
            {
                Controller controller = displaySystemController.ToTPD(system);
                if(controller == null)
                {
                    continue;
                }


                tuples.Add(new Tuple<Controller, IDisplaySystemController>(controller, displaySystemController));
            }

            foreach (Tuple<Controller, IDisplaySystemController> tuple in tuples)
            {
                List<IDisplaySystemController> displaySystemControllers_Connected = systemPlantRoom.GetRelatedObjects<IDisplaySystemController>(tuple.Item2);
                if (displaySystemControllers_Connected != null)
                {
                    foreach (IDisplaySystemController displaySystemController_Connected in displaySystemControllers_Connected)
                    {
                        Controller controller_Connected = tuples.Find(x => x.Item2.Guid == displaySystemController_Connected.Guid)?.Item1;

                        ControlArc controlArc = tuple.Item1.AddChainArc(controller_Connected);

                        List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(tuple.Item2, displaySystemController_Connected, new SystemType(airSystem));
                        if(systemConnections != null && systemConnections.Count > 0)
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

                if(dictionary_SystemComponent != null)
                {
                    List<Core.Systems.SystemComponent> systemComponents_Connected = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(tuple.Item2);
                    if (systemComponents_Connected != null)
                    {
                        foreach (Core.Systems.SystemComponent systemComponent_Connected in systemComponents_Connected)
                        {
                            if(!dictionary_SystemComponent.TryGetValue(systemComponent_Connected.Guid, out global::TPD.ISystemComponent systemComponent_TPD) || systemComponent_TPD == null)
                            {
                                continue;
                            }

                            ControlArc controlArc = tuple.Item1.AddControlArc(systemComponent_TPD as global::TPD.SystemComponent);

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
                            }
                        }
                    }
                }


                if(dictionary_Duct != null)
                {
                    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(tuple.Item2);
                    if (systemSensors_Connected != null)
                    {
                        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                        {
                            ISystemConnection systemConnection = systemPlantRoom.GetRelatedObjects<ISystemConnection>(systemSensor_Connected)?.FirstOrDefault();
                            if (systemConnection == null || !dictionary_Duct.TryGetValue(systemConnection.Guid, out Duct duct) || duct == null)
                            {
                                continue;
                            }

                            SensorArc sensorArc = tuple.Item1.AddSensorArc(duct);

                            if(systemSensor_Connected is DisplaySystemSensor)
                            {
                                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    sensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }
                        }
                    }
                }

            }

            return tuples.ConvertAll(x => x.Item1);
        }

    }
}
