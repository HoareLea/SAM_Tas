// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The day-by-day TSD read of Modify.AddResults (the SAM_Tas PR record): the same
    /// values as the zone-by-zone read, asked for day-major; and the SAM zones' cooling peaks taken from those series by
    /// the rule TSD's own GetPeakZoneGroupGains was measured to follow, without asking TSD.
    /// </summary>
    [TestFixture]
    public class TsdDayMajorReadTests
    {
        private static float Value(int zone, int param, int hour) => (float)Math.Sin(zone * 7.1 + param * 3.3 + hour * 0.013) * 30 + 5;

        private static FakeZoneData Zone(int zone, string name = null, IEnumerable<tsdZoneArray> tsdZoneArrays = null)
        {
            FakeZoneData zoneData = new FakeZoneData(name ?? ("Zone " + zone), string.Format("00000000-0000-4000-8000-{0:D12}", zone), zone);
            foreach (tsdZoneArray tsdZoneArray in tsdZoneArrays ?? new[] { tsdZoneArray.occupantSensibleGain, tsdZoneArray.resultantTemp, tsdZoneArray.dryBulbTemp })
            {
                for (int hour = 1; hour <= 8760; hour++)
                {
                    zoneData.Set(hour, tsdZoneArray, Value(zone, (int)tsdZoneArray, hour));
                }
            }

            return zoneData;
        }

        /// <summary>Records the order TSD is asked in.</summary>
        private static ZoneData Recording(FakeZoneData zoneData, int zone, List<(int zone, int day, int param)> calls)
        {
            return ComProxy.Create<ZoneData>((methodInfo, args) =>
            {
                if (methodInfo.Name == "GetDailyZoneResult")
                {
                    calls.Add((zone, (int)args[0], (int)args[1]));
                    return zoneData.GetDailyZoneResult((int)args[0], (int)args[1]);
                }

                throw new NotSupportedException("ZoneData." + methodInfo.Name);
            });
        }

        [Test]
        public void ZoneResultSeries_AsksDayByDay_AndReturnsTheZoneByZoneValues()
        {
            List<(int zone, int day, int param)> calls = new List<(int, int, int)>();
            List<FakeZoneData> zones = Enumerable.Range(1, 3).Select(x => Zone(x)).ToList();
            List<ZoneData> zoneDatas = zones.Select((x, i) => Recording(x, i, calls)).ToList();

            List<Dictionary<tsdZoneArray, float[]>> series = zoneDatas.ZoneResultSeries(1, 365, Analytical.Tas.Query.OverheatingZoneArrays);

            //Every zone and array for a day before the next day: the order that lets TSD decode each day once.
            Assert.That(calls, Has.Count.EqualTo(365 * 3 * 3));
            Assert.That(calls.Select(x => x.day), Is.Ordered);
            Assert.That(calls.Take(9).Select(x => x.zone).Distinct(), Is.EquivalentTo(new[] { 0, 1, 2 }));

            for (int z = 0; z < zones.Count; z++)
            {
                foreach (tsdZoneArray tsdZoneArray in Analytical.Tas.Query.OverheatingZoneArrays)
                {
                    float[] expected = Enumerable.Range(1, 8760).Select(h => zones[z].GetHourlyZoneResult(h, (int)tsdZoneArray)).ToArray();
                    Assert.That(series[z][tsdZoneArray], Is.EqualTo(expected), tsdZoneArray.ToString());
                }
            }
        }

        [Test]
        public void ZoneResultSeries_OverPartOfTheYear_LeavesTheOtherHoursZero_AsOverheatingAlwaysHas()
        {
            FakeZoneData zone = Zone(1);

            float[] series = new ZoneData[] { zone, null }.ZoneResultSeries(10, 11, new[] { tsdZoneArray.resultantTemp })[0][tsdZoneArray.resultantTemp];

            Assert.That(series, Has.Length.EqualTo(8760));
            Assert.That(series.Take(9 * 24), Is.All.EqualTo(0f));
            Assert.That(series.Skip(9 * 24).Take(48), Is.EqualTo(Enumerable.Range(9 * 24 + 1, 48).Select(h => zone.GetHourlyZoneResult(h, (int)tsdZoneArray.resultantTemp))));
            Assert.That(series.Skip(11 * 24), Is.All.EqualTo(0f));
        }

        [Test]
        public void Overheating_FromTheSeries_IsTheZoneReadsResult_AndLeavesTheSeriesAlone()
        {
            FakeZoneData zone = Zone(4);
            Dictionary<tsdZoneArray, float[]> series = new ZoneData[] { zone }.ZoneResultSeries(1, 365, Analytical.Tas.Query.OverheatingZoneArrays)[0];
            float[] dryBulb = (float[])series[tsdZoneArray.dryBulbTemp].Clone();

            Dictionary<Analytical.SpaceSimulationResultParameter, object> fromZone = Analytical.Tas.Query.Overheating(zone, 1, 365);
            Dictionary<Analytical.SpaceSimulationResultParameter, object> fromSeries = Analytical.Tas.Query.Overheating(series[tsdZoneArray.occupantSensibleGain], series[tsdZoneArray.resultantTemp], series[tsdZoneArray.dryBulbTemp]);

            Assert.That(fromSeries, Is.EquivalentTo(fromZone));
            Assert.That((int)fromSeries[Analytical.SpaceSimulationResultParameter.OccupiedHours], Is.GreaterThan(0));
            Assert.That(series[tsdZoneArray.dryBulbTemp], Is.EqualTo(dryBulb));
        }

        // ---- the TSD group-peak rule ----------------------------------------------------------------------------

        [Test]
        public void GroupPeak_SumsInSinglePrecision_InTheOrderGiven()
        {
            const float big = 16777216f; //2^24: adding 1f to it is lost in single precision

            float[] a = { big, big + 2 };
            float[] b = { 1f, 0f };
            float[] c = { 1f, 0f };

            //a, b, c: hour 1 sums to 2^24 (both 1s lost), hour 2 to 2^24 + 2 - the peak is hour 2.
            Assert.That(new[] { a, b, c }.TryGetPeakZoneGroupGain(out int index, out double max), Is.True);
            Assert.That(index, Is.EqualTo(2));
            Assert.That(max, Is.EqualTo((double)(big + 2)));

            //b, c, a: hour 1 sums to 2 + 2^24 exactly, tying hour 2 - the FIRST hour wins.
            Assert.That(new[] { b, c, a }.TryGetPeakZoneGroupGain(out index, out max), Is.True);
            Assert.That(index, Is.EqualTo(1));
            Assert.That(max, Is.EqualTo((double)(big + 2)));
        }

        [Test]
        public void GroupPeak_WithNoPositiveHour_IsZeroAtHourZero_AsTsdReportsIt()
        {
            Assert.That(new[] { new float[8760] }.TryGetPeakZoneGroupGain(out int index, out double max), Is.True);
            Assert.That((index, max), Is.EqualTo((0, 0.0)));

            Assert.That(new[] { Enumerable.Repeat(-5f, 8760).ToArray() }.TryGetPeakZoneGroupGain(out index, out max), Is.True);
            Assert.That((index, max), Is.EqualTo((0, 0.0)));
        }

        [Test]
        public void GroupPeak_WithNothingToSum_IsRefused()
        {
            Assert.That(new List<float[]>().TryGetPeakZoneGroupGain(out int index, out double max), Is.False);
            Assert.That(index, Is.EqualTo(-1));
            Assert.That(max, Is.NaN);

            Assert.That(new[] { new float[24], new float[23] }.TryGetPeakZoneGroupGain(out _, out _), Is.False);
            Assert.That(((IList<float[]>)null).TryGetPeakZoneGroupGain(out _, out _), Is.False);
        }

        // ---- AddResults: the zone peak without asking TSD ----------------------------------------------------------

        private static AdjacencyCluster TwoSpacesInOneZone(out Analytical.Zone zone)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            zone = new Analytical.Zone("Flat");
            adjacencyCluster.AddObject(zone);
            foreach (string name in new[] { "Kitchen", "Bedroom" })
            {
                Space space = new Space(name, new Point3D(0, 0, 0));
                adjacencyCluster.AddObject(space);
                adjacencyCluster.AddRelation(zone, space);
            }

            return adjacencyCluster;
        }

        private static FakeSimulationData Simulation(int firstDay, int lastDay)
        {
            FakeSimulationData simulationData = new FakeSimulationData { firstDay = firstDay, lastDay = lastDay };
            //Spelled out rather than Query.OverheatingZoneArrays, so these two tests also run against the previous build.
            tsdZoneArray[] tsdZoneArrays = { tsdZoneArray.occupantSensibleGain, tsdZoneArray.resultantTemp, tsdZoneArray.dryBulbTemp, tsdZoneArray.coolingLoad };
            simulationData.Building.Zones.Add(Zone(1, "Kitchen", tsdZoneArrays));
            simulationData.Building.Zones.Add(Zone(2, "Bedroom", tsdZoneArrays));
            return simulationData;
        }

        [Test]
        public void AddResults_FullYear_TakesTheZonePeakFromTheSeries_WithoutAskingTsd()
        {
            //FakeBuildingData.GetPeakZoneGroupGains throws: a zone result exists only if TSD was not asked.
            FakeSimulationData simulationData = Simulation(1, 365);
            AdjacencyCluster adjacencyCluster = TwoSpacesInOneZone(out Analytical.Zone zone);

            Analytical.Tas.Modify.AddResults(simulationData, adjacencyCluster);

            ZoneSimulationResult zoneSimulationResult = adjacencyCluster.GetRelatedObjects<ZoneSimulationResult>(zone).Single();

            List<FakeZoneData> zones = simulationData.Building.Zones;
            List<float[]> coolingLoads = zones.Select(z => Enumerable.Range(1, 8760).Select(h => z.GetHourlyZoneResult(h, (int)tsdZoneArray.coolingLoad)).ToArray()).ToList();
            float peak = 0;
            int index = 0;
            for (int h = 0; h < 8760; h++)
            {
                float sum = coolingLoads[0][h] + coolingLoads[1][h];
                if (sum > peak)
                {
                    peak = sum;
                    index = h + 1;
                }
            }

            Assert.That(index, Is.GreaterThan(0));
            Assert.That(zoneSimulationResult.TryGetValue(ZoneSimulationResultParameter.MaxSensibleLoad, out double max), Is.True);
            Assert.That(zoneSimulationResult.TryGetValue(ZoneSimulationResultParameter.MaxSensibleLoadIndex, out int maxIndex), Is.True);
            Assert.That(max, Is.EqualTo((double)peak));
            Assert.That(maxIndex, Is.EqualTo(index));
        }

        [Test]
        public void AddResults_PartOfTheYear_StillAsksTsdForTheZonePeak()
        {
            //Outside a full year the TSD call is kept, as before - here it throws, so TryGetMax refuses and no zone result is made.
            FakeSimulationData simulationData = Simulation(1, 200);
            AdjacencyCluster adjacencyCluster = TwoSpacesInOneZone(out Analytical.Zone zone);

            Analytical.Tas.Modify.AddResults(simulationData, adjacencyCluster);

            Assert.That(adjacencyCluster.GetRelatedObjects<SpaceSimulationResult>(adjacencyCluster.GetSpaces()[0]), Is.Not.Empty);
            Assert.That(adjacencyCluster.GetRelatedObjects<ZoneSimulationResult>(zone), Is.Null.Or.Empty);
        }
    }
}
