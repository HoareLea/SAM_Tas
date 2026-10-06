// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
        public static List<Duct> Ducts(this SystemPlantRoom systemPlantRoom, global::TPD.System system, Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponents, out Dictionary<Guid, Duct> dictionary_Ducts, SystemVentilationConversionContext systemVentilationConversionContext = null)
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

            if (systemVentilationConversionContext != null)
            {
                return DuctsWithBranchJunctions(
                    systemPlantRoom,
                    system,
                    dictionary_SystemComponents,
                    systemConnections,
                    systemVentilationConversionContext,
                    out dictionary_Ducts);
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

        /// <summary>
        /// Materialises the explicit route's many-to-one and one-to-many connectors through native
        /// junctions. <c>System.AddDuct</c> accepts several ducts on one ordinary component port, but
        /// TAS rejects that graph when the air system is simulated. A junction is the native topology
        /// carrier: it owns no duty, while every extract or transfer leg keeps its own in-line damper.
        /// </summary>
        private static List<Duct> DuctsWithBranchJunctions(
            SystemPlantRoom systemPlantRoom,
            global::TPD.System system,
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponents,
            List<ISystemConnection> systemConnections,
            SystemVentilationConversionContext systemVentilationConversionContext,
            out Dictionary<Guid, Duct> dictionary_Ducts)
        {
            dictionary_Ducts = new Dictionary<Guid, Duct>();

            List<BranchDuctEdge> edges = new List<BranchDuctEdge>();
            Dictionary<Guid, Core.Systems.SystemComponent> components_Ranked = new Dictionary<Guid, Core.Systems.SystemComponent>();
            SystemType systemType = new SystemType(typeof(AirSystem));

            systemConnections.Sort((x, y) => x.Guid.CompareTo(y.Guid));

            foreach (ISystemConnection systemConnection in systemConnections)
            {
                if (systemConnection == null || systemConnection.SystemType != systemType)
                {
                    continue;
                }

                List<Core.Systems.SystemComponent> components = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(systemConnection);
                if (components == null)
                {
                    continue;
                }

                components.Sort((x, y) => x.Guid.CompareTo(y.Guid));

                for (int i = 0; i < components.Count - 1; i++)
                {
                    for (int j = i + 1; j < components.Count; j++)
                    {
                        BranchDuctEnd end_1 = BranchEnd(systemConnection, components[i], dictionary_SystemComponents);
                        BranchDuctEnd end_2 = BranchEnd(systemConnection, components[j], dictionary_SystemComponents);

                        if (end_1 == null || end_2 == null || end_1.Guid_Component == end_2.Guid_Component)
                        {
                            continue;
                        }

                        components_Ranked[components[i].Guid] = components[i];
                        components_Ranked[components[j].Guid] = components[j];

                        if (end_1.Direction == Direction.In)
                        {
                            BranchDuctEnd end_Temp = end_1;
                            end_1 = end_2;
                            end_2 = end_Temp;
                        }

                        edges.Add(new BranchDuctEdge(systemConnection, end_1, end_2));
                    }
                }
            }

            Dictionary<string, int> count_By_End = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, BranchDuctEnd> end_By_Key = new Dictionary<string, BranchDuctEnd>(StringComparer.Ordinal);

            foreach (BranchDuctEdge edge in edges)
            {
                Count(edge.Upstream, count_By_End, end_By_Key);
                Count(edge.Downstream, count_By_End, end_By_Key);
            }

            Dictionary<string, Junction> junction_By_End = new Dictionary<string, Junction>(StringComparer.Ordinal);
            List<string> branchKeys = new List<string>();

            foreach (KeyValuePair<string, int> keyValuePair in count_By_End)
            {
                if (keyValuePair.Value > 1)
                {
                    branchKeys.Add(keyValuePair.Key);
                }
            }

            //SAM #113: TAS sizing and simulation depend on creation order, so junctions and ducts are
            //created along the air path (Query.CreationOrder), never in guid order - the same network
            //under any identities is materialised identically.
            Dictionary<Guid, int> rank = systemPlantRoom.VentilationCreationRank(components_Ranked.Values, systemVentilationConversionContext);
            Func<Guid, int> Rank = guid => rank.TryGetValue(guid, out int value) ? value : int.MaxValue;

            branchKeys.Sort((x, y) =>
            {
                BranchDuctEnd end_x = end_By_Key[x], end_y = end_By_Key[y];
                int compare = Rank(end_x.Guid_Component).CompareTo(Rank(end_y.Guid_Component));
                if (compare != 0) { return compare; }
                compare = end_x.Index_Connector.CompareTo(end_y.Index_Connector);
                return compare != 0 ? compare : string.CompareOrdinal(x, y);
            });

            edges.Sort((x, y) =>
            {
                int compare = Rank(x.Upstream.Guid_Component).CompareTo(Rank(y.Upstream.Guid_Component));
                if (compare != 0) { return compare; }
                compare = x.Upstream.Index_Connector.CompareTo(y.Upstream.Index_Connector);
                if (compare != 0) { return compare; }
                compare = Rank(x.Downstream.Guid_Component).CompareTo(Rank(y.Downstream.Guid_Component));
                if (compare != 0) { return compare; }
                compare = x.Downstream.Index_Connector.CompareTo(y.Downstream.Index_Connector);
                return compare != 0 ? compare : x.Connection.Guid.CompareTo(y.Connection.Guid);
            });

            List<Duct> result = new List<Duct>();

            foreach (string branchKey in branchKeys)
            {
                BranchDuctEnd end = end_By_Key[branchKey];
                Junction junction = system.AddJunction();

                if (junction == null)
                {
                    systemVentilationConversionContext.Refuse(string.Format(
                        "Native TAS did not create the topology junction required by component {0} connector {1}.",
                        end.Guid_Component,
                        end.Index_Connector));
                    continue;
                }

                ((dynamic)junction).Name = "Part O Branch Junction";
                ((dynamic)junction).Description = string.Format(
                    "Part O topology junction for component {0}, connector {1}.",
                    end.Guid_Component,
                    end.Index_Connector);

                junction_By_End[branchKey] = junction;

                //Presentation only: beside the component whose connector it branches.
                Modify.PlaceVentilationJunction(junction, end.Native, end.Direction != Direction.In);

                Duct bridge = end.Direction == Direction.In
                    ? AddDuct(system, (global::TPD.SystemComponent)junction, 1, end.Native, end.Index_Port, systemVentilationConversionContext, "branch-to-component bridge")
                    : AddDuct(system, end.Native, end.Index_Port, (global::TPD.SystemComponent)junction, 1, systemVentilationConversionContext, "component-to-branch bridge");

                if (bridge != null)
                {
                    result.Add(bridge);
                }
            }

            foreach (BranchDuctEdge edge in edges)
            {
                global::TPD.SystemComponent upstream = junction_By_End.TryGetValue(edge.Upstream.Key, out Junction junction_Upstream)
                    ? (global::TPD.SystemComponent)junction_Upstream
                    : edge.Upstream.Native;

                global::TPD.SystemComponent downstream = junction_By_End.TryGetValue(edge.Downstream.Key, out Junction junction_Downstream)
                    ? (global::TPD.SystemComponent)junction_Downstream
                    : edge.Downstream.Native;

                int port_Upstream = junction_Upstream == null ? edge.Upstream.Index_Port : 1;
                int port_Downstream = junction_Downstream == null ? edge.Downstream.Index_Port : 1;

                Duct duct = AddDuct(
                    system,
                    upstream,
                    port_Upstream,
                    downstream,
                    port_Downstream,
                    systemVentilationConversionContext,
                    string.Format("connection {0}", edge.Connection.Guid));

                if (duct == null)
                {
                    continue;
                }

                dictionary_Ducts[edge.Connection.Guid] = duct;
                result.Add(duct);

                //A duct reaching a room, a duty carrier or a branch junction is routed from where
                //Modify.LayOutVentilationSystem drew them; PR1's polyline was drawn for the template
                //prototype's position and would bend to nowhere. Trunk-to-trunk ducts keep it.
                bool laidOut = junction_Upstream != null
                    || junction_Downstream != null
                    || LaidOut(systemVentilationConversionContext, edge.Upstream.Guid_Component)
                    || LaidOut(systemVentilationConversionContext, edge.Downstream.Guid_Component);

                if (laidOut)
                {
                    foreach (int[] node in Modify.VentilationDuctNodes(upstream, downstream, junction_Downstream != null))
                    {
                        duct.AddNode(node[0], node[1]);
                    }
                }
                else if (edge.Connection is DisplaySystemConnection displaySystemConnection)
                {
                    SystemPolyline systemPolyline = displaySystemConnection.SystemGeometry;
                    List<Point2D> point2Ds = systemPolyline?.Points;

                    if (point2Ds != null && point2Ds.Count > 2)
                    {
                        for (int i = 1; i < point2Ds.Count - 1; i++)
                        {
                            Point2D point2D = point2Ds[i].ToTPD();
                            duct.AddNode(System.Convert.ToInt32(point2D.X), System.Convert.ToInt32(point2D.Y));
                        }
                    }
                }
            }

            return result;
        }

        private static bool LaidOut(SystemVentilationConversionContext systemVentilationConversionContext, Guid guid_Component)
        {
            return systemVentilationConversionContext.RoomIntent(guid_Component) != null
                || systemVentilationConversionContext.LegIntentByDutyCarrier(guid_Component) != null;
        }

        private static BranchDuctEnd BranchEnd(
            ISystemConnection systemConnection,
            Core.Systems.SystemComponent component,
            Dictionary<Guid, global::TPD.ISystemComponent> dictionary_SystemComponents)
        {
            if (component == null
                || !dictionary_SystemComponents.TryGetValue(component.Guid, out global::TPD.ISystemComponent native)
                || !(native is global::TPD.SystemComponent nativeComponent)
                || !systemConnection.TryGetIndex(component, out int index_Connector))
            {
                return null;
            }

            Direction direction = component.SystemConnectorManager.GetDirection(index_Connector);
            int index_Port = 1;

            if (component is SystemExchanger || component is SystemEconomiser || component is SystemDesiccantWheel)
            {
                if (component.SystemConnectorManager.TryGetSystemConnector(index_Connector, out SystemConnector systemConnector)
                    && systemConnector != null
                    && systemConnector.ConnectionIndex != -1)
                {
                    index_Port = systemConnector.ConnectionIndex;
                }
            }

            return new BranchDuctEnd(component.Guid, index_Connector, index_Port, direction, nativeComponent);
        }

        private static void Count(
            BranchDuctEnd end,
            Dictionary<string, int> count_By_End,
            Dictionary<string, BranchDuctEnd> end_By_Key)
        {
            count_By_End.TryGetValue(end.Key, out int count);
            count_By_End[end.Key] = count + 1;
            end_By_Key[end.Key] = end;
        }

        private static Duct AddDuct(
            global::TPD.System system,
            global::TPD.SystemComponent upstream,
            int port_Upstream,
            global::TPD.SystemComponent downstream,
            int port_Downstream,
            SystemVentilationConversionContext systemVentilationConversionContext,
            string description)
        {
            try
            {
                Duct result = system.AddDuct(upstream, port_Upstream, downstream, port_Downstream);
                if (result == null)
                {
                    systemVentilationConversionContext.Refuse(string.Format("Native TAS did not create the duct for {0}.", description));
                }

                return result;
            }
            catch (Exception exception)
            {
                systemVentilationConversionContext.Refuse(string.Format(
                    "Creating the native duct for {0} threw {1}: {2}.",
                    description,
                    exception.GetType().Name,
                    exception.Message));
                return null;
            }
        }

        private sealed class BranchDuctEnd
        {
            public BranchDuctEnd(Guid guid_Component, int index_Connector, int index_Port, Direction direction, global::TPD.SystemComponent native)
            {
                Guid_Component = guid_Component;
                Index_Connector = index_Connector;
                Index_Port = index_Port;
                Direction = direction;
                Native = native;
                Key = string.Format(
                    global::System.Globalization.CultureInfo.InvariantCulture,
                    "{0:N}:{1}:{2}",
                    guid_Component,
                    index_Connector,
                    direction);
            }

            public Direction Direction { get; }
            public Guid Guid_Component { get; }
            public int Index_Connector { get; }
            public int Index_Port { get; }
            public string Key { get; }
            public global::TPD.SystemComponent Native { get; }
        }

        private sealed class BranchDuctEdge
        {
            public BranchDuctEdge(ISystemConnection connection, BranchDuctEnd upstream, BranchDuctEnd downstream)
            {
                Connection = connection;
                Upstream = upstream;
                Downstream = downstream;
            }

            public ISystemConnection Connection { get; }
            public BranchDuctEnd Downstream { get; }
            public BranchDuctEnd Upstream { get; }
        }

    }
}
