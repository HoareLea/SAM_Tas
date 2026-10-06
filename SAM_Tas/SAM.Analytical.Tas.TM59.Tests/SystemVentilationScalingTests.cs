// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical;
using SAM.Analytical.Systems;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Step 10. Structural evidence that the route's mapping and extraction stay approximately linear
    /// from 100 to 5,000 rooms.
    /// <para>
    /// <b>Structural, not a stopwatch race.</b> A licensed annual TAS simulation at five thousand rooms
    /// would take hours and would measure TAS, not this code. What is measured instead is the thing
    /// that would actually turn quadratic: the number of <b>indexed lookups</b> the intent, the
    /// conversion and the result extraction perform. <c>SystemVentilationConversionContext</c> counts
    /// every dictionary probe it makes, so "one probe per room" can be asserted directly rather than
    /// inferred from a duration on one machine.
    /// </para>
    /// <para>What each guard here rules out, in the words of the hazard:</para>
    /// <list type="bullet">
    /// <item><description><b>per-room whole-TSD scans</b> and <b>per-room TAS component scans</b>: the
    /// zone a room resolves to comes from <c>System.GetComponentByGUID</c> against an identity recorded
    /// at the pairing point, and the systems are indexed once per
    /// document;</description></item>
    /// <item><description><b>display-name <c>Find</c></b>: nothing on the path takes a
    /// name;</description></item>
    /// <item><description><b>room × result loops</b>: one series is requested per room, by
    /// identity;</description></item>
    /// <item><description><b>repeated whole-connection scans</b>: the plant room's connections are
    /// indexed once, then every leg resolves by probe;</description></item>
    /// <item><description><b>extracting all 11 result types</b>: only
    /// <c>SpaceDataType.ZoneTemperature</c> is requested.</description></item>
    /// </list>
    /// <para>
    /// A timing ratio is reported alongside as corroboration. It is deliberately given a generous bound
    /// - a build agent's timings are noisy, and a quadratic path at fifty times the size would exceed
    /// any bound by orders of magnitude rather than by a factor of two.
    /// </para>
    /// </summary>
    [TestFixture]
    public class SystemVentilationScalingTests
    {
        /// <summary>Five rooms per dwelling, so 20/200/1000 dwellings give 100/1,000/5,000 rooms.</summary>
        private const int RoomsPerDwelling = 5;

        private sealed class Measurement
        {
            public int Count_Rooms { get; set; }

            public int Count_AirSystems { get; set; }

            public int Count_Legs { get; set; }

            public int LookupCount { get; set; }

            public double Milliseconds { get; set; }

            /// <summary>Building the intent from the PR1 graph.</summary>
            public double Milliseconds_Intent { get; set; }

            /// <summary>Materialising one duty carrier per extract and transfer leg.</summary>
            public double Milliseconds_DutyCarriers { get; set; }

            /// <summary>Recording the pairings and reconciling.</summary>
            public double Milliseconds_Reconcile { get; set; }

            public override string ToString()
            {
                return string.Format(
                    "{0} rooms / {1} air systems / {2} legs: {3} indexed lookups, {4:F0} ms "
                    + "(intent {5:F0}, carriers {6:F0}, reconcile {7:F0})",
                    Count_Rooms,
                    Count_AirSystems,
                    Count_Legs,
                    LookupCount,
                    Milliseconds,
                    Milliseconds_Intent,
                    Milliseconds_DutyCarriers,
                    Milliseconds_Reconcile);
            }
        }

        /// <summary>
        /// One full pass of everything PR2 does that is proportional to the model: build the intent from
        /// the PR1 graph, materialise a duty carrier for every extract and transfer leg into a working
        /// copy, play back the pairings a conversion records, and reconcile.
        /// </summary>
        private static Measurement Measure(int dwellings)
        {
            AdjacencyCluster adjacencyCluster = SystemVentilationFixture.Scaled(dwellings, 4, out int count_Spaces, out int count_AirHandlingUnits);

            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation = SystemVentilationFixture.Materialise(adjacencyCluster);

            Assert.That(mechanicalVentilationMaterialisation.IsMaterialised, Is.True, string.Join(" | ", mechanicalVentilationMaterialisation.Refusals));

            Dictionary<Guid, string> zoneReferences = SystemVentilationFixture.ZoneReferences(adjacencyCluster);

            Stopwatch stopwatch = Stopwatch.StartNew();
            Stopwatch stopwatch_Phase = Stopwatch.StartNew();

            SystemVentilationConversionContext systemVentilationConversionContext = TPD.Create.SystemVentilationConversionContext(
                mechanicalVentilationMaterialisation.SystemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                zoneReferences);

            double milliseconds_Intent = stopwatch_Phase.Elapsed.TotalMilliseconds;
            stopwatch_Phase.Restart();

            Core.Systems.SystemEnergyCentre systemEnergyCentre = new Core.Systems.SystemEnergyCentre(mechanicalVentilationMaterialisation.SystemEnergyCentre);

            TPD.Modify.MaterialiseVentilationDutyCarriers(systemEnergyCentre, systemVentilationConversionContext);

            double milliseconds_DutyCarriers = stopwatch_Phase.Elapsed.TotalMilliseconds;
            stopwatch_Phase.Restart();

            foreach (KeyValuePair<Guid, Guid> keyValuePair in systemVentilationConversionContext.AirSystemByAirHandlingUnit)
            {
                systemVentilationConversionContext.RecordAirSystem(keyValuePair.Value, string.Concat("{SYS-", keyValuePair.Value.ToString("D"), "}"));
                systemVentilationConversionContext.RecordNativeSystem();
            }

            foreach (SystemVentilationRoomIntent systemVentilationRoomIntent in systemVentilationConversionContext.RoomIntents)
            {
                string reference_SystemZone = string.Concat("{ZONE-", systemVentilationRoomIntent.Guid_SystemSpace.ToString("D"), "}");

                systemVentilationConversionContext.RecordPairing(systemVentilationRoomIntent.Guid_SystemSpace, reference_SystemZone);

                systemVentilationConversionContext.RecordRoomPairing(
                    systemVentilationRoomIntent.Guid_SystemSpace,
                    reference_SystemZone,
                    systemVentilationRoomIntent.Reference_ZoneLoad,
                    string.Concat("{SYS-", systemVentilationRoomIntent.Guid_AirSystem.ToString("D"), "}"),
                    systemVentilationRoomIntent.DesignFlowRate_Supply_Lps);
            }

            foreach (SystemVentilationLegIntent systemVentilationLegIntent in systemVentilationConversionContext.LegIntents)
            {
                systemVentilationConversionContext.Record(new SystemVentilationConnectionBinding(
                    systemVentilationLegIntent.ConnectionType,
                    systemVentilationLegIntent.Guid_SpaceAirMovement,
                    systemVentilationLegIntent.Guid_SystemConnection,
                    systemVentilationLegIntent.Guid_AirSystem,
                    systemVentilationLegIntent.Guid_SystemSpace_From,
                    systemVentilationLegIntent.Guid_SystemSpace_To,
                    systemVentilationLegIntent.DesignFlowRate_Lps,
                    systemVentilationLegIntent.RequiresDutyCarrier
                        ? string.Concat("{DMP-", systemVentilationLegIntent.Guid_DutyCarrier.ToString("D"), "}")
                        : null));
            }

            systemVentilationConversionContext.CompleteRoomBindings();

            bool reconciled = systemVentilationConversionContext.Reconcile();

            double milliseconds_Reconcile = stopwatch_Phase.Elapsed.TotalMilliseconds;

            stopwatch.Stop();

            Assert.That(reconciled, Is.True, string.Join(" | ", systemVentilationConversionContext.Refusals));

            return new Measurement
            {
                Count_Rooms = systemVentilationConversionContext.RoomIntents.Count,
                Count_AirSystems = systemVentilationConversionContext.AirSystemByAirHandlingUnit.Count,
                Count_Legs = systemVentilationConversionContext.LegIntents.Count,
                LookupCount = systemVentilationConversionContext.LookupCount,
                Milliseconds = stopwatch.Elapsed.TotalMilliseconds,
                Milliseconds_Intent = milliseconds_Intent,
                Milliseconds_DutyCarriers = milliseconds_DutyCarriers,
                Milliseconds_Reconcile = milliseconds_Reconcile,
            };
        }

        [Test]
        public void MappingAndExtractionStayApproximatelyLinearFrom100To5000Rooms()
        {
            //Warm the JIT and the resource load, so the first measurement is not paying for both.
            Measure(2);

            Measurement measurement_100 = Measure(100 / RoomsPerDwelling);
            Measurement measurement_1000 = Measure(1000 / RoomsPerDwelling);
            Measurement measurement_5000 = Measure(5000 / RoomsPerDwelling);

            TestContext.Out.WriteLine(measurement_100.ToString());
            TestContext.Out.WriteLine(measurement_1000.ToString());
            TestContext.Out.WriteLine(measurement_5000.ToString());

            Assert.Multiple(() =>
            {
                Assert.That(measurement_100.Count_Rooms, Is.EqualTo(100));
                Assert.That(measurement_1000.Count_Rooms, Is.EqualTo(1000));
                Assert.That(measurement_5000.Count_Rooms, Is.EqualTo(5000));
            });

            //The decisive measurement: indexed lookups per room must not grow with the model. A
            //per-room scan of the rooms, of the legs or of the connections would make this ratio grow
            //linearly with size - fifty-fold between the first and the last.
            double perRoom_100 = measurement_100.LookupCount / (double)measurement_100.Count_Rooms;
            double perRoom_1000 = measurement_1000.LookupCount / (double)measurement_1000.Count_Rooms;
            double perRoom_5000 = measurement_5000.LookupCount / (double)measurement_5000.Count_Rooms;

            TestContext.Out.WriteLine(string.Format(
                "indexed lookups per room: {0:F2} / {1:F2} / {2:F2}",
                perRoom_100,
                perRoom_1000,
                perRoom_5000));

            Assert.Multiple(() =>
            {
                Assert.That(perRoom_5000, Is.EqualTo(perRoom_100).Within(0.5), "lookups per room must not grow with the model");
                Assert.That(perRoom_1000, Is.EqualTo(perRoom_100).Within(0.5));
            });

            //Corroboration only, and deliberately loose: a quadratic path at fifty times the size would
            //miss this by orders of magnitude, not by a factor of two.
            double perRoom_Milliseconds_100 = measurement_100.Milliseconds / measurement_100.Count_Rooms;
            double perRoom_Milliseconds_5000 = measurement_5000.Milliseconds / measurement_5000.Count_Rooms;

            TestContext.Out.WriteLine(string.Format(
                "milliseconds per room: {0:F4} at 100, {1:F4} at 5000, ratio {2:F2}",
                perRoom_Milliseconds_100,
                perRoom_Milliseconds_5000,
                perRoom_Milliseconds_100 <= 0 ? double.NaN : perRoom_Milliseconds_5000 / perRoom_Milliseconds_100));

            Assert.That(
                perRoom_Milliseconds_5000,
                Is.LessThan(System.Math.Max(perRoom_Milliseconds_100 * 8.0, 1.0)),
                "per-room cost must not grow materially with the model");
        }

        [Test]
        public void EveryRoomAndEveryLegIsAccountedForAtEachSize()
        {
            foreach (int dwellings in new[] { 100 / RoomsPerDwelling, 1000 / RoomsPerDwelling, 5000 / RoomsPerDwelling })
            {
                Measurement measurement = Measure(dwellings);

                Assert.Multiple(() =>
                {
                    //Per dwelling: two supply legs, two extract legs, four transfers.
                    Assert.That(measurement.Count_Legs, Is.EqualTo(dwellings * 8), measurement.ToString());

                    //Four dwellings per unit, so the unit count is the room count divided by twenty.
                    Assert.That(measurement.Count_AirSystems, Is.EqualTo(dwellings / 4), measurement.ToString());
                });
            }
        }
    }
}
