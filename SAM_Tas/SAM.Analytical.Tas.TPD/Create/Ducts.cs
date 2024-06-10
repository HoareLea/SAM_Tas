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
                    if (!dictionary.TryGetValue(systemComponents_SystemConnection[i].Guid, out global::TPD.ISystemComponent systemComponent_1) || systemComponent_1 == null)
                    {
                        continue;
                    }

                    Core.Systems.SystemComponent systemComponent_Temp_1 = systemComponents_SystemConnection[i];

                    systemConnection.TryGetIndex(systemComponents_SystemConnection[i], out int index_1);
                    Direction direction_1 = systemComponents_SystemConnection[i].SystemConnectorManager.GetDirection(index_1);

                    for (int j = i + 1; j < systemComponents_SystemConnection.Count; j++)
                    {
                        if (!dictionary.TryGetValue(systemComponents_SystemConnection[j].Guid, out global::TPD.ISystemComponent systemComponent_2) || systemComponent_2 == null)
                        {
                            continue;
                        }

                        Core.Systems.SystemComponent systemComponent_Temp_2 = systemComponents_SystemConnection[j];

                        systemConnection.TryGetIndex(systemComponent_Temp_2, out int index_2);
                        Direction direction_2 = systemComponent_Temp_2.SystemConnectorManager.GetDirection(index_2);
                        if (direction_1 == Direction.In)
                        {
                            global::TPD.ISystemComponent systemComponent_Temp = systemComponent_1;
                            systemComponent_1 = systemComponent_2;
                            systemComponent_2 = systemComponent_Temp;
                        }

                        Duct duct = system.AddDuct((global::TPD.SystemComponent)systemComponent_1, 1, (global::TPD.SystemComponent)systemComponent_2, 1);
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
