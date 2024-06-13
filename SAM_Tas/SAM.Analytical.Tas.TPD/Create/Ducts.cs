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
        public static List<Duct> Ducts(this SystemPlantRoom systemPlantRoom, global::TPD.System system, Dictionary<Guid, global::TPD.ISystemComponent> dictionary)
        {
            if(dictionary == null || dictionary.Count == 0)
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
                    Core.Systems.SystemComponent systemComponent_Temp_1 = systemComponents_SystemConnection[i];

                    if (!dictionary.TryGetValue(systemComponent_Temp_1.Guid, out global::TPD.ISystemComponent systemComponent_1) || systemComponent_1 == null)
                    {
                        continue;
                    }

                    systemConnection.TryGetIndex(systemComponent_Temp_1, out int index_1);
                    Direction direction_1 = systemComponent_Temp_1.SystemConnectorManager.GetDirection(index_1);

                    for (int j = i + 1; j < systemComponents_SystemConnection.Count; j++)
                    {
                        Core.Systems.SystemComponent systemComponent_Temp_2 = systemComponents_SystemConnection[j];

                        if (!dictionary.TryGetValue(systemComponent_Temp_2.Guid, out global::TPD.ISystemComponent systemComponent_2) || systemComponent_2 == null)
                        {
                            continue;
                        }

                        if((systemComponent_2 as dynamic).GUID == (systemComponent_1 as dynamic).GUID)
                        {
                            continue;
                        }

                        systemConnection.TryGetIndex(systemComponent_Temp_2, out int index_2);
                        Direction direction_2 = systemComponent_Temp_2.SystemConnectorManager.GetDirection(index_2);

                        int portIndex_1 = 1;
                        if (systemComponent_Temp_1 is SystemExchanger || systemComponent_Temp_1 is SystemEconomiser)
                        {
                            if (systemComponent_Temp_1.SystemConnectorManager.TryGetSystemConnector(index_1, out SystemConnector systemConnector_1) && systemConnector_1 != null)
                            {
                                if (systemConnector_1.ConnectionIndex != -1)
                                {
                                    portIndex_1 = systemConnector_1.ConnectionIndex;
                                }
                            }
                        }

                        int portIndex_2 = 1;
                        if (systemComponent_Temp_2 is SystemExchanger || systemComponent_Temp_2 is SystemEconomiser)
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
                            global::TPD.ISystemComponent systemComponent_Temp = systemComponent_1;
                            systemComponent_1 = systemComponent_2;
                            systemComponent_2 = systemComponent_Temp;

                            int portIndex = portIndex_1;
                            portIndex_1 = portIndex_2;
                            portIndex_2 = portIndex;
                        }

                        //------ TO BE REMOVED - START --------

                        int inputPortCount = -1;
                        int outputPortCount = -1;

                        Dictionary<int, int> dictionary_Input_1 = new Dictionary<int, int>();

                        inputPortCount = (systemComponent_1 as dynamic).GetInputPortCount();
                        for (int k = 1; k <= inputPortCount; k++)
                        {
                            dictionary_Input_1[k] = (systemComponent_1 as dynamic).GetInputDuctCount(k);
                        }

                        Dictionary<int, int> dictionary_Output_1 = new Dictionary<int, int>();

                        outputPortCount = (systemComponent_1 as dynamic).GetOutputPortCount();
                        for (int k = 1; k <= outputPortCount; k++)
                        {
                            dictionary_Output_1[k] = (systemComponent_1 as dynamic).GetOutputDuctCount(k);
                        }

                        Dictionary<int, int> dictionary_Input_2 = new Dictionary<int, int>();

                        inputPortCount = (systemComponent_2 as dynamic).GetInputPortCount();
                        for (int k = 1; k <= inputPortCount; k++)
                        {
                            dictionary_Input_2[k] = (systemComponent_2 as dynamic).GetInputDuctCount(k);
                        }

                        Dictionary<int, int> dictionary_Output_2 = new Dictionary<int, int>();

                        outputPortCount = (systemComponent_2 as dynamic).GetOutputPortCount();
                        for (int k = 1; k <= outputPortCount; k++)
                        {
                            dictionary_Output_2[k] = (systemComponent_2 as dynamic).GetOutputDuctCount(k);
                        }

                        //------ TO BE REMOVED - END --------

                        Duct duct = system.AddDuct((global::TPD.SystemComponent)systemComponent_1, portIndex_1, (global::TPD.SystemComponent)systemComponent_2, portIndex_2);
                        if (duct == null)
                        {
                            continue;
                        }

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
