using TPD;
using SAM.Core;
using System;
using System.Collections.Generic;
using SAM.Analytical.Systems;
using SAM.Core.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static List<Pipe> Pipes(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, Dictionary<Guid, PlantComponent> dictionary_PlantComponents, out Dictionary<Guid, Pipe> dictionary_Pipes)
        {
            dictionary_Pipes = null;

            if (dictionary_PlantComponents == null || dictionary_PlantComponents.Count == 0)
            {
                return null;
            }

            List<ISystemConnection> systemConnections = systemPlantRoom?.GetSystemConnections();
            if(systemConnections == null || systemConnections.Count == 0)
            {
                return null;
            }

            SystemType systemType = new SystemType(typeof(LiquidSystem));

            List<Pipe> result = new List<Pipe>();

            dictionary_Pipes = new Dictionary<Guid, Pipe>();

            foreach (ISystemConnection systemConnection in systemConnections)
            {
                if (systemConnection.SystemType != systemType)
                {
                    continue;
                }

                List<Core.Systems.SystemComponent> systemComponents_SystemConnection = systemPlantRoom?.GetRelatedObjects<Core.Systems.SystemComponent>(systemConnection);
                if (systemComponents_SystemConnection == null)
                {
                    continue;
                }

                for (int i = 0; i < systemComponents_SystemConnection.Count - 1; i++)
                {
                    for (int j = i + 1; j < systemComponents_SystemConnection.Count; j++)
                    {
                        Core.Systems.SystemComponent systemComponent_Temp_1 = systemComponents_SystemConnection[i];
                        if (!dictionary_PlantComponents.TryGetValue(systemComponent_Temp_1.Guid, out PlantComponent plantComponent_1) || plantComponent_1 == null)
                        {
                            continue;
                        }

                        systemConnection.TryGetIndex(systemComponent_Temp_1, out int index_1);
                        Direction direction_1 = systemComponent_Temp_1.SystemConnectorManager.GetDirection(index_1);

                        int portIndex_1 = 1;
                        HashSet<int> connectionIndexes_1 = systemComponent_Temp_1.SystemConnectorManager.GetConnectionIndexes(systemType);
                        if (connectionIndexes_1.Count > 1)
                        {
                            if (systemComponent_Temp_1.SystemConnectorManager.TryGetSystemConnector(index_1, out SystemConnector systemConnector_1) && systemConnector_1 != null)
                            {
                                if (systemConnector_1.ConnectionIndex != -1)
                                {
                                    portIndex_1 = systemConnector_1.ConnectionIndex;
                                }
                            }
                        }

                        Core.Systems.SystemComponent systemComponent_Temp_2 = systemComponents_SystemConnection[j];
                        if (!dictionary_PlantComponents.TryGetValue(systemComponent_Temp_2.Guid, out PlantComponent plantComponent_2) || plantComponent_2 == null)
                        {
                            continue;
                        }

                        if ((plantComponent_2 as dynamic).GUID == (plantComponent_1 as dynamic).GUID)
                        {
                            continue;
                        }

                        systemConnection.TryGetIndex(systemComponent_Temp_2, out int index_2);
                        Direction direction_2 = systemComponent_Temp_2.SystemConnectorManager.GetDirection(index_2);

                        int portIndex_2 = 1;
                        HashSet<int> connectionIndexes_2 = systemComponent_Temp_2.SystemConnectorManager.GetConnectionIndexes(systemType);
                        if(connectionIndexes_2.Count > 1)
                        {
                            if (systemComponent_Temp_2.SystemConnectorManager.TryGetSystemConnector(index_2, out SystemConnector systemConnector_2) && systemConnector_2 != null)
                            {
                                if (systemConnector_2.ConnectionIndex != -1)
                                {
                                    portIndex_2 = systemConnector_2.ConnectionIndex;
                                }
                            }
                        }

                        if (direction_1 == Direction.In)
                        {
                            PlantComponent plantComponent_Temp = plantComponent_1;
                            plantComponent_1 = plantComponent_2;
                            plantComponent_2 = plantComponent_Temp;

                            int portIndex = portIndex_1;
                            portIndex_1 = portIndex_2;
                            portIndex_2 = portIndex;
                        }

                        Pipe pipe = null;
                        try
                        {
                            pipe = plantRoom.AddPipe(plantComponent_1, portIndex_1, plantComponent_2, portIndex_2);
                        }
                        catch(Exception exception)
                        {
                            string message = exception.Message;

                            pipe = null;
                        }

                        if (pipe == null)
                        {
                            continue;
                        }

                        dictionary_Pipes[systemConnection.Guid] = pipe;

                        result.Add(pipe);

                        if (systemConnection is DisplaySystemConnection)
                        {
                            DisplaySystemConnection displaySystemConnection = (DisplaySystemConnection)systemConnection;
                            SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                            if (systemPolyline != null)
                            {
                                List<Point2D> point2Ds = systemPolyline.Points;
                                if (point2Ds != null && point2Ds.Count > 2)
                                {
                                    for (int k = 1; k < point2Ds.Count - 1; k++)
                                    {
                                        Point2D point2D = point2Ds[k].ToTPD();

                                        pipe.AddNode(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
                                    }
                                }
                            }

                        }
                    }
                }
            }

            return result;
        }

    }
}
