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
        public static List<PlantController> PlantControllers(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, LiquidSystem liquidSystem, Dictionary<Guid, PlantComponent> dictionary_PlantComponents, Dictionary<Guid, Pipe> dictionary_Pipes)
        {
            if (systemPlantRoom == null || plantRoom == null || dictionary_PlantComponents == null)
            {
                return null;
            }

            List<Tuple<PlantController, IDisplaySystemController>> tuples = new List<Tuple<PlantController, IDisplaySystemController>>();

            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(liquidSystem);
            foreach (IDisplaySystemController displaySystemController in displaySystemControllers)
            {
                PlantController plantController = displaySystemController.ToTPD(plantRoom);
                if (plantController == null)
                {
                    continue;
                }


                tuples.Add(new Tuple<PlantController, IDisplaySystemController>(plantController, displaySystemController));
            }

            foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
            {
                List<IDisplaySystemController> displaySystemControllers_Connected = systemPlantRoom.GetRelatedObjects<IDisplaySystemController>(tuple.Item2);
                if (displaySystemControllers_Connected != null)
                {
                    foreach (IDisplaySystemController displaySystemController_Connected in displaySystemControllers_Connected)
                    {
                        PlantController plantController_Connected = tuples.Find(x => x.Item2.Guid == displaySystemController_Connected.Guid)?.Item1;

                        PlantControlArc plantControlArc = tuple.Item1.AddChainArc(plantController_Connected);

                        List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(tuple.Item2, displaySystemController_Connected, new SystemType(liquidSystem));
                        if (systemConnections != null && systemConnections.Count > 0)
                        {
                            DisplaySystemConnection displaySystemConnection = systemConnections.Find(x => x is DisplaySystemConnection) as DisplaySystemConnection;
                            if (displaySystemConnection != null)
                            {
                                SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    plantControlArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }
                        }
                    }
                }

                //if (dictionary_PlantComponents != null)
                //{
                //    List<Core.Systems.SystemComponent> systemComponents_Connected = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(tuple.Item2);
                //    if (systemComponents_Connected != null)
                //    {
                //        foreach (Core.Systems.SystemComponent systemComponent_Connected in systemComponents_Connected)
                //        {
                //            if (!dictionary_PlantComponents.TryGetValue(systemComponent_Connected.Guid, out PlantComponent plantComponent_TPD) || plantComponent_TPD == null)
                //            {
                //                continue;
                //            }

                //            PlantControlArc plantControlArc = tuple.Item1.AddControlArc(plantComponent_TPD);

                //            List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(tuple.Item2, systemComponent_Connected, new SystemType(liquidSystem));
                //            if (systemConnections != null && systemConnections.Count > 0)
                //            {
                //                DisplaySystemConnection displaySystemConnection = systemConnections.Find(x => x is DisplaySystemConnection) as DisplaySystemConnection;
                //                if (displaySystemConnection != null)
                //                {
                //                    SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                //                    List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                //                    for (int i = 1; i < point2Ds.Count - 1; i++)
                //                    {
                //                        plantControlArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}


                //if (dictionary_Pipes != null)
                //{
                //    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(tuple.Item2);
                //    if (systemSensors_Connected != null)
                //    {
                //        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                //        {
                //            ISystemConnection systemConnection = systemPlantRoom.GetRelatedObjects<ISystemConnection>(systemSensor_Connected)?.FirstOrDefault();
                //            if (systemConnection == null || !dictionary_Pipes.TryGetValue(systemConnection.Guid, out Pipe pipe) || pipe == null)
                //            {
                //                continue;
                //            }

                //            PlantSensorArc plantSensorArc = tuple.Item1.AddSensorArc(pipe);

                //            if (systemSensor_Connected is DisplaySystemSensor)
                //            {
                //                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                //                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                //                for (int i = 1; i < point2Ds.Count - 1; i++)
                //                {
                //                    plantSensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                //                }
                //            }
                //        }
                //    }
                //}

            }

            return tuples.ConvertAll(x => x.Item1);
        }

    }
}
