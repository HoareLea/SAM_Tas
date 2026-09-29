// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Analytical.Tas.TPD;
using SAM.Core;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// PR3 of SAM-BIM/SAM#111: the temporary ResultantTemperature thermostat bridge and the provider seam in
    /// front of it.
    /// <para>
    /// <b>What runs for real here.</b> The production plan, the production TBD thermostat writer and the
    /// production TSD reader - driven with managed stand-ins for the TAS objects they touch, so no licence and
    /// no COM server is involved. The one TAS behaviour a stand-in has to imitate is modelled on what the
    /// licensed machine was <b>measured</b> to do (<see cref="MeasuredYearlyProfile"/>), not on what would be
    /// convenient: that is what makes the hour-alignment test able to fail.
    /// </para>
    /// <para>
    /// That a real Systems route produces a complete bridged resultant temperature is the licensed acceptance,
    /// recorded in <c>Documentation/evidence/PR3-RESULTANT-TEMPERATURE-BRIDGE.md</c>.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ThermostatBridgeTests
    {
        private const int Hours = ThermostatBridgePlan.HoursPerYear;

        private string directory;
        private string path_TBD_Source;
        private string path_TSD_Source;
        private string path_TPD;

        [OneTimeSetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SAM_PR3_Bridge", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            path_TBD_Source = Path.Combine(directory, "src.tbd");
            path_TSD_Source = Path.Combine(directory, "src.tsd");
            path_TPD = Path.Combine(directory, "acc.tpd");

            //Stand-ins for the files a complete thermal source is required to have; nothing opens them. The
            //TSD is above SimulationEvidence's stub threshold so the source counts as evidenced.
            File.WriteAllText(path_TBD_Source, "the no-IZAM source TBD - never opened by these tests");
            File.WriteAllBytes(path_TSD_Source, new byte[4096]);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
            }
        }

        // ============================================================================================ fixture

        private static Guid G(int seed)
        {
            return new Guid(seed, 0, 0, new byte[8]);
        }

        /// <summary>Room <paramref name="room"/>'s TBD zone guid, braced as TAS answers it.</summary>
        private static string Zone(int room)
        {
            return string.Concat("{", G(room + 500).ToString("D").ToUpperInvariant(), "}");
        }

        /// <summary>A distinct value for every room AND every hour, so a shift or a swap cannot cancel out.</summary>
        private static double Temperature(int room, int hour)
        {
            return 18.0 + room + hour / 10000.0 + 1.0 / 3.0;
        }

        private static IndexedDoubles Series(int room, int startHour, int endHour, Func<int, double> value = null)
        {
            IndexedDoubles result = new IndexedDoubles();

            for (int hour = startHour; hour <= endHour; hour++)
            {
                result[hour] = value == null ? Temperature(room, hour) : value(hour);
            }

            return result;
        }

        private static SystemVentilationBinding Binding(int room, string reference_ZoneLoad = null)
        {
            return new SystemVentilationBinding(G(room), G(room + 100), G(1000), string.Format("{{SZ-{0}}}", room), reference_ZoneLoad ?? Zone(room), "{SYS-1}", 20.0, null);
        }

        /// <summary>
        /// A complete Systems route over the given rooms, built through the route's own public constructors.
        /// The source states each zone as a BARE lower-case guid while the route's binding carries it braced
        /// upper-case - the same identity spelt the two ways TAS spells it.
        /// </summary>
        private SystemVentilationRoute Route(
            int[] rooms,
            int startHour = 0,
            int endHour = Hours - 1,
            Func<int, int, double> value = null,
            Dictionary<int, string> zoneLoadOverride = null,
            Dictionary<int, string> sourceZoneOverride = null)
        {
            List<SystemVentilationBinding> bindings = new List<SystemVentilationBinding>();
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(startHour, endHour);
            List<KeyValuePair<Guid, string>> zoneReferences = new List<KeyValuePair<Guid, string>>();

            foreach (int room in rooms)
            {
                string zoneLoad = zoneLoadOverride != null && zoneLoadOverride.TryGetValue(room, out string temp) ? temp : Zone(room);
                SystemVentilationBinding binding = Binding(room, zoneLoad);
                bindings.Add(binding);

                string sourceZone = sourceZoneOverride != null && sourceZoneOverride.TryGetValue(room, out string temp_Source) ? temp_Source : G(room + 500).ToString("D");
                zoneReferences.Add(new KeyValuePair<Guid, string>(G(room), sourceZone));

                systemZoneTemperatureResults.Add(new SystemZoneTemperatureResult(
                    binding.Guid_Space,
                    binding.Guid_SystemSpace,
                    binding.Reference_SystemZone,
                    binding.Reference_ZoneLoad,
                    startHour,
                    endHour,
                    Series(room, startHour, endHour, value == null ? (Func<int, double>)null : h => value(room, h)),
                    null));
            }

            systemZoneTemperatureResults.Validate(bindings);

            SimulationEvidence simulationEvidence_Source = new SimulationEvidence(SimulationOutputShape.SeparateOutputFile, path_TBD_Source, path_TSD_Source);
            simulationEvidence_Source.RecordCallReturned();
            simulationEvidence_Source.Conclude();

            NoIzamThermalSource noIzamThermalSource = new NoIzamThermalSource(path_TBD_Source, path_TSD_Source, true, true, simulationEvidence_Source, zoneReferences, null, null);

            SimulationEvidence simulationEvidence_Route = new SimulationEvidence(SimulationOutputShape.InPlaceDocument, path_TPD, null);
            simulationEvidence_Route.RecordCallReturned("Done");
            simulationEvidence_Route.RecordResultsReconciled(bindings.Count, startHour, endHour);

            //The route's own refusals are forwarded exactly as Create.SystemVentilationRoute forwards them, so a
            //refused route here says what a refused production route says.
            return new SystemVentilationRoute(noIzamThermalSource, path_TPD, simulationEvidence_Route, bindings, null, systemZoneTemperatureResults, systemZoneTemperatureResults.Refusals, null);
        }

        // ================================================================================ the plan (COM-free)

        [Test]
        public void TheAchievedZoneTemperatureOfEveryRoomIsPlannedByGuid()
        {
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 3, 1, 2 });
            Assert.That(systemVentilationRoute.IsComplete, Is.True, string.Join(" | ", systemVentilationRoute.Refusals));

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            Assert.That(thermostatBridgePlan.IsValid, Is.True, string.Join(" | ", thermostatBridgePlan.Refusals));
            Assert.That(thermostatBridgePlan.Count, Is.EqualTo(3));

            //Ordered by room guid, never by the order the rooms arrived in.
            Assert.That(thermostatBridgePlan.Transfers.Select(x => x.Guid_Space), Is.EqualTo(new[] { G(1), G(2), G(3) }));

            foreach (int room in new[] { 1, 2, 3 })
            {
                //Found by room guid AND by the zone guid in either spelling.
                ThermostatBridgeTransfer thermostatBridgeTransfer = thermostatBridgePlan.Transfer(G(room));
                Assert.That(thermostatBridgePlan.Transfer(Zone(room)), Is.SameAs(thermostatBridgeTransfer));
                Assert.That(thermostatBridgePlan.Transfer(G(room + 500).ToString("N")), Is.SameAs(thermostatBridgeTransfer));

                Assert.That(thermostatBridgeTransfer.Count, Is.EqualTo(Hours));

                foreach (int hour in new[] { 0, 1, 4380, Hours - 1 })
                {
                    Assert.That(thermostatBridgeTransfer.ZoneTemperature(hour), Is.EqualTo(Temperature(room, hour)));
                    Assert.That(thermostatBridgeTransfer.Value(hour), Is.EqualTo((float)Temperature(room, hour)));
                }
            }
        }

        [Test]
        public void OnlyTheWholeYearIsBridged_NeverPaddedOrTruncated()
        {
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 1 }, 0, 23);
            Assert.That(systemVentilationRoute.IsComplete, Is.True);

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            Assert.That(thermostatBridgePlan.IsValid, Is.False);
            Assert.That(thermostatBridgePlan.Transfers, Is.Empty);
            Assert.That(thermostatBridgePlan.Refusals.Single(), Does.Contain("without padding, truncating or repeating"));
        }

        [Test]
        public void TwoStatementsOfARoomsZoneThatDisagreeAreRefused()
        {
            //The no-IZAM source says one TBD zone; the Systems route bound a different zone load.
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 1, 2 }, zoneLoadOverride: new Dictionary<int, string> { { 2, Zone(9) } });
            Assume.That(systemVentilationRoute.IsComplete, Is.True, string.Join(" | ", systemVentilationRoute.Refusals));

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            Assert.That(thermostatBridgePlan.IsValid, Is.False);
            Assert.That(thermostatBridgePlan.Transfers, Is.Empty, "No partial plan: room 1 is fine and still is not bridged.");
            Assert.That(thermostatBridgePlan.Refusals.Single(), Does.Contain("ambiguous"));
        }

        [Test]
        public void TwoRoomsResolvingToOneTbdZoneAreRefused()
        {
            SystemVentilationRoute systemVentilationRoute = Route(
                new[] { 1, 2 },
                zoneLoadOverride: new Dictionary<int, string> { { 2, Zone(1) } },
                sourceZoneOverride: new Dictionary<int, string> { { 2, G(501).ToString("D") } });

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            //PR2's own result gate already refuses two rooms on one zone load; the bridge carries that refusal
            //through and plans nothing. (The plan's own duplicate-zone check is the same rule stated again, and
            //the writer and reader each refuse a zone guid answered twice - pinned below.)
            Assert.That(thermostatBridgePlan.IsValid, Is.False);
            Assert.That(thermostatBridgePlan.Transfers, Is.Empty);
            Assert.That(thermostatBridgePlan.Refusals.Any(x => x.Contains("both took their series from zone load")), Is.True, string.Join(" | ", thermostatBridgePlan.Refusals));
        }

        [Test]
        public void ARoomTheSourceStatesNoZoneForIsRefused()
        {
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 1, 2 }, sourceZoneOverride: new Dictionary<int, string> { { 2, null } });
            Assume.That(systemVentilationRoute.IsComplete, Is.True);

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            Assert.That(thermostatBridgePlan.IsValid, Is.False);
            Assert.That(thermostatBridgePlan.Refusals.Single(), Does.Contain("states no TBD zone"));
        }

        [Test]
        public void AFiniteValueNoThermostatCanHoldIsRefused()
        {
            //Finite as a double, infinite the moment TAS stores it single precision.
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 1 }, value: (room, hour) => hour == 5000 ? 1e39 : Temperature(room, hour));
            Assume.That(systemVentilationRoute.IsComplete, Is.True, "The route validates double finiteness only.");

            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(systemVentilationRoute);

            Assert.That(thermostatBridgePlan.IsValid, Is.False);
            Assert.That(thermostatBridgePlan.Refusals.Single(), Does.Contain("hour 5000"));
        }

        [Test]
        public void AMissingOrNonFiniteZoneTemperatureRefusesTheWholeBridge()
        {
            foreach (Func<int, int, double> value in new Func<int, int, double>[] { (room, hour) => room == 2 && hour == 17 ? double.NaN : Temperature(room, hour), (room, hour) => room == 2 && hour == 17 ? double.PositiveInfinity : Temperature(room, hour) })
            {
                SystemVentilationRoute systemVentilationRoute = Route(new[] { 1, 2 }, value: value);

                //The route itself refuses; the bridge must refuse with it and hand back no series.
                ResultantTemperatureResults resultantTemperatureResults = Provider().ResultantTemperatureResults(systemVentilationRoute);

                Assert.That(resultantTemperatureResults.IsComplete, Is.False);
                Assert.That(resultantTemperatureResults.Results, Is.Empty);
                Assert.That(resultantTemperatureResults.Path_Result, Is.Null);
                Assert.That(resultantTemperatureResults.Refusals.Any(x => x.Contains("hour 17")), Is.True, string.Join(" | ", resultantTemperatureResults.Refusals));
            }

            //A room with no series at all.
            SystemVentilationRoute systemVentilationRoute_Missing = Route(new[] { 1, 2 });
            SystemZoneTemperatureResults systemZoneTemperatureResults = new SystemZoneTemperatureResults(0, Hours - 1);
            systemZoneTemperatureResults.Add(systemVentilationRoute_Missing.SystemZoneTemperatureResults.Result(G(1)));
            systemZoneTemperatureResults.Validate(systemVentilationRoute_Missing.Bindings);

            SystemVentilationRoute systemVentilationRoute_Refused = new SystemVentilationRoute(
                systemVentilationRoute_Missing.NoIzamThermalSource, path_TPD, systemVentilationRoute_Missing.SimulationEvidence, systemVentilationRoute_Missing.Bindings, null, systemZoneTemperatureResults, systemZoneTemperatureResults.Refusals, null);

            ResultantTemperatureResults resultantTemperatureResults_Missing = Provider().ResultantTemperatureResults(systemVentilationRoute_Refused);

            Assert.That(resultantTemperatureResults_Missing.IsComplete, Is.False);
            Assert.That(resultantTemperatureResults_Missing.Refusals.Any(x => x.Contains("no zone temperature series came back")), Is.True);
        }

        // ============================================================================ the thermostat writer

        [Test]
        public void EachZoneReceivesItsOwnRoomsSeries_ByGuid_WhateverTheNamesOrOrder()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            //Both zones carry the SAME display name, and TAS enumerates them in the reverse of the plan's order.
            TbdZone zone_2 = new TbdZone(Zone(2), "Bedroom 2", 2);
            TbdZone zone_1 = new TbdZone(Zone(1), "Bedroom 2", 2);
            TbdZone zone_Unbridged = new TbdZone(Zone(7), "Bedroom 2", 1);

            List<string> refusals = new List<string>();
            List<ThermostatBridgeRoom> rooms = global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(TbdBuilding.Create(zone_Unbridged, zone_2, zone_1), thermostatBridgePlan, refusals);

            Assert.That(refusals, Is.Empty, string.Join(" | ", refusals));
            Assert.That(rooms.Select(x => x.Guid_Space), Is.EqualTo(new[] { G(1), G(2) }));

            foreach (KeyValuePair<int, TbdZone> keyValuePair in new Dictionary<int, TbdZone> { { 1, zone_1 }, { 2, zone_2 } })
            {
                AssertHoldsSeries(keyValuePair.Value, keyValuePair.Key);
            }

            //A zone nobody bridged is never touched.
            Assert.That(zone_Unbridged.InternalConditions[0].Thermostat.UpperLimit.Written, Is.EqualTo(0));
            Assert.That(zone_Unbridged.InternalConditions[0].Thermostat.LowerLimit.Written, Is.EqualTo(0));

            foreach (ThermostatBridgeRoom room in rooms)
            {
                Assert.That(room.Count_InternalConditions, Is.EqualTo(2));
                Assert.That(room.Count_Heating, Is.EqualTo(Hours));
                Assert.That(room.Count_Cooling, Is.EqualTo(Hours));
                Assert.That(room.MaxTransferDelta, Is.EqualTo(thermostatBridgePlan.Transfer(room.Guid_Space).MaxRepresentationDelta));
            }
        }

        /// <summary>
        /// Heating AND cooling, on EVERY internal condition, hold the room's achieved series at exactly the hour
        /// it belongs to: TAS slot <c>h</c> is 0-based hour <c>h - 1</c>. The stand-in profile shifts a bulk
        /// 0-based write by one hour exactly as licensed TAS was measured to, so a writer that took the bulk
        /// shortcut would fail here.
        /// </summary>
        private static void AssertHoldsSeries(TbdZone zone, int room)
        {
            foreach (TbdInternalCondition internalCondition in zone.InternalConditions)
            {
                MeasuredYearlyProfile upperLimit = internalCondition.Thermostat.UpperLimit;
                MeasuredYearlyProfile lowerLimit = internalCondition.Thermostat.LowerLimit;

                Assert.That(upperLimit.type, Is.EqualTo(global::TBD.ProfileTypes.ticYearlyProfile));
                Assert.That(lowerLimit.type, Is.EqualTo(global::TBD.ProfileTypes.ticYearlyProfile));
                Assert.That(upperLimit.factor, Is.EqualTo(1f));
                Assert.That(lowerLimit.factor, Is.EqualTo(1f));

                for (int slot = 1; slot <= Hours; slot++)
                {
                    float expected = (float)Temperature(room, slot - 1);

                    if (upperLimit.get_yearlyValues(slot) != expected || lowerLimit.get_yearlyValues(slot) != expected)
                    {
                        Assert.Fail(string.Format("Room {0}, IC '{1}', slot {2}: cooling {3}, heating {4}, expected {5}.", room, internalCondition.name, slot, upperLimit.get_yearlyValues(slot), lowerLimit.get_yearlyValues(slot), expected));
                    }
                }
            }
        }

        [Test]
        public void TheMeasuredBulkWriteWouldShiftEveryHour_WhichIsWhyTheWriterDoesNotUseIt()
        {
            //Pins the stand-in to the licensed measurement, so the alignment test above means something.
            MeasuredYearlyProfile profile = new MeasuredYearlyProfile();
            float[] values = Enumerable.Range(0, Hours).Select(i => 1000f + i).ToArray();

            profile.SetYearlyValues(values);

            Assert.That(profile.get_yearlyValues(1), Is.EqualTo(1001f), "element 0 is ignored");
            Assert.That(profile.get_yearlyValues(Hours), Is.EqualTo(1000f + Hours - 1), "the last value is repeated");
        }

        [Test]
        public void AnInternalConditionSharedWithAnotherZoneIsRefused()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1 }));

            TbdZone zone_1 = new TbdZone(Zone(1), "Studio", 1);
            TbdZone zone_Other = new TbdZone(Zone(8), "Corridor", 0);
            zone_1.InternalConditions[0].Zones.Add(zone_Other.Zone);

            List<string> refusals = new List<string>();
            global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(TbdBuilding.Create(zone_1, zone_Other), thermostatBridgePlan, refusals);

            Assert.That(refusals.Single(), Does.Contain("also assigned to another zone"));
            Assert.That(zone_1.InternalConditions[0].Thermostat.UpperLimit.Written, Is.EqualTo(0), "Nothing is written into a shared thermostat.");
        }

        [Test]
        public void AMissingOrDuplicatedZoneInTheCopyIsRefused()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            List<string> refusals = new List<string>();
            List<ThermostatBridgeRoom> rooms = global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(
                TbdBuilding.Create(new TbdZone(Zone(1), "A", 1), new TbdZone(Zone(1).ToLowerInvariant(), "B", 1)),
                thermostatBridgePlan,
                refusals);

            Assert.That(refusals.Count, Is.EqualTo(2), string.Join(" | ", refusals));
            Assert.That(refusals.Any(x => x.Contains("more than one zone")), Is.True);
            Assert.That(refusals.Any(x => x.Contains("has no zone")), Is.True);
            Assert.That(rooms, Is.Empty);
        }

        [Test]
        public void AThermostatThatWouldNotHoldTheAirIsRefused_NotReconfigured()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1 }));

            TbdZone zone = new TbdZone(Zone(1), "Studio", 1);
            zone.InternalConditions[0].Thermostat.radiantProportion = 0.5f;

            List<string> refusals = new List<string>();
            global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(TbdBuilding.Create(zone), thermostatBridgePlan, refusals);

            Assert.That(refusals.Single(), Does.Contain("would not hold the air"));
            Assert.That(zone.InternalConditions[0].Thermostat.radiantProportion, Is.EqualTo(0.5f));
        }

        [Test]
        public void AValueTasDoesNotKeepIsAWriteFailure()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1 }));

            TbdZone zone = new TbdZone(Zone(1), "Studio", 1);
            zone.InternalConditions[0].Thermostat.LowerLimit.DroppedSlot = 4001;

            List<string> refusals = new List<string>();
            List<ThermostatBridgeRoom> rooms = global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(TbdBuilding.Create(zone), thermostatBridgePlan, refusals);

            Assert.That(refusals.Single(), Does.Contain("heating thermostat").And.Contain("hour 4000"));
            Assert.That(rooms.Single().Count_Heating, Is.EqualTo(Hours - 1));
            Assert.That(rooms.Single().Count_Cooling, Is.EqualTo(Hours));
        }

        // ================================================================================= the TSD reader

        [Test]
        public void TheSecondTsdIsReadByZoneGuid_AndTheAchievedAirIsChecked()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            //Same name twice, reverse order, and an unbridged zone first.
            global::TSD.BuildingData buildingData = TsdBuilding(
                TsdZone(Zone(7), "Bedroom 2", Constant(21.0), Constant(21.0)),
                TsdZone(Zone(2), "Bedroom 2", Air(2, 0.0), Resultant(2)),
                TsdZone(Zone(1).Trim('{', '}').ToLowerInvariant(), "Bedroom 2", Air(1, 0.01), Resultant(1)));

            List<string> refusals = new List<string>();

            List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(buildingData, thermostatBridgePlan, null, 0.5, refusals);

            Assert.That(refusals, Is.Empty, string.Join(" | ", refusals));
            Assert.That(results.Select(x => x.Guid_Space), Is.EqualTo(new[] { G(1), G(2) }));

            foreach (ResultantTemperatureResult result in results)
            {
                int room = result.Guid_Space == G(1) ? 1 : 2;

                Assert.That(result.IsComplete, Is.True, result.Refusal());
                Assert.That(result.TryGetValue(0, out double first), Is.True);
                Assert.That(first, Is.EqualTo((double)(float)(Temperature(room, 0) + 0.5)));
                Assert.That(result.TryGetValue(Hours - 1, out double last), Is.True);
                Assert.That(last, Is.EqualTo((double)(float)(Temperature(room, Hours - 1) + 0.5)));
                Assert.That(result.TryGetValue(Hours, out _), Is.False);
            }
        }

        [Test]
        public void AirThatDidNotFollowTheImposedSeriesRefusesTheRoom()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1 }));

            float[] air = Air(1, 0.0);
            air[6000] += 0.8f;

            List<string> refusals = new List<string>();
            global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(TsdBuilding(TsdZone(Zone(1), "Studio", air, Resultant(1))), thermostatBridgePlan, null, 0.5, refusals);

            Assert.That(refusals.Single(), Does.Contain("hour 6000").And.Contain("not the Systems building"));
        }

        [Test]
        public void AnIncompleteResultantTemperatureIsRefused_AndNothingIsHandedBack()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            float[] shortResultant = Resultant(2).Take(Hours - 1).ToArray();
            float[] nanResultant = Resultant(1);
            nanResultant[8000] = float.NaN;

            foreach (float[][] pair in new[] { new[] { Resultant(1), shortResultant }, new[] { nanResultant, Resultant(2) } })
            {
                List<string> refusals = new List<string>();
                List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(
                    TsdBuilding(TsdZone(Zone(1), "A", Air(1, 0), pair[0]), TsdZone(Zone(2), "B", Air(2, 0), pair[1])),
                    thermostatBridgePlan,
                    null,
                    0.5,
                    refusals);

                ResultantTemperatureResults resultantTemperatureResults = new ResultantTemperatureResults(
                    TPD.ThermostatBridge.Method, 0, Hours - 1, new[] { G(1), G(2) }, results, @"C:\TasOut\bridge.tsd", null, refusals, null);

                Assert.That(resultantTemperatureResults.IsComplete, Is.False);
                Assert.That(resultantTemperatureResults.Results, Is.Empty);
                Assert.That(resultantTemperatureResults.Result(G(1)), Is.Null);
                Assert.That(resultantTemperatureResults.Path_Result, Is.Null);
                Assert.That(resultantTemperatureResults.Refusals, Is.Not.Empty);
            }
        }

        /// <summary>
        /// SAM_Tas#72 follow-up: the bridge reads its two series DAY-MAJOR - every bridged zone's resultant and dry
        /// bulb for day 1, then day 2 - and never zone by zone through <c>GetAnnualZoneResult</c> (the stand-in does
        /// not answer it). The values are those of the whole-year series, in plan order, each zone read once.
        /// </summary>
        [Test]
        public void TheSecondTsdIsReadDayMajor_AndGivesTheWholeYearSeries()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            List<(string guid, int day, int param)> calls = new List<(string, int, int)>();

            //Room 2 first and an unbridged zone in the TSD: the reader asks only for bridged zones.
            global::TSD.BuildingData buildingData = TsdBuilding(
                TsdZone(Zone(2), "B", Air(2, 0), Resultant(2), calls),
                TsdZone(Zone(7), "X", Constant(21.0), Constant(21.0), calls),
                TsdZone(Zone(1), "A", Air(1, 0), Resultant(1), calls));

            List<string> refusals = new List<string>();
            List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(buildingData, thermostatBridgePlan, null, 0.5, refusals);

            Assert.That(refusals, Is.Empty, string.Join(" | ", refusals));

            //365 days x 2 bridged zones x 2 arrays, in day order; within a day, zone by zone in PLAN order
            //(room 1, then room 2), resultant then dry bulb.
            Assert.That(calls, Has.Count.EqualTo(365 * 2 * 2));
            Assert.That(calls.Select(x => x.day), Is.Ordered);
            Assert.That(calls.Any(x => x.guid == Zone(7)), Is.False);
            Assert.That(calls.Take(4), Is.EqualTo(new[]
            {
                (Zone(1), 1, (int)global::TSD.tsdZoneArray.resultantTemp),
                (Zone(1), 1, (int)global::TSD.tsdZoneArray.dryBulbTemp),
                (Zone(2), 1, (int)global::TSD.tsdZoneArray.resultantTemp),
                (Zone(2), 1, (int)global::TSD.tsdZoneArray.dryBulbTemp),
            }));

            //Every hour is the whole-year series' value, widened exactly from float as the annual read did.
            foreach (ResultantTemperatureResult result in results)
            {
                int room = result.Guid_Space == G(1) ? 1 : 2;
                float[] resultant = Resultant(room);

                Assert.That(result.IsComplete, Is.True, result.Refusal());
                for (int hour = 0; hour < Hours; hour++)
                {
                    Assert.That(result.TryGetValue(hour, out double value), Is.True);
                    Assert.That(value, Is.EqualTo((double)resultant[hour]), "hour " + hour);
                }
            }
        }

        /// <summary>
        /// The bridge's full-year guard. TSD answers every day 1..365 for a part-year simulation too and pads the
        /// days it did not simulate with -1 - so, read without the guard, a days 1..2 file passes every length check
        /// the reader has and hands back 8712 hours of -1 degC as the room's resultant temperature. The
        /// SimulationData overload (the one Create.ThermostatBridge calls) refuses it from the stated day range,
        /// before building data is even asked for; 1..365 reads as before.
        /// </summary>
        [Test]
        public void APartYearSecondTsdIsRefused_FromItsDayRange_BeforeAnythingIsRead()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1 }));

            //What TAS writes for a days 1..2 simulation: 48 real hours, then -1.
            float[] padded(float[] series) => series.Select((x, h) => h < 48 ? x : -1f).ToArray();
            Func<global::TSD.BuildingData> building = () => TsdBuilding(TsdZone(Zone(1), "A", padded(Air(1, 0)), padded(Resultant(1))));

            //Without the guard the padding is not detectable by length: 8760 values, all finite.
            List<string> refusals_Unguarded = new List<string>();
            List<ResultantTemperatureResult> unguarded = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(building(), thermostatBridgePlan, null, 1000, refusals_Unguarded);
            Assert.That(refusals_Unguarded, Is.Empty, "the achieved-air tolerance is opened wide to isolate the length checks");
            Assert.That(unguarded.Single().IsComplete, Is.True);
            Assert.That(unguarded.Single().TryGetValue(Hours - 1, out double last) && last == -1.0, Is.True);

            foreach ((int firstDay, int lastDay) in new[] { (1, 2), (1, 364), (2, 365), (0, 365), (1, 366) })
            {
                int count_GetBuildingData = 0;
                List<string> refusals = new List<string>();
                List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(
                    TsdSimulation(firstDay, lastDay, building(), () => count_GetBuildingData++), thermostatBridgePlan, null, 1000, refusals);

                string label = string.Format("days {0}..{1}", firstDay, lastDay);
                Assert.That(results, Is.Empty, label);
                Assert.That(refusals.Single(), Does.Contain(string.Format("days {0}..{1}", firstDay, lastDay)).And.Contain("not the full year 1..365"), label);
                Assert.That(count_GetBuildingData, Is.EqualTo(0), label);

                ResultantTemperatureResults resultantTemperatureResults = new ResultantTemperatureResults(TPD.ThermostatBridge.Method, 0, Hours - 1, new[] { G(1) }, results, null, null, refusals, null);
                Assert.That(resultantTemperatureResults.IsComplete, Is.False, label);
            }

            //A full year through the same overload reads as the building-data one does.
            List<string> refusals_Full = new List<string>();
            List<ResultantTemperatureResult> full = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(
                TsdSimulation(1, 365, TsdBuilding(TsdZone(Zone(1), "A", Air(1, 0), Resultant(1)))), thermostatBridgePlan, null, 0.5, refusals_Full);
            Assert.That(refusals_Full, Is.Empty, string.Join(" | ", refusals_Full));
            Assert.That(full.Single().IsComplete, Is.True);

            Assert.That(global::SAM.Analytical.Tas.TPD.Query.FullYearRefusal(1, 365), Is.Null);
        }

        /// <summary>A daily read that throws refuses every readable room - never a partial or zero series.</summary>
        [Test]
        public void ADailyReadThatFailsRefusesEveryReadableRoom()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            //Room 2's zone answers no series at all: its daily read throws.
            List<string> refusals = new List<string>();
            List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(
                TsdBuilding(TsdZone(Zone(1), "A", Air(1, 0), Resultant(1)), TsdZone(Zone(2), "B", null, null)),
                thermostatBridgePlan,
                null,
                0.5,
                refusals);

            Assert.That(results, Has.Count.EqualTo(2));
            Assert.That(results.All(x => !x.IsComplete), Is.True);
            Assert.That(refusals, Has.Count.EqualTo(2));
            Assert.That(refusals, Has.All.Contain("could not be read"));
        }

        [Test]
        public void AZoneMissingFromTheSecondTsdOrAnsweringTwiceIsRefused()
        {
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(new[] { 1, 2 }));

            List<string> refusals = new List<string>();
            List<ResultantTemperatureResult> results = global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(
                TsdBuilding(TsdZone(Zone(1), "A", Air(1, 0), Resultant(1)), TsdZone(Zone(1), "B", Air(1, 0), Resultant(1))),
                thermostatBridgePlan,
                null,
                0.5,
                refusals);

            ResultantTemperatureResults resultantTemperatureResults = new ResultantTemperatureResults(TPD.ThermostatBridge.Method, 0, Hours - 1, new[] { G(1), G(2) }, results, null, null, refusals, null);

            Assert.That(resultantTemperatureResults.IsComplete, Is.False);
            Assert.That(resultantTemperatureResults.Refusals.Any(x => x.Contains("more than one zone")), Is.True);
            Assert.That(resultantTemperatureResults.Refusals.Any(x => x.Contains("has no zone")), Is.True);
        }

        // ============================================================== the completeness gate (COM-free)

        [Test]
        public void TheResultantTemperatureGate_RefusesEveryIncompleteShape()
        {
            Func<int, ResultantTemperatureResult> complete = room => new ResultantTemperatureResult(G(room), Zone(room), 0, Hours - 1, Series(room, 0, Hours - 1), null);

            ResultantTemperatureResults ok = new ResultantTemperatureResults("m", 0, Hours - 1, new[] { G(1), G(2) }, new[] { complete(2), complete(1) }, "x.tsd", null, null, null);
            Assert.That(ok.IsComplete, Is.True, string.Join(" | ", ok.Refusals));
            Assert.That(ok.Results.Select(x => x.Guid_Space), Is.EqualTo(new[] { G(1), G(2) }));
            Assert.That(ok.Path_Result, Is.EqualTo("x.tsd"));

            Dictionary<string, IEnumerable<ResultantTemperatureResult>> shapes = new Dictionary<string, IEnumerable<ResultantTemperatureResult>>
            {
                { "no resultant temperature series came back", new[] { complete(1) } },
                { "which nobody asked for", new[] { complete(1), complete(2), complete(3) } },
                { "two resultant temperature series", new[] { complete(1), complete(2), complete(2) } },
                { "8759 resultant temperature value(s)", new[] { complete(1), new ResultantTemperatureResult(G(2), Zone(2), 0, Hours - 1, Series(2, 0, Hours - 2), null) } },
                { "no resultant temperature at hour 100", new[] { complete(1), new ResultantTemperatureResult(G(2), Zone(2), 0, Hours - 1, Hole(Series(2, 0, Hours), 100), null) } },
                { "at hour 7 is NaN", new[] { complete(1), new ResultantTemperatureResult(G(2), Zone(2), 0, Hours - 1, Series(2, 0, Hours - 1, h => h == 7 ? double.NaN : 20.0), null) } },
                { "covers hours 1..8760", new[] { complete(1), new ResultantTemperatureResult(G(2), Zone(2), 1, Hours, Series(2, 1, Hours), null) } },
            };

            foreach (KeyValuePair<string, IEnumerable<ResultantTemperatureResult>> keyValuePair in shapes)
            {
                ResultantTemperatureResults refused = new ResultantTemperatureResults("m", 0, Hours - 1, new[] { G(1), G(2) }, keyValuePair.Value, "x.tsd", null, null, null);

                Assert.That(refused.IsComplete, Is.False, keyValuePair.Key);
                Assert.That(refused.Results, Is.Empty, keyValuePair.Key);
                Assert.That(refused.Path_Result, Is.Null, keyValuePair.Key);
                Assert.That(refused.Refusals.Any(x => x.Contains(keyValuePair.Key)), Is.True, keyValuePair.Key + " <- " + string.Join(" | ", refused.Refusals));
            }
        }

        private static IndexedDoubles Hole(IndexedDoubles indexedDoubles, int hour)
        {
            //Right count, wrong hours: hour 100 missing and hour 8760 extra.
            IndexedDoubles result = new IndexedDoubles();
            foreach (int index in Enumerable.Range(0, Hours + 1))
            {
                if (index != hour && indexedDoubles.TryGetValue(index, out double value))
                {
                    result[index] = value;
                }
            }

            return result;
        }

        // ================================================================================= the provider seam

        private IResultantTemperatureProvider Provider()
        {
            return new ThermostatBridgeResultantTemperatureProvider(Path.Combine(directory, "bridge.tbd"));
        }

        [Test]
        public void TheProviderSeamExposesOnlyProviderNeutralTypes()
        {
            MethodInfo[] methodInfos = typeof(IResultantTemperatureProvider).GetMethods();

            Assert.That(methodInfos.Length, Is.EqualTo(1));
            Assert.That(methodInfos[0].ReturnType, Is.EqualTo(typeof(ResultantTemperatureResults)));
            Assert.That(methodInfos[0].GetParameters().Select(x => x.ParameterType), Is.EqualTo(new[] { typeof(SystemVentilationRoute) }));

            //Nothing a consumer reads names the bridge: replacing it with a native TAS provider changes no caller.
            foreach (Type type in new[] { typeof(ResultantTemperatureResults), typeof(ResultantTemperatureResult) })
            {
                foreach (PropertyInfo propertyInfo in type.GetProperties())
                {
                    Assert.That(propertyInfo.PropertyType.Name, Does.Not.Contain("Thermostat"), type.Name + "." + propertyInfo.Name);
                }
            }

            Assert.That(typeof(IResultantTemperatureProvider).IsAssignableFrom(typeof(ThermostatBridgeResultantTemperatureProvider)), Is.True);
        }

        [Test]
        public void ACallerHoldingTheSeamGetsARefusalNeverAnException_AndTheSourceIsNeverTouched()
        {
            SystemVentilationRoute systemVentilationRoute = Route(new[] { 1, 2 });
            byte[] source_Before = File.ReadAllBytes(path_TBD_Source);

            //The copy aimed at the source itself (whose TSD sibling is the source's TSD), at a non-TBD path, into a
            //directory that does not exist, and at nothing. Every one refuses BEFORE anything is copied, so no
            //COM object is ever created here.
            foreach (string path in new[] { path_TBD_Source, Path.Combine(directory, "bridge.txt"), Path.Combine(directory, "no such directory", "bridge.tbd"), null })
            {
                IResultantTemperatureProvider resultantTemperatureProvider = new ThermostatBridgeResultantTemperatureProvider(path);

                ResultantTemperatureResults resultantTemperatureResults = resultantTemperatureProvider.ResultantTemperatureResults(systemVentilationRoute);

                Assert.That(resultantTemperatureResults, Is.Not.Null);
                Assert.That(resultantTemperatureResults.IsComplete, Is.False, path);
                Assert.That(resultantTemperatureResults.Results, Is.Empty);
                Assert.That(resultantTemperatureResults.Method, Is.EqualTo(TPD.ThermostatBridge.Method));
                Assert.That(resultantTemperatureResults.Refusals, Is.Not.Empty);
            }

            Assert.That(File.ReadAllBytes(path_TBD_Source), Is.EqualTo(source_Before));
            Assert.That(new ThermostatBridgeResultantTemperatureProvider(Path.Combine(directory, "b.tbd")).ResultantTemperatureResults(null).IsComplete, Is.False);
        }

        // ============================================================================== scaling (structure)

        /// <summary>
        /// The room-binding path walks each TAS document <b>once</b> - one pass over the TBD's zones, one over
        /// the TSD's - and then resolves every room by dictionary probe. Asserted by counting the COM accessor
        /// calls rather than timing, in the manner of <c>SystemVentilationScalingTests</c>: a per-room scan
        /// would call the accessors rooms x zones times.
        /// </summary>
        [Test]
        public void EveryDocumentIsWalkedOnce_HoweverManyRoomsAreBridged()
        {
            int[] rooms = Enumerable.Range(1, 12).ToArray();
            ThermostatBridgePlan thermostatBridgePlan = new ThermostatBridgePlan(Route(rooms));
            Assume.That(thermostatBridgePlan.IsValid, Is.True);

            const int count_Zones = 5000;

            List<TbdZone> zones = new List<TbdZone>();
            List<global::TSD.ZoneData> zoneDatas = new List<global::TSD.ZoneData>();
            for (int i = 0; i < count_Zones; i++)
            {
                int room = i < rooms.Length ? rooms[i] : 10000 + i;
                zones.Add(new TbdZone(Zone(room), "Flat", room < 10000 ? 1 : 0));
                zoneDatas.Add(TsdZone(Zone(room), "Flat", room < 10000 ? Air(room, 0) : null, room < 10000 ? Resultant(room) : null));
            }

            //Bridged zones last, so a linear search per room would be the worst case.
            zones.Reverse();
            zoneDatas.Reverse();

            int count_GetZone = 0;
            List<string> refusals = new List<string>();
            global::SAM.Analytical.Tas.TPD.Modify.WriteThermostatBridge(TbdBuilding.Create(zones.ToArray(), () => count_GetZone++), thermostatBridgePlan, refusals);

            int count_GetZoneData = 0;
            global::SAM.Analytical.Tas.TPD.Query.ReadThermostatBridge(TsdBuilding(zoneDatas.ToArray(), () => count_GetZoneData++), thermostatBridgePlan, null, 0.5, refusals);

            Assert.That(refusals, Is.Empty, string.Join(" | ", refusals));
            Assert.That(count_GetZone, Is.EqualTo(count_Zones + 1), "TBD zones walked once, plus the terminating null.");
            Assert.That(count_GetZoneData, Is.EqualTo(count_Zones + 1), "TSD zones walked once, plus the terminating null.");
            Assert.That(zones.Sum(x => x.GuidReads), Is.LessThanOrEqualTo(count_Zones + 2 * rooms.Length), "Each zone guid read once to index it, plus each bridged IC's own assignment.");
        }

        // ========================================================================= TSD stand-ins and series

        private static float[] Constant(double value)
        {
            return Enumerable.Repeat((float)value, Hours).ToArray();
        }

        /// <summary>The copy's simulated air: the imposed series plus a small offset.</summary>
        private static float[] Air(int room, double offset)
        {
            return Enumerable.Range(0, Hours).Select(h => (float)(Temperature(room, h) + offset)).ToArray();
        }

        private static float[] Resultant(int room)
        {
            return Enumerable.Range(0, Hours).Select(h => (float)(Temperature(room, h) + 0.5)).ToArray();
        }

        /// <summary>
        /// A TSD zone answering its series DAY BY DAY, as the reader asks: day <c>d</c> is the series' hours
        /// <c>(d - 1) * 24</c> onwards, 24 of them or as many as the series still has (a short series gives a short
        /// last day). <c>GetAnnualZoneResult</c> is not answered, so a reader that went back to it fails loudly.
        /// </summary>
        private static global::TSD.ZoneData TsdZone(string guid, string name, float[] dryBulb, float[] resultant, List<(string guid, int day, int param)> calls = null)
        {
            return ComProxy.Create<global::TSD.ZoneData>((methodInfo, args) =>
            {
                switch (methodInfo.Name)
                {
                    case "get_zoneGUID":
                        return guid;
                    case "get_name":
                        return name;
                    case "GetDailyZoneResult":
                        int day = (int)args[0];
                        int param = (int)args[1];
                        calls?.Add((guid, day, param));

                        float[] series = null;
                        if (param == (int)global::TSD.tsdZoneArray.dryBulbTemp) series = dryBulb;
                        if (param == (int)global::TSD.tsdZoneArray.resultantTemp) series = resultant;
                        if (series == null) break;

                        int start = (day - 1) * 24;
                        float[] result = new float[Math.Max(0, Math.Min(24, series.Length - start))];
                        Array.Copy(series, start, result, 0, result.Length);
                        return result;
                }

                throw new NotSupportedException("ZoneData." + methodInfo.Name);
            });
        }

        /// <summary>A second TSD's simulation data: its stated day range and its building.</summary>
        private static global::TSD.SimulationData TsdSimulation(int firstDay, int lastDay, global::TSD.BuildingData buildingData, Action onGetBuildingData = null)
        {
            return ComProxy.Create<global::TSD.SimulationData>((methodInfo, args) =>
            {
                switch (methodInfo.Name)
                {
                    case "get_firstDay":
                        return firstDay;
                    case "get_lastDay":
                        return lastDay;
                    case "GetBuildingData":
                        onGetBuildingData?.Invoke();
                        return buildingData;
                }

                throw new NotSupportedException("SimulationData." + methodInfo.Name);
            });
        }

        private static global::TSD.BuildingData TsdBuilding(params global::TSD.ZoneData[] zoneDatas)
        {
            return TsdBuilding(zoneDatas, null);
        }

        private static global::TSD.BuildingData TsdBuilding(global::TSD.ZoneData[] zoneDatas, Action onGetZoneData)
        {
            return ComProxy.Create<global::TSD.BuildingData>((methodInfo, args) =>
            {
                if (methodInfo.Name == "GetZoneData")
                {
                    onGetZoneData?.Invoke();

                    //1-based, null past the end - what TSD answers.
                    int index = (int)args[0];
                    return index >= 1 && index <= zoneDatas.Length ? zoneDatas[index - 1] : null;
                }

                throw new NotSupportedException("BuildingData." + methodInfo.Name);
            });
        }
    }

    // ============================================================================== TBD stand-ins

    /// <summary>
    /// Implements a large TAS interface with a handler, so a stand-in answers only the members the code under
    /// test is supposed to touch - and throws on any other, so a quiet new dependency fails loudly.
    /// </summary>
    public class ComProxy : DispatchProxy
    {
        private Func<MethodInfo, object[], object> handler;

        public static T Create<T>(Func<MethodInfo, object[], object> handler)
        {
            T result = DispatchProxy.Create<T, ComProxy>();
            ((ComProxy)(object)result).handler = handler;
            return result;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            return handler(targetMethod, args);
        }
    }

    /// <summary>
    /// A yearly profile that behaves as licensed TAS was <b>measured</b> to (PR3 evidence, `yearly` probe):
    /// slots are 1-based 1..8760; the bulk <c>SetYearlyValues</c> ignores element 0, stores element <c>i</c> in
    /// slot <c>i</c> and repeats the last element into any slot the array does not reach.
    /// </summary>
    internal sealed class MeasuredYearlyProfile : global::TBD.profile
    {
        private readonly float[] slots = new float[ThermostatBridgePlan.HoursPerYear + 1];

        /// <summary>Slots written one at a time.</summary>
        public int Written { get; private set; }

        /// <summary>A slot TAS "loses" - to prove a read-back mismatch refuses. 0 for none.</summary>
        public int DroppedSlot { get; set; }

        public float factor { get; set; }
        public float value { get; set; }
        public global::TBD.ProfileTypes type { get; set; } = global::TBD.ProfileTypes.ticHourlyProfile;
        public float setbackValue { get; set; }
        public global::TBD.Profiles profile { get; set; }
        public int useDaylightAdjustment { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string function { get; set; }
        public string units { get; set; }
        public global::TBD.schedule schedule { get; set; }

        public float get_hourlyValues(int index) => throw new NotSupportedException("profile.hourlyValues");

        public void set_hourlyValues(int index, float value) => throw new NotSupportedException("profile.hourlyValues");

        public float get_yearlyValues(int index)
        {
            return index >= 1 && index <= ThermostatBridgePlan.HoursPerYear ? slots[index] : 0;
        }

        public void set_yearlyValues(int index, float value)
        {
            Written++;

            if (index >= 1 && index <= ThermostatBridgePlan.HoursPerYear && index != DroppedSlot)
            {
                slots[index] = value;
            }
        }

        public object GetYearlyValues()
        {
            Array result = Array.CreateInstance(typeof(float), new[] { ThermostatBridgePlan.HoursPerYear }, new[] { 1 });
            for (int i = 1; i <= ThermostatBridgePlan.HoursPerYear; i++)
            {
                result.SetValue(slots[i], i);
            }

            return result;
        }

        public void SetYearlyValues(object values)
        {
            float[] array = (float[])values;
            for (int i = 1; i <= ThermostatBridgePlan.HoursPerYear; i++)
            {
                slots[i] = i < array.Length ? array[i] : array[array.Length - 1];
            }
        }

        public float GetExtremeValue(bool findMax) => throw new NotSupportedException("profile.GetExtremeValue");
    }

    internal sealed class TbdThermostat : global::TBD.Thermostat
    {
        public MeasuredYearlyProfile UpperLimit { get; } = new MeasuredYearlyProfile { profile = global::TBD.Profiles.ticUL };
        public MeasuredYearlyProfile LowerLimit { get; } = new MeasuredYearlyProfile { profile = global::TBD.Profiles.ticLL };

        public string name { get; set; }
        public string description { get; set; }
        public int proportionalControl { get; set; }
        public float controlRange { get; set; }
        public float radiantProportion { get; set; }

        public global::TBD.profile GetProfile(int profile)
        {
            if (profile == (int)global::TBD.Profiles.ticUL) return UpperLimit;
            if (profile == (int)global::TBD.Profiles.ticLL) return LowerLimit;
            return null;
        }

        public float GetMinimumDeadBand() => throw new NotSupportedException("Thermostat.GetMinimumDeadBand");
    }

    internal sealed class TbdInternalCondition : global::TBD.InternalCondition
    {
        public TbdThermostat Thermostat { get; } = new TbdThermostat();

        /// <summary>The zones this condition is assigned to, as <c>GetZone</c> answers them.</summary>
        public List<global::TBD.zone> Zones { get; } = new List<global::TBD.zone>();

        public string name { get; set; }
        public string description { get; set; }
        public int includeSolarInMRT { get; set; }

        public global::TBD.zone GetZone(int index) => index >= 0 && index < Zones.Count ? Zones[index] : null;

        public global::TBD.Thermostat GetThermostat() => Thermostat;

        public float GetUpperLimit() => throw new NotSupportedException();
        public float GetLowerLimit() => throw new NotSupportedException();
        public global::TBD.InternalGain GetInternalGain() => throw new NotSupportedException();
        public global::TBD.dayType GetDayType(int index) => throw new NotSupportedException();
        public global::TBD.Emitter GetCoolingEmitter() => throw new NotSupportedException();
        public global::TBD.Emitter GetHeatingEmitter() => throw new NotSupportedException();
        public int SetDayType(global::TBD.dayType dayType, bool bAdd) => throw new NotSupportedException();
        public string CalculateDataChecksum() => throw new NotSupportedException();
    }

    /// <summary>A TBD zone with its own internal conditions - one normal and one HDD, as SAM exports them.</summary>
    internal sealed class TbdZone
    {
        public TbdZone(string guid, string name, int count_InternalConditions)
        {
            Guid = guid;
            Name = name;

            Zone = ComProxy.Create<global::TBD.zone>((methodInfo, args) =>
            {
                switch (methodInfo.Name)
                {
                    case "get_GUID":
                        GuidReads++;
                        return Guid;
                    case "get_name":
                        return Name;
                    case "GetIC":
                        int index = (int)args[0];
                        return index >= 0 && index < InternalConditions.Count ? InternalConditions[index] : null;
                }

                throw new NotSupportedException("zone." + methodInfo.Name);
            });

            for (int i = 0; i < count_InternalConditions; i++)
            {
                TbdInternalCondition internalCondition = new TbdInternalCondition { name = i == 0 ? name : name + " - HDD" };
                internalCondition.Zones.Add(Zone);
                InternalConditions.Add(internalCondition);
            }
        }

        public string Guid { get; }

        public string Name { get; }

        public int GuidReads { get; private set; }

        public global::TBD.zone Zone { get; }

        public List<TbdInternalCondition> InternalConditions { get; } = new List<TbdInternalCondition>();
    }

    internal static class TbdBuilding
    {
        public static global::TBD.Building Create(params TbdZone[] zones)
        {
            return Create(zones, null);
        }

        public static global::TBD.Building Create(TbdZone[] zones, Action onGetZone)
        {
            return ComProxy.Create<global::TBD.Building>((methodInfo, args) =>
            {
                if (methodInfo.Name == "GetZone")
                {
                    onGetZone?.Invoke();

                    //0-based, null past the end - what TBD answers.
                    int index = (int)args[0];
                    return index >= 0 && index < zones.Length ? zones[index].Zone : null;
                }

                throw new NotSupportedException("Building." + methodInfo.Name);
            });
        }
    }
}
