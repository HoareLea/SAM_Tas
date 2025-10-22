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
        public static Dictionary<Guid, PlantController> PlantControllers(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, LiquidSystem liquidSystem, Dictionary<Guid, PlantComponent> dictionary_PlantComponents, Dictionary<Guid, Pipe> dictionary_Pipes)
        {
            if (systemPlantRoom == null || plantRoom == null || dictionary_PlantComponents == null)
            {
                return null;
            }

            List<Tuple<PlantController, IDisplaySystemController>> tuples = new List<Tuple<PlantController, IDisplaySystemController>>();

            #region Create all PlantControllers
            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(liquidSystem);
            if(displaySystemControllers != null && displaySystemControllers.Count != 0)
            {
                foreach (IDisplaySystemController displaySystemController in displaySystemControllers)
                {
                    PlantController plantController = displaySystemController.ToTPD(plantRoom);
                    if (plantController == null)
                    {
                        continue;
                    }

                    tuples.Add(new Tuple<PlantController, IDisplaySystemController>(plantController, displaySystemController));
                }
            }
            #endregion

            if (tuples == null || tuples.Count == 0)
            {
                return null;
            }

            #region Create PlantController to PlantController connection

            List<Tuple<Guid, Guid>> tuples_Controllers = new List<Tuple<Guid, Guid>>();

            foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
            {
                IDisplaySystemController displaySystemController = tuple.Item2;

                if (!(displaySystemController is SystemLiquidNormalController))
                {
                    List<IDisplaySystemController> displaySystemControllers_Connected = systemPlantRoom.GetRelatedObjects<IDisplaySystemController>(displaySystemController);
                    if (displaySystemControllers_Connected != null)
                    {
                        foreach (IDisplaySystemController displaySystemController_Connected in displaySystemControllers_Connected)
                        {
                            PlantController plantController_Connected = tuples.Find(x => x.Item2.Guid == displaySystemController_Connected.Guid)?.Item1;
                            if (plantController_Connected == null)
                            {
                                continue;
                            }

                            if (tuples_Controllers.Find(x => (x.Item1 == displaySystemController.Guid && x.Item2 == displaySystemController_Connected.Guid) || (x.Item2 == displaySystemController.Guid && x.Item1 == displaySystemController_Connected.Guid)) != null)
                            {
                                continue;
                            }

                            tuples_Controllers.Add(new Tuple<Guid, Guid>(displaySystemController.Guid, displaySystemController_Connected.Guid));

                            PlantControlArc plantControlArc = tuple.Item1.AddChainArc(plantController_Connected);

                            List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(displaySystemController, displaySystemController_Connected, new SystemType(liquidSystem));
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
                }
            }
            #endregion

            #region Create PlantController to PlantComponent connection
            if (dictionary_PlantComponents != null)
            {
                foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<Core.Systems.SystemComponent> systemComponents_Connected = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(displaySystemController);
                    if (systemComponents_Connected != null)
                    {
                        foreach (Core.Systems.SystemComponent systemComponent_Connected in systemComponents_Connected)
                        {
                            if (!dictionary_PlantComponents.TryGetValue(systemComponent_Connected.Guid, out PlantComponent plantComponent_TPD) || plantComponent_TPD == null)
                            {
                                continue;
                            }

                            PlantControlArc plantControlArc = tuple.Item1.AddControlArc(plantComponent_TPD);

                            List<ISystemConnection> systemConnections = systemPlantRoom.GetSystemConnections(tuple.Item2, systemComponent_Connected, new SystemType(liquidSystem));
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

                                if (systemComponent_Connected is SystemWaterToWaterHeatPump)
                                {
                                    ISystemConnection systemConnection = displaySystemConnection == null ? systemConnections[0] : displaySystemConnection;
                                    if (systemConnection != null && systemConnection.TryGetIndex(new Core.ObjectReference(systemComponent_Connected), out int index))
                                    {
                                        plantControlArc.ControlPort = index - 4;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create PlantController to Pipe sensor connection
            if (dictionary_Pipes != null)
            {
                foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(displaySystemController);
                    if (systemSensors_Connected != null)
                    {
                        Dictionary<string, PlantSensorArc> dictionary = new Dictionary<string, PlantSensorArc>();
                        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                        {
                            PlantSensorArc plantSensorArc = null;

                            ISystemConnection systemConnection = systemPlantRoom.GetRelatedObjects<ISystemConnection>(systemSensor_Connected)?.FirstOrDefault();
                            if (systemConnection != null)
                            {
                                if (!dictionary_Pipes.TryGetValue(systemConnection.Guid, out Pipe pipe) || pipe == null)
                                {
                                    continue;
                                }

                                plantSensorArc = tuple.Item1.AddSensorArc(pipe);
                            }

                            if(plantSensorArc == null)
                            {
                                continue;
                            }

                            if (systemSensor_Connected is DisplaySystemSensor)
                            {
                                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    plantSensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }

                            dictionary[systemSensor_Connected.Guid.ToString()] = plantSensorArc;
                        }

                        if (dictionary != null && dictionary.Count != 0)
                        {
                            string reference;
                            PlantSensorArc plantSensorArc_Temp;

                            reference = null;

                            if (displaySystemController is SystemLiquidNormalController)
                            {
                                reference = ((SystemLiquidNormalController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemOutdoorController)
                            {
                                reference = ((SystemOutdoorController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemLiquidDifferenceController)
                            {
                                reference = ((SystemLiquidDifferenceController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemLiquidPassthroughController)
                            {
                                reference = ((SystemLiquidPassthroughController)displaySystemController).SensorReference;
                            }

                            if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out plantSensorArc_Temp))
                            {
                                tuple.Item1.SensorArc1 = plantSensorArc_Temp;
                            }

                            reference = null;

                            if (displaySystemController is SystemLiquidDifferenceController)
                            {
                                reference = ((SystemLiquidDifferenceController)displaySystemController).SecondarySensorReference;
                                if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out plantSensorArc_Temp))
                                {
                                    tuple.Item1.SensorArc2 = plantSensorArc_Temp;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Create PlantController to PlantComponent sensor connection
            if (dictionary_Pipes != null)
            {
                foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
                {
                    IDisplaySystemController displaySystemController = tuple.Item2;

                    List<ISystemSensor> systemSensors_Connected = systemPlantRoom.GetRelatedObjects<ISystemSensor>(displaySystemController);
                    if (systemSensors_Connected != null)
                    {
                        Dictionary<string, PlantSensorArc> dictionary = new Dictionary<string, PlantSensorArc>();
                        foreach (ISystemSensor systemSensor_Connected in systemSensors_Connected)
                        {
                            List<Core.Systems.SystemComponent> systemComponents = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(systemSensor_Connected);
                            if (systemComponents == null || systemComponents.Count == 0)
                            {
                                continue;
                            }

                            PlantSensorArc plantSensorArc = null;

                            foreach (Core.Systems.SystemComponent systemComponent in systemComponents)
                            {
                                if (!dictionary_PlantComponents.TryGetValue(systemComponent.Guid, out PlantComponent plantComponent) || plantComponent == null)
                                {
                                    continue;
                                }

                                int count = ((dynamic)plantComponent).GetSensorPortCount();
                                if (count < 1)
                                {
                                    continue;
                                }

                                plantSensorArc = tuple.Item1.AddSensorArcToComponent(plantComponent, 1);
                                break;
                            }

                            if (plantSensorArc == null)
                            {
                                continue;
                            }

                            if (systemSensor_Connected is DisplaySystemSensor)
                            {
                                SystemPolyline systemPolyline = ((DisplaySystemSensor)systemSensor_Connected).SystemGeometry;
                                List<Geometry.Planar.Point2D> point2Ds = systemPolyline.GetPoints().ConvertAll(x => x.ToTPD());
                                for (int i = 1; i < point2Ds.Count - 1; i++)
                                {
                                    plantSensorArc.AddNode(System.Convert.ToInt32(point2Ds[i].X), System.Convert.ToInt32(point2Ds[i].Y));
                                }
                            }

                            dictionary[systemSensor_Connected.Guid.ToString()] = plantSensorArc;
                        }

                        if (dictionary != null && dictionary.Count != 0)
                        {
                            string reference;
                            PlantSensorArc plantSensorArc_Temp;

                            reference = null;

                            if (displaySystemController is SystemLiquidNormalController)
                            {
                                reference = ((SystemLiquidNormalController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemOutdoorController)
                            {
                                reference = ((SystemOutdoorController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemLiquidDifferenceController)
                            {
                                reference = ((SystemLiquidDifferenceController)displaySystemController).SensorReference;
                            }
                            else if (displaySystemController is SystemLiquidPassthroughController)
                            {
                                reference = ((SystemLiquidPassthroughController)displaySystemController).SensorReference;
                            }

                            if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out plantSensorArc_Temp))
                            {
                                tuple.Item1.SensorArc1 = plantSensorArc_Temp;
                            }

                            reference = null;

                            if (displaySystemController is SystemLiquidDifferenceController)
                            {
                                reference = ((SystemLiquidDifferenceController)displaySystemController).SecondarySensorReference;
                                if (!string.IsNullOrWhiteSpace(reference) && dictionary.TryGetValue(reference, out plantSensorArc_Temp))
                                {
                                    tuple.Item1.SensorArc2 = plantSensorArc_Temp;
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Assign controller type
            foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
            {
                IDisplaySystemController displaySystemController = tuple.Item2;
                PlantController plantController = tuple.Item1;

                if (displaySystemController is SystemOutdoorController)
                {
                    plantController.ControlType = tpdControlType.tpdControlOutdoor;
                }
                else if (displaySystemController is SystemLiquidDifferenceController)
                {
                    plantController.ControlType = tpdControlType.tpdControlDifference;
                }
                else if (displaySystemController is SystemMaxLogicalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlMax;
                }
                else if (displaySystemController is SystemMinLogicalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlMin;
                }
                else if (displaySystemController is SystemNotLogicalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlNot;
                }
                else if (displaySystemController is SystemSigLogicalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlSig;
                }
                else if (displaySystemController is SystemIfLogicalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlIf;
                }
                else if (displaySystemController is SystemLiquidPassthroughController)
                {
                    plantController.ControlType = tpdControlType.tpdControlPassThrough;
                }
                else if (displaySystemController is SystemLiquidNormalController)
                {
                    plantController.ControlType = tpdControlType.tpdControlNormal;
                }
            }
            #endregion

            Dictionary<Guid, PlantController> result = new Dictionary<Guid, PlantController>();
            foreach (Tuple<PlantController, IDisplaySystemController> tuple in tuples)
            {
                result[tuple.Item2.Guid] = tuple.Item1;
            }

            return result;
        }
    }
}
