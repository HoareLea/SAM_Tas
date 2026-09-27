// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using TSD;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// Phase-2 result authority, PR2A (audit B1–B5, SAM documentation/Reporting-Phase2-ResultAuthority.md): the real
    /// <c>Convert.ToSAM_Results(SimulationData)</c> run over managed TSD fakes (<see cref="FakeSimulationData"/>).
    /// <para>
    /// The heating values are Bathroom_2's, read from C:\TasOut\final1b\open.tsd (Leeds TRY): the heating design day
    /// peaks at 1139.796 W at TSD hour 1608 (the design day occupies 1585–1608), the full-year simulation at
    /// 104.010 W at hour 8554 (23 Dec 09:00–10:00), outdoor -2.3 °C / 100 %.
    /// </para>
    /// <para>
    /// Types are qualified: inside this namespace SAM.Analytical.Tas has its own Convert, Create, Query and
    /// SpaceSimulationResultParameter.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TsdPeakAuthorityTests
    {
        private const string ZoneName = "Bathroom_2";
        private const string ZoneGuid = "3f7c1d2e-0000-4000-8000-0000000000b2";
        private const string HeatingDesignDayName = "Leeds_TRY ANN HTG 100% CONDS DB";
        private const string CoolingDesignDayName = "Leeds_TRY ANN CLG 0% CONDS DB=>GRad";

        private const int Hdd_FirstHour = 1585;
        private const int Hdd_PeakHour = 1608;
        private const int Annual_PeakHour = 8554;

        private static void Hdd_Bathroom2(FakeZoneData zoneData)
        {
            zoneData.Set(Hdd_FirstHour, tsdZoneArray.heatingLoad, 1133.679f);
            zoneData.Set(Hdd_PeakHour, tsdZoneArray.heatingLoad, 1139.79614f)
                .Set(Hdd_PeakHour, tsdZoneArray.dryBulbTemp, 16f)
                .Set(Hdd_PeakHour, tsdZoneArray.resultantTemp, 13.8705444f)
                .Set(Hdd_PeakHour, tsdZoneArray.relativeHumidity, 19.6374722f)
                .Set(Hdd_PeakHour, tsdZoneArray.humidityRatio, 0.00220824825f)
                .Set(Hdd_PeakHour, tsdZoneArray.infVentGain, -111.737091f)
                .Set(Hdd_PeakHour, tsdZoneArray.buildingHeatTransfer, -1023.25586f)
                .Set(Hdd_PeakHour, tsdZoneArray.externalConductionOpaque, -4.80297852f);
        }

        private static void Annual_Bathroom2(FakeZoneData zoneData, FakeBuildingData buildingData)
        {
            zoneData.Set(Annual_PeakHour, tsdZoneArray.heatingLoad, 104.009911f)
                .Set(Annual_PeakHour, tsdZoneArray.dryBulbTemp, 16.0009079f)
                .Set(Annual_PeakHour, tsdZoneArray.resultantTemp, 15.93449f)
                .Set(Annual_PeakHour, tsdZoneArray.relativeHumidity, 34.96767f)
                .Set(Annual_PeakHour, tsdZoneArray.humidityRatio, 0.00394329941f)
                .Set(Annual_PeakHour, tsdZoneArray.infVentGain, -93.37398f)
                .Set(Annual_PeakHour, tsdZoneArray.buildingHeatTransfer, -3.15779853f)
                .Set(Annual_PeakHour, tsdZoneArray.externalConductionOpaque, -7.47790527f);

            buildingData.Set(Annual_PeakHour, tsdBuildingArray.externalTemperature, -2.3f)
                .Set(Annual_PeakHour, tsdBuildingArray.externalHumidity, 100f);
        }

        /// <summary>One zone with an HDD and a CDD data set. Nothing is populated; the tests add what they need.</summary>
        private static FakeSimulationData Simulation(out FakeZoneData zone_Annual, out FakeZoneData zone_Hdd, out FakeZoneData zone_Cdd, bool heatingDesignDay = true)
        {
            FakeSimulationData simulationData = new FakeSimulationData();

            zone_Annual = new FakeZoneData(ZoneName, ZoneGuid, 1);
            simulationData.Building.Zones.Add(zone_Annual);

            zone_Hdd = new FakeZoneData(ZoneName, ZoneGuid, 1, Hdd_FirstHour, Hdd_PeakHour);
            if (heatingDesignDay)
            {
                FakeHeatingDesignData heatingDesignData = new FakeHeatingDesignData { name = HeatingDesignDayName, firstDay = 67, lastDay = 68 };
                heatingDesignData.Zones.Add(zone_Hdd);
                simulationData.HeatingDesignDatas.Add(heatingDesignData);
            }

            zone_Cdd = new FakeZoneData(ZoneName, ZoneGuid, 1, 5089, 5112);
            FakeCoolingDesignData coolingDesignData = new FakeCoolingDesignData { name = CoolingDesignDayName, firstDay = 213, lastDay = 214 };
            coolingDesignData.Zones.Add(zone_Cdd);
            simulationData.CoolingDesignDatas.Add(coolingDesignData);

            return simulationData;
        }

        private static FakeSimulationData Bathroom2()
        {
            FakeSimulationData simulationData = Simulation(out FakeZoneData zone_Annual, out FakeZoneData zone_Hdd, out _);
            Hdd_Bathroom2(zone_Hdd);
            Annual_Bathroom2(zone_Annual, simulationData.Building);
            return simulationData;
        }

        private static SpaceSimulationResult Result(IEnumerable<Core.Result> results, LoadType loadType)
        {
            List<SpaceSimulationResult> spaceSimulationResults = results.OfType<SpaceSimulationResult>().Where(x => x.LoadType() == loadType).ToList();
            Assert.That(spaceSimulationResults, Has.Count.LessThanOrEqualTo(1));
            return spaceSimulationResults.FirstOrDefault();
        }

        private static SpaceLoadPeak Peak(SpaceSimulationResult spaceSimulationResult, Analytical.SpaceSimulationResultParameter spaceSimulationResultParameter)
        {
            return spaceSimulationResult.TryGetValue(spaceSimulationResultParameter, out SpaceLoadPeak spaceLoadPeak) ? spaceLoadPeak : null;
        }

        private static double Legacy(SpaceSimulationResult spaceSimulationResult, Analytical.SpaceSimulationResultParameter spaceSimulationResultParameter)
        {
            Assert.That(spaceSimulationResult.TryGetValue(spaceSimulationResultParameter, out double value), Is.True, spaceSimulationResultParameter.ToString());
            return value;
        }

        private static List<Core.Result> Convert(FakeSimulationData simulationData)
        {
            return Analytical.Tas.Convert.ToSAM_Results(simulationData);
        }

        // ---- B3: both peaks survive, independently ----------------------------------------------------------------

        [Test]
        public void B3_DesignDayAndAnnualHeatingPeaks_AreBothKept()
        {
            SpaceSimulationResult heating = Result(Convert(Bathroom2()), LoadType.Heating);

            SpaceLoadPeak designDay = Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak);
            SpaceLoadPeak annual = Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak);

            Assert.That(designDay, Is.Not.Null);
            Assert.That(annual, Is.Not.Null);
            Assert.That(designDay.Basis, Is.EqualTo(LoadPeakBasis.DesignDay));
            Assert.That(annual.Basis, Is.EqualTo(LoadPeakBasis.AnnualSimulation));
            Assert.That(designDay.Load, Is.EqualTo(1139.796).Within(0.001));
            Assert.That(annual.Load, Is.EqualTo(104.010).Within(0.001));
            Assert.That(designDay.DesignDayName, Is.EqualTo(HeatingDesignDayName));
            Assert.That(annual.DesignDayName, Is.Null);
        }

        /// <summary>TSD conversion -> AddResults onto a model -> saved as JSON text -> reopened.</summary>
        [Test]
        public void B3_BothPeaks_SurvivePersistAndReopen()
        {
            Space space = new Space(ZoneName, new Point3D(0, 0, 0));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(space);
            Analytical.Tas.Modify.AddResults(Bathroom2(), adjacencyCluster);

            string json = new AnalyticalModel("Model", null, null, null, adjacencyCluster).ToJsonObject().ToJsonString();
            AnalyticalModel reopened = new AnalyticalModel(JsonNode.Parse(json) as JsonObject);

            Space space_Reopened = reopened.AdjacencyCluster.GetSpaces().Single();
            List<SpaceSimulationResult> results = reopened.AdjacencyCluster.GetRelatedObjects<SpaceSimulationResult>(space_Reopened);
            SpaceSimulationResult heating = Result(results, LoadType.Heating);

            SpaceLoadPeak designDay = Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak);
            SpaceLoadPeak annual = Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak);
            Assert.That(designDay.Load, Is.EqualTo(1139.796).Within(0.001));
            Assert.That(designDay.HourOfDay, Is.EqualTo(23));
            Assert.That(designDay.HourOfYear, Is.Null);
            Assert.That(annual.Load, Is.EqualTo(104.010).Within(0.001));
            Assert.That(annual.HourOfYear, Is.EqualTo(Annual_PeakHour - 1));
            Assert.That(annual.OutdoorDryBulbTemperature, Is.EqualTo(-2.3).Within(1e-6));
        }

        // ---- B1: an annual heating winner stays a heating result ------------------------------------------------

        [Test]
        public void B1_AnnualHeatingAboveTheDesignDay_GovernsTheHeatingResult()
        {
            FakeSimulationData simulationData = Simulation(out FakeZoneData zone_Annual, out FakeZoneData zone_Hdd, out _);
            zone_Hdd.Set(Hdd_PeakHour, tsdZoneArray.heatingLoad, 50f).Set(Hdd_PeakHour, tsdZoneArray.dryBulbTemp, 21f);
            Annual_Bathroom2(zone_Annual, simulationData.Building);

            List<Core.Result> results = Convert(simulationData);
            SpaceSimulationResult heating = Result(results, LoadType.Heating);

            Assert.That(heating.SizingMethod(), Is.EqualTo(SizingMethod.Simulation));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.Load), Is.EqualTo(104.010).Within(0.001));
            Assert.That(heating.GetValue<int>(Analytical.SpaceSimulationResultParameter.LoadIndex), Is.EqualTo(Annual_PeakHour));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.DryBulbTempearture), Is.EqualTo(16.0009).Within(0.001), "room state at the annual peak, not the design day's");
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.InfiltrationGain), Is.EqualTo(-93.374).Within(0.001));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.DesignDayTemperature), Is.EqualTo(-2.3).Within(1e-6));

            Assert.That(Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak).Load, Is.EqualTo(50).Within(1e-6));
            Assert.That(Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak).Load, Is.EqualTo(104.010).Within(0.001));

            //And the cooling result is untouched by the heating branch.
            SpaceSimulationResult cooling = Result(results, LoadType.Cooling);
            Assert.That(cooling.SizingMethod(), Is.EqualTo(SizingMethod.CDD));
            Assert.That(Peak(cooling, Analytical.SpaceSimulationResultParameter.AnnualPeak).Load, Is.EqualTo(0));
        }

        [Test]
        public void B1_NoHeatingDesignDay_StillGivesTheAnnualHeatingResult()
        {
            FakeSimulationData simulationData = Simulation(out FakeZoneData zone_Annual, out _, out _, heatingDesignDay: false);
            Annual_Bathroom2(zone_Annual, simulationData.Building);

            SpaceSimulationResult heating = Result(Convert(simulationData), LoadType.Heating);

            Assert.That(heating, Is.Not.Null);
            Assert.That(heating.SizingMethod(), Is.EqualTo(SizingMethod.Simulation));
            Assert.That(Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak), Is.Null, "no design day ran: unavailable, not zero");
            Assert.That(Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak).Load, Is.EqualTo(104.010).Within(0.001));
        }

        // ---- B2: zero is zero, missing is missing ----------------------------------------------------------------

        [Test]
        public void B2_ZeroPeak_IsARealZero_WithNoSentinels()
        {
            SpaceSimulationResult cooling = Result(Convert(Bathroom2()), LoadType.Cooling);

            foreach (Analytical.SpaceSimulationResultParameter parameter in new[] { Analytical.SpaceSimulationResultParameter.DesignDayPeak, Analytical.SpaceSimulationResultParameter.AnnualPeak })
            {
                SpaceLoadPeak zero = Peak(cooling, parameter);
                Assert.That(zero, Is.Not.Null, parameter.ToString());
                Assert.That(zero.Load, Is.EqualTo(0));
                Assert.That(zero.HourOfDay, Is.Null);
                Assert.That(zero.HourOfYear, Is.Null);
                Assert.That(zero.DryBulbTemperature, Is.Null);
                Assert.That(zero.RelativeHumidity, Is.Null);
                Assert.That(zero.OutdoorDryBulbTemperature, Is.Null);
                Assert.That(zero.Components, Is.Empty);
                Assert.That(zero.ToJsonObject().ToJsonString(), Does.Not.Contain("-1"));
            }

            Assert.That(Peak(cooling, Analytical.SpaceSimulationResultParameter.DesignDayPeak).DesignDayName, Is.EqualTo(CoolingDesignDayName));
        }

        /// <summary>
        /// A value of exactly -1 at a real peak hour is a value. (The fake answers -1 only outside its hours, as TAS
        /// does; here -1 W is set explicitly at the peak.)
        /// </summary>
        [Test]
        public void B2_AGenuineMinusOneAtAPeak_IsKept()
        {
            FakeSimulationData simulationData = Bathroom2();
            simulationData.Building.Zones[0].Set(Annual_PeakHour, tsdZoneArray.externalConductionGlazing, -1f);

            SpaceLoadPeak annual = Peak(Result(Convert(simulationData), LoadType.Heating), Analytical.SpaceSimulationResultParameter.AnnualPeak);
            Assert.That(annual.TryGetComponent(LoadPeakComponent.ExternalConductionGlazing, out double glazing), Is.True);
            Assert.That(glazing, Is.EqualTo(-1));
        }

        /// <summary>
        /// Compatibility: the legacy values of a zero governing peak are unchanged - still the -1 TAS returns for
        /// index 0, with LoadIndex 0 - so Print RDS and the benchmark read exactly what they did. The sentinel is
        /// confined to those legacy values; the typed peaks above never carry it.
        /// </summary>
        [Test]
        public void B2_Compatibility_LegacyZeroPeakValuesAreUnchanged()
        {
            SpaceSimulationResult cooling = Result(Convert(Bathroom2()), LoadType.Cooling);

            Assert.That(cooling.SizingMethod(), Is.EqualTo(SizingMethod.CDD));
            Assert.That(Legacy(cooling, Analytical.SpaceSimulationResultParameter.Load), Is.EqualTo(-1));
            Assert.That(cooling.GetValue<int>(Analytical.SpaceSimulationResultParameter.LoadIndex), Is.EqualTo(0));
            Assert.That(Legacy(cooling, Analytical.SpaceSimulationResultParameter.DryBulbTempearture), Is.EqualTo(-1));
        }

        // ---- B4: one time convention ---------------------------------------------------------------------------

        [Test]
        public void B4_TasOneBasedHours_BecomeZeroBased_AndOnlyTheAnnualPeakHasADate()
        {
            SpaceSimulationResult heating = Result(Convert(Bathroom2()), LoadType.Heating);
            SpaceLoadPeak designDay = Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak);
            SpaceLoadPeak annual = Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak);

            //Annual: TSD 8554 = 23 Dec 09:00-10:00 = SAM hour of year 8553.
            Assert.That(annual.HourOfYear, Is.EqualTo(8553));
            Assert.That(annual.HourOfDay, Is.EqualTo(9));
            Assert.That(annual.TryGetDateTime(2018, out DateTime dateTime), Is.True);
            Assert.That(dateTime, Is.EqualTo(new DateTime(2018, 12, 23, 9, 0, 0)));

            //Design day: TSD 1608 is the design day's last hour (23:00-24:00), placed at an internal slot ("8 Mar").
            Assert.That(designDay.HourOfDay, Is.EqualTo(23));
            Assert.That(designDay.HourOfYear, Is.Null);
            Assert.That(designDay.TryGetDateTime(2018, out _), Is.False);

            //Legacy LoadIndex keeps the raw 1-based TSD index of the governing (design-day) peak.
            Assert.That(heating.GetValue<int>(Analytical.SpaceSimulationResultParameter.LoadIndex), Is.EqualTo(Hdd_PeakHour));
        }

        [TestCase(1, 0, 0)]
        [TestCase(24, 23, 23)]
        [TestCase(25, 24, 0)]
        [TestCase(8760, 8759, 23)]
        public void B4_ZeroBasedHour_OfATsdIndex(int tsdIndex, int hourOfYear, int hourOfDay)
        {
            Assert.That(Analytical.Tas.Query.ZeroBasedHourOfYear(tsdIndex), Is.EqualTo(hourOfYear));
            Assert.That(Analytical.Tas.Query.ZeroBasedHourOfDay(tsdIndex), Is.EqualTo(hourOfDay));
        }

        [Test]
        public void B4_TsdIndexZero_IsNotAnHour()
        {
            Assert.That(Analytical.Tas.Query.ZeroBasedHourOfYear(0), Is.Null);
            Assert.That(Analytical.Tas.Query.ZeroBasedHourOfDay(0), Is.Null);
        }

        // ---- B5: the heating peak carries everything the TSD reports --------------------------------------------

        [Test]
        public void B5_HeatingPeaks_CarryHumidity_EveryTerm_AndCloseOnTheLoad()
        {
            SpaceSimulationResult heating = Result(Convert(Bathroom2()), LoadType.Heating);

            foreach (SpaceLoadPeak spaceLoadPeak in new[] { Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak), Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak) })
            {
                Assert.That(spaceLoadPeak.RelativeHumidity, Is.Not.Null);
                Assert.That(spaceLoadPeak.HumidityRatio, Is.Not.Null);
                Assert.That(spaceLoadPeak.ResultantTemperature, Is.Not.Null);

                //Every TSD term, internal gains included, is recorded - here as the 0 the TSD reports.
                foreach (LoadPeakComponent loadPeakComponent in Enum.GetValues(typeof(LoadPeakComponent)).Cast<LoadPeakComponent>().Where(x => x != LoadPeakComponent.Undefined))
                {
                    Assert.That(spaceLoadPeak.TryGetComponent(loadPeakComponent, out _), Is.True, loadPeakComponent.ToString());
                }

                //Heating: load = -(sum of the sensible gains to room air), Tas signs kept.
                double sensible = spaceLoadPeak.Components.Where(x => x.Key != LoadPeakComponent.OccupancyLatent && x.Key != LoadPeakComponent.EquipmentLatent).Sum(x => x.Value);
                Assert.That(-sensible, Is.EqualTo(spaceLoadPeak.Load).Within(0.001));
            }

            SpaceLoadPeak designDay = Peak(heating, Analytical.SpaceSimulationResultParameter.DesignDayPeak);
            Assert.That(designDay.RelativeHumidity, Is.EqualTo(19.637).Within(0.001));
            Assert.That(designDay.OutdoorDryBulbTemperature, Is.Null, "the design-day data has no weather results");

            SpaceLoadPeak annual = Peak(heating, Analytical.SpaceSimulationResultParameter.AnnualPeak);
            Assert.That(annual.RelativeHumidity, Is.EqualTo(34.968).Within(0.001));
            Assert.That(annual.OutdoorRelativeHumidity, Is.EqualTo(100).Within(1e-6));
        }

        // ---- Compatibility: legacy values of a design-day winner ------------------------------------------------

        [Test]
        public void Compatibility_DesignDayGoverning_LegacyValuesAreTheDesignDays()
        {
            SpaceSimulationResult heating = Result(Convert(Bathroom2()), LoadType.Heating);

            Assert.That(heating.SizingMethod(), Is.EqualTo(SizingMethod.HDD));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.Load), Is.EqualTo(1139.796).Within(0.001));
            Assert.That(heating.GetValue<int>(Analytical.SpaceSimulationResultParameter.LoadIndex), Is.EqualTo(Hdd_PeakHour));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.DryBulbTempearture), Is.EqualTo(16));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.ResultantTemperature), Is.EqualTo(13.8705).Within(0.001));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.InfiltrationGain), Is.EqualTo(-111.737).Within(0.001));
            Assert.That(Legacy(heating, Analytical.SpaceSimulationResultParameter.BuildingHeatTransfer), Is.EqualTo(-1023.256).Within(0.001));
            Assert.That(heating.GetValue<string>(Analytical.Tas.SpaceSimulationResultParameter.DesignDayName), Is.EqualTo(HeatingDesignDayName));
            Assert.That(heating.TryGetValue(Analytical.SpaceSimulationResultParameter.DesignDayTemperature, out double _), Is.False, "outdoor state is legacy-recorded only when the annual peak governs");

            //Legacy heating still has no RH; it is on the typed peaks (B5) instead.
            Assert.That(heating.TryGetValue(Analytical.SpaceSimulationResultParameter.RelativeHumidity, out double _), Is.False);
        }

        // ---- Cooling: structure only (B6 needs real evidence) ---------------------------------------------------

        /// <summary>
        /// The cooling path goes through the same code, so its peaks get the same availability and time semantics.
        /// This does NOT verify cooling's sign or closure: that needs a real cooled TSD (audit B6).
        /// </summary>
        [Test]
        public void Cooling_BothPeaksKept_WithTheSameTimeSemantics()
        {
            FakeSimulationData simulationData = Simulation(out FakeZoneData zone_Annual, out _, out FakeZoneData zone_Cdd);
            zone_Cdd.Set(5100, tsdZoneArray.coolingLoad, 800f).Set(5100, tsdZoneArray.dryBulbTemp, 24f);
            zone_Annual.Set(4800, tsdZoneArray.coolingLoad, 650f).Set(4800, tsdZoneArray.dryBulbTemp, 24.5f);
            simulationData.Building.Set(4800, tsdBuildingArray.externalTemperature, 29.4f);

            SpaceSimulationResult cooling = Result(Convert(simulationData), LoadType.Cooling);
            SpaceLoadPeak designDay = Peak(cooling, Analytical.SpaceSimulationResultParameter.DesignDayPeak);
            SpaceLoadPeak annual = Peak(cooling, Analytical.SpaceSimulationResultParameter.AnnualPeak);

            Assert.That(cooling.SizingMethod(), Is.EqualTo(SizingMethod.CDD));
            Assert.That(designDay.Load, Is.EqualTo(800));
            Assert.That(designDay.HourOfDay, Is.EqualTo((5100 - 1) % 24));
            Assert.That(designDay.HourOfYear, Is.Null);
            Assert.That(designDay.DesignDayName, Is.EqualTo(CoolingDesignDayName));
            Assert.That(annual.Load, Is.EqualTo(650));
            Assert.That(annual.HourOfYear, Is.EqualTo(4799));
            Assert.That(annual.OutdoorDryBulbTemperature, Is.EqualTo(29.4).Within(1e-5));
        }
    }
}
