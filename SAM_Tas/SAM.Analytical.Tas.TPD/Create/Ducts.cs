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
        public static List<Duct> Ducts(this SystemPlantRoom systemPlantRoom, global::TPD.System system, Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponents, out Dictionary<Guid, Duct> dictionary_Ducts)
        {
            dictionary_Ducts = null;

            if(dictionary_SystemComponents == null || dictionary_SystemComponents.Count == 0)
            {
                return null;
            }

            List<ISystemConnection> systemConnections = systemPlantRoom?.GetSystemConnections();
            if(systemConnections == null || systemConnections.Count == 0)
            {
                return null;
            }

            SystemType systemType = new SystemType(typeof(AirSystem));

            List<Duct> result = new List<Duct>();

            dictionary_Ducts = new Dictionary<Guid, Duct>();

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
                        if (!dictionary_SystemComponents.TryGetValue(systemComponent_Temp_1.Guid, out global::TPD.ISystemComponent systemComponent_1) || systemComponent_1 == null)
                        {
                            continue;
                        }

                        systemConnection.TryGetIndex(systemComponent_Temp_1, out int index_1);
                        Direction direction_1 = systemComponent_Temp_1.SystemConnectorManager.GetDirection(index_1);

                        int portIndex_1 = 1;
                        if (systemComponent_Temp_1 is SystemExchanger || systemComponent_Temp_1 is SystemEconomiser || systemComponent_Temp_1 is SystemDesiccantWheel)
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
                        if (!dictionary_SystemComponents.TryGetValue(systemComponent_Temp_2.Guid, out global::TPD.ISystemComponent systemComponent_2) || systemComponent_2 == null)
                        {
                            continue;
                        }

                        systemConnection.TryGetIndex(systemComponent_Temp_2, out int index_2);
                        Direction direction_2 = systemComponent_Temp_2.SystemConnectorManager.GetDirection(index_2);

                        int portIndex_2 = 1;
                        if (systemComponent_Temp_2 is SystemExchanger || systemComponent_Temp_2 is SystemEconomiser || systemComponent_Temp_2 is SystemDesiccantWheel)
                        {
                            if (systemComponent_Temp_2.SystemConnectorManager.TryGetSystemConnector(index_2, out SystemConnector systemConnector_2) && systemConnector_2 != null)
                            {
                                if (systemConnector_2.ConnectionIndex != -1)
                                {
                                    portIndex_2 = systemConnector_2.ConnectionIndex;
                                }
                            }
                        }

                        if ((systemComponent_2 as dynamic).GUID == (systemComponent_1 as dynamic).GUID)
                        {
                            continue;
                        }

                        if (direction_1 == Direction.In)
                        {
                            global::TPD.ISystemComponent systemComponent_Temp = systemComponent_1;
                            systemComponent_1 = systemComponent_2;
                            systemComponent_2 = systemComponent_Temp;

                            int portIndex = portIndex_1;
                            portIndex_1 = portIndex_2;
                            portIndex_2 = portIndex;
                        }

                        Duct duct = null;
                        try
                        {
                            duct = system.AddDuct((global::TPD.SystemComponent)systemComponent_1, portIndex_1, (global::TPD.SystemComponent)systemComponent_2, portIndex_2);
                        }
                        catch(Exception exception)
                        {
                            string message = exception.Message;

                            duct = null;
                        }

                        if (duct == null)
                        {
                            continue;
                        }

                        dictionary_Ducts[systemConnection.Guid] = duct;

                        result.Add(duct);

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

                                        duct.AddNode(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
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
