// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Core.Systems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// SAM #113, on the real explicit route. PR1 derives every guid from a key that includes the operating
    /// schedule's name, so renaming the schedule re-keys the whole graph - which is exactly how the original
    /// observation made one network size or refuse. The native creation rank must not move with it.
    /// </summary>
    [TestFixture]
    public class VentilationCreationRankTests
    {
        private static MechanicalVentilationSettings Settings(string name_Schedule)
        {
            YearlySchedule yearlySchedule = new YearlySchedule(name_Schedule);
            double[] values = new double[8760];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = 1.0;
            }

            yearlySchedule.Values = values;

            return new MechanicalVentilationSettings { Schedule = yearlySchedule, MaterialiseSystemSpaceComponents = false };
        }

        /// <summary>Per air system (keyed by its rooms), the components in rank order, each written as what it is.</summary>
        private static Dictionary<string, List<string>> RankedIdentities(AdjacencyCluster adjacencyCluster, string name_Schedule, out HashSet<Guid> guids)
        {
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = adjacencyCluster.MechanicalVentilation(SystemVentilationFixture.Template(), Settings(name_Schedule));
            Assert.That(mechanicalVentilationMaterialisation.IsMaterialised, Is.True, string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                SystemVentilationFixture.ZoneReferences(adjacencyCluster));

            SystemEnergyCentre systemEnergyCentre = new SystemEnergyCentre(mechanicalVentilationMaterialisation.SystemEnergyCentre);
            TPD.Modify.MaterialiseVentilationDutyCarriers(systemEnergyCentre, systemVentilationConversionContext);
            Assert.That(systemVentilationConversionContext.Refusals, Is.Empty);

            SystemPlantRoom systemPlantRoom = systemEnergyCentre.GetSystemPlantRooms().Single();
            guids = new HashSet<Guid>();

            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            foreach (AirSystem airSystem in systemPlantRoom.GetSystems<AirSystem>())
            {
                List<Core.Systems.SystemComponent> components = systemPlantRoom.GetSystemComponents<Core.Systems.SystemComponent>(airSystem);
                components.RemoveAll(x => x is ISystemController || x is ISystemConnection || (systemPlantRoom.GetRelatedObjects<ISystemConnection>(x)?.Count ?? 0) == 0);

                Dictionary<Guid, int> rank = systemPlantRoom.VentilationCreationRank(components, systemVentilationConversionContext);
                Assert.That(rank.Count, Is.EqualTo(components.Count), "Every connected component has a rank.");

                Dictionary<Guid, string> identity_Base = components.ToDictionary(x => x.Guid, x => Identity(x, systemVentilationConversionContext));
                List<string> identities = components.OrderBy(x => rank[x.Guid]).Select(x => Label(systemPlantRoom, x, identity_Base)).ToList();
                string key = string.Join(",", identities.Where(x => x.StartsWith("space:", StringComparison.Ordinal)).OrderBy(x => x, StringComparer.Ordinal));
                result[key] = identities;

                foreach (Core.Systems.SystemComponent component in components)
                {
                    guids.Add(component.Guid);
                }
            }

            return result;
        }

        private static string Identity(Core.Systems.SystemComponent component, SystemVentilationConversionContext systemVentilationConversionContext)
        {
            SystemVentilationRoomIntent roomIntent = systemVentilationConversionContext.RoomIntent(component.Guid);
            if (roomIntent != null)
            {
                return "space:" + roomIntent.Guid_Space.ToString("N");
            }

            SystemVentilationLegIntent legIntent = systemVentilationConversionContext.LegIntentByDutyCarrier(component.Guid);
            if (legIntent != null)
            {
                return string.Concat("leg:", legIntent.ConnectionType, ":", systemVentilationConversionContext.RoomIntent(legIntent.Guid_SystemSpace_From)?.Guid_Space, ">", systemVentilationConversionContext.RoomIntent(legIntent.Guid_SystemSpace_To)?.Guid_Space);
            }

            return component.GetType().Name;
        }

        /// <summary>
        /// A room or duty carrier as the analytical object it stands for; any other component as its kind plus what it
        /// is connected to, connector by connector - so two components of one kind (a supply and a return fan) cannot
        /// swap places without the sequence changing.
        /// </summary>
        private static string Label(SystemPlantRoom systemPlantRoom, Core.Systems.SystemComponent component, Dictionary<Guid, string> identity_Base)
        {
            string result = identity_Base[component.Guid];
            if (result.StartsWith("space:", StringComparison.Ordinal) || result.StartsWith("leg:", StringComparison.Ordinal))
            {
                return result;
            }

            List<string> neighbours = new List<string>();
            foreach (ISystemConnection systemConnection in systemPlantRoom.GetRelatedObjects<ISystemConnection>(component) ?? new List<ISystemConnection>())
            {
                if (!systemConnection.TryGetIndex(component, out int index))
                {
                    continue;
                }

                foreach (Core.Systems.SystemComponent other in systemPlantRoom.GetRelatedObjects<Core.Systems.SystemComponent>(systemConnection) ?? new List<Core.Systems.SystemComponent>())
                {
                    if (other.Guid != component.Guid)
                    {
                        neighbours.Add(index + ">" + (identity_Base.TryGetValue(other.Guid, out string identity) ? identity : other.GetType().Name));
                    }
                }
            }

            neighbours.Sort(StringComparer.Ordinal);
            return string.Concat(result, "[", string.Join(",", neighbours), "]");
        }

        [Test]
        public void CreationRank_DoesNotMoveWhenTheScheduleNameReKeysEveryGuid()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.TwoUnits(out AirHandlingUnit _, out AirHandlingUnit _);

            Dictionary<string, List<string>> a = RankedIdentities(adjacencyCluster, "Part O Continuous Operation", out HashSet<Guid> guids_A);
            Dictionary<string, List<string>> b = RankedIdentities(adjacencyCluster, "Part O Continuous Operation A", out HashSet<Guid> guids_B);

            Assert.That(guids_A.Overlaps(guids_B), Is.False, "The rename must re-key the graph, or this test proves nothing.");
            Assert.That(a.Keys, Is.EquivalentTo(b.Keys), "Same air systems, same rooms.");
            Assert.That(a.Count, Is.EqualTo(2));

            foreach (string key in a.Keys)
            {
                Assert.That(b[key], Is.EqualTo(a[key]), "Re-keying the same network changed its native creation order.");
            }
        }

        [Test]
        public void CreationRank_EveryRoomAndDutyCarrierIsRanked()
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Dwelling(out AirHandlingUnit _);

            Dictionary<string, List<string>> ranked = RankedIdentities(adjacencyCluster, "Continuous", out HashSet<Guid> _);
            List<string> identities = ranked.Values.Single();

            Assert.That(identities.Count(x => x.StartsWith("space:", StringComparison.Ordinal)), Is.EqualTo(identities.Where(x => x.StartsWith("space:", StringComparison.Ordinal)).Distinct().Count()), "One rank per room.");
            Assert.That(identities.Any(x => x.StartsWith("leg:", StringComparison.Ordinal)), Is.True, "Duty carriers are ranked by the leg they carry.");
        }
    }
}
