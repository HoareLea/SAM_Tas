// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>One directed air path between two component connectors, upstream to downstream.</summary>
    public sealed class CreationOrderEdge
    {
        public CreationOrderEdge(Guid upstream, int index_Upstream, Guid downstream, int index_Downstream)
        {
            Upstream = upstream;
            Index_Upstream = index_Upstream;
            Downstream = downstream;
            Index_Downstream = index_Downstream;
        }

        public Guid Downstream { get; }
        public int Index_Downstream { get; }
        public int Index_Upstream { get; }
        public Guid Upstream { get; }
    }

    public static partial class Query
    {
        /// <summary>
        /// The order an air system's components are created in natively, as a function of the graph alone.
        /// <para>
        /// TAS Systems sizing and simulation are sensitive to creation order (SAM #113): the same network
        /// built in two orders can size in one and fail flow sizing, or never terminate, in the other. Guids
        /// are arbitrary, so ordering by guid makes that outcome arbitrary too.
        /// </para>
        /// <para>
        /// The order is an air-path traversal: from each source (a component nothing flows into) downstream,
        /// breadth first, a component's outlets in connector order. Siblings are ranked by a structural key,
        /// refined from each component's own identity (<paramref name="identity"/>: its kind and any analytical
        /// identity it stands for) through its whole neighbourhood, so the key depends on what the component
        /// is and where it sits - never on a guid. Only components the refinement cannot tell apart (graph
        /// automorphisms, whose swap yields the same network) fall back to the guid.
        /// </para>
        /// </summary>
        public static List<Guid> CreationOrder(IEnumerable<Guid> nodes, IEnumerable<CreationOrderEdge> edges, Func<Guid, string> identity)
        {
            List<Guid> result = new List<Guid>();
            if (nodes == null)
            {
                return result;
            }

            List<Guid> guids = nodes.Distinct().ToList();
            HashSet<Guid> set = new HashSet<Guid>(guids);
            List<CreationOrderEdge> edges_Valid = edges?.Where(x => x != null && set.Contains(x.Upstream) && set.Contains(x.Downstream) && x.Upstream != x.Downstream).ToList() ?? new List<CreationOrderEdge>();

            Dictionary<Guid, List<CreationOrderEdge>> outs = guids.ToDictionary(x => x, x => new List<CreationOrderEdge>());
            Dictionary<Guid, List<CreationOrderEdge>> ins = guids.ToDictionary(x => x, x => new List<CreationOrderEdge>());
            foreach (CreationOrderEdge edge in edges_Valid)
            {
                outs[edge.Upstream].Add(edge);
                ins[edge.Downstream].Add(edge);
            }

            Dictionary<Guid, string> key = guids.ToDictionary(x => x, x => Hash(identity?.Invoke(x) ?? string.Empty));
            int count_Classes = key.Values.Distinct().Count();
            for (int round = 0; round < guids.Count; round++)
            {
                Dictionary<Guid, string> key_Next = new Dictionary<Guid, string>();
                foreach (Guid guid in guids)
                {
                    IEnumerable<string> down = outs[guid].Select(x => string.Concat("o", x.Index_Upstream.ToString(), ">", x.Index_Downstream.ToString(), ":", key[x.Downstream])).OrderBy(x => x, StringComparer.Ordinal);
                    IEnumerable<string> up = ins[guid].Select(x => string.Concat("i", x.Index_Downstream.ToString(), "<", x.Index_Upstream.ToString(), ":", key[x.Upstream])).OrderBy(x => x, StringComparer.Ordinal);
                    key_Next[guid] = Hash(string.Concat(key[guid], "|", string.Join(",", down), "|", string.Join(",", up)));
                }

                key = key_Next;
                int count = key.Values.Distinct().Count();
                if (count == count_Classes)
                {
                    break;
                }

                count_Classes = count;
            }

            Comparison<Guid> comparison = (x, y) =>
            {
                int compare = string.CompareOrdinal(key[x], key[y]);
                return compare != 0 ? compare : x.CompareTo(y);
            };

            HashSet<Guid> visited = new HashSet<Guid>();
            Queue<Guid> queue = new Queue<Guid>();

            List<Guid> sources = guids.Where(x => ins[x].Count == 0).ToList();
            sources.Sort(comparison);
            List<Guid> remaining = new List<Guid>(guids);
            remaining.Sort(comparison);

            IEnumerator<Guid> enumerator_Source = sources.GetEnumerator();
            while (result.Count < guids.Count)
            {
                if (queue.Count == 0)
                {
                    Guid root = Guid.Empty;
                    bool found = false;
                    while (enumerator_Source.MoveNext())
                    {
                        if (!visited.Contains(enumerator_Source.Current))
                        {
                            root = enumerator_Source.Current;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        //every remaining component lies on a cycle no source reaches
                        root = remaining.First(x => !visited.Contains(x));
                    }

                    visited.Add(root);
                    queue.Enqueue(root);
                }

                Guid guid = queue.Dequeue();
                result.Add(guid);

                List<CreationOrderEdge> edges_Out = new List<CreationOrderEdge>(outs[guid]);
                edges_Out.Sort((x, y) =>
                {
                    int compare = x.Index_Upstream.CompareTo(y.Index_Upstream);
                    if (compare != 0) { return compare; }
                    compare = comparison(x.Downstream, y.Downstream);
                    return compare != 0 ? compare : x.Index_Downstream.CompareTo(y.Index_Downstream);
                });

                foreach (CreationOrderEdge edge in edges_Out)
                {
                    if (visited.Add(edge.Downstream))
                    {
                        queue.Enqueue(edge.Downstream);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// <see cref="CreationOrder(IEnumerable{Guid}, IEnumerable{CreationOrderEdge}, Func{Guid, string})"/> for the
        /// explicit ventilation route: the given components of one air system, the plant room's air connections
        /// between them, and each component's identity = its kind, its connector count and, where it stands for
        /// one, the analytical room (<c>Space</c>) or leg it carries. Returns rank by component guid.
        /// </summary>
        public static Dictionary<Guid, int> VentilationCreationRank(this SystemPlantRoom systemPlantRoom, IEnumerable<Core.Systems.SystemComponent> systemComponents, SystemVentilationConversionContext systemVentilationConversionContext)
        {
            Dictionary<Guid, int> result = new Dictionary<Guid, int>();
            if (systemPlantRoom == null || systemComponents == null)
            {
                return result;
            }

            Dictionary<Guid, Core.Systems.SystemComponent> components = new Dictionary<Guid, Core.Systems.SystemComponent>();
            foreach (Core.Systems.SystemComponent systemComponent in systemComponents)
            {
                if (systemComponent != null)
                {
                    components[systemComponent.Guid] = systemComponent;
                }
            }

            List<CreationOrderEdge> edges = VentilationCreationEdges(systemPlantRoom, components);

            List<Guid> order = CreationOrder(components.Keys, edges, guid => VentilationCreationIdentity(components[guid], systemVentilationConversionContext));
            for (int i = 0; i < order.Count; i++)
            {
                result[order[i]] = i;
            }

            return result;
        }

        private static List<CreationOrderEdge> VentilationCreationEdges(SystemPlantRoom systemPlantRoom, Dictionary<Guid, Core.Systems.SystemComponent> components)
        {
            List<CreationOrderEdge> result = new List<CreationOrderEdge>();
            SystemType systemType = new SystemType(typeof(AirSystem));

            HashSet<Guid> guids_Connection = new HashSet<Guid>();
            foreach (Core.Systems.SystemComponent systemComponent in components.Values)
            {
                List<ISystemConnection> systemConnections = systemPlantRoom.GetRelatedObjects<ISystemConnection>(systemComponent);
                if (systemConnections == null)
                {
                    continue;
                }

                foreach (ISystemConnection systemConnection in systemConnections)
                {
                    if (systemConnection == null || systemConnection.SystemType != systemType || !guids_Connection.Add(systemConnection.Guid))
                    {
                        continue;
                    }

                    List<Core.Systems.SystemComponent> ends = systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(systemConnection)?.FindAll(x => x != null && components.ContainsKey(x.Guid));
                    if (ends == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < ends.Count - 1; i++)
                    {
                        for (int j = i + 1; j < ends.Count; j++)
                        {
                            if (ends[i].Guid == ends[j].Guid
                                || !systemConnection.TryGetIndex(ends[i], out int index_i)
                                || !systemConnection.TryGetIndex(ends[j], out int index_j))
                            {
                                continue;
                            }

                            bool i_Downstream = ends[i].SystemConnectorManager.GetDirection(index_i) == Direction.In;
                            result.Add(i_Downstream
                                ? new CreationOrderEdge(ends[j].Guid, index_j, ends[i].Guid, index_i)
                                : new CreationOrderEdge(ends[i].Guid, index_i, ends[j].Guid, index_j));
                        }
                    }
                }
            }

            return result;
        }

        private static string VentilationCreationIdentity(Core.Systems.SystemComponent systemComponent, SystemVentilationConversionContext systemVentilationConversionContext)
        {
            StringBuilder stringBuilder = new StringBuilder(systemComponent.GetType().FullName);
            stringBuilder.Append('|').Append(systemComponent.SystemConnectorManager?.SystemConnectors?.Count() ?? 0);

            SystemVentilationRoomIntent roomIntent = systemVentilationConversionContext?.RoomIntent(systemComponent.Guid);
            if (roomIntent != null)
            {
                stringBuilder.Append("|space:").Append(roomIntent.Guid_Space.ToString("N"));
            }

            SystemVentilationLegIntent legIntent = systemVentilationConversionContext?.LegIntentByDutyCarrier(systemComponent.Guid);
            if (legIntent != null)
            {
                Guid guid_Space_From = systemVentilationConversionContext.RoomIntent(legIntent.Guid_SystemSpace_From)?.Guid_Space ?? Guid.Empty;
                Guid guid_Space_To = systemVentilationConversionContext.RoomIntent(legIntent.Guid_SystemSpace_To)?.Guid_Space ?? Guid.Empty;
                stringBuilder.Append("|leg:").Append(legIntent.ConnectionType)
                    .Append(':').Append(guid_Space_From.ToString("N"))
                    .Append('>').Append(guid_Space_To.ToString("N"))
                    .Append(':').Append(legIntent.Guid_SpaceAirMovement.ToString("N"));
            }

            return stringBuilder.ToString();
        }

        private static string Hash(string text)
        {
            using (SHA256 sHA256 = SHA256.Create())
            {
                byte[] bytes = sHA256.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                StringBuilder stringBuilder = new StringBuilder(32);
                for (int i = 0; i < 16; i++)
                {
                    stringBuilder.Append(bytes[i].ToString("x2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}
