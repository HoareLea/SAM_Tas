// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using TSD;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// The SAM_Tas#72 follow-up (Documentation/evidence/TSD-RESULT-READ-PERFORMANCE.md): the TM59 TSD conversion and the
    /// seven weather arrays read DAY-MAJOR, giving exactly what the zone-by-zone annual reads gave - -1 padding of a
    /// part-year simulation included - and the explicit, opt-in full-year guard a whole-year workflow applies from the
    /// simulation's stated day range, because no series length can show a part year.
    /// <para>
    /// The stand-ins answer as TSD 2.0.0.1 was measured to: annual getters always 8760 values, -1 outside the simulated
    /// hours; daily getters 24 values, -1 outside them. Their annual getters are the reference ("the old read"), and
    /// they count how often they are asked, so a reader that falls back to them is caught.
    /// </para>
    /// </summary>
    [TestFixture]
    public class TsdFullYearReadTests
    {
        private static readonly SpaceDataType[] Tm59SpaceDataTypes = new[] { SpaceDataType.ResultantTemperature, SpaceDataType.OccupantSensibleGain };

        private static float Value(int zone, int param, int hour) => (float)Math.Sin(zone * 5.3 + param * 1.7 + hour * 0.021) * 25 + 3.123456f;

        private static FakeZoneData Zone(int zone, int lastHour = 8760)
        {
            FakeZoneData zoneData = new FakeZoneData("Zone " + zone, string.Format("00000000-0000-4000-8000-{0:D12}", zone), zone, 1, lastHour);
            foreach (tsdZoneArray tsdZoneArray in new[] { tsdZoneArray.resultantTemp, tsdZoneArray.occupantSensibleGain, tsdZoneArray.dryBulbTemp })
            {
                for (int hour = 1; hour <= 8760; hour++)
                {
                    zoneData.Set(hour, tsdZoneArray, Value(zone, (int)tsdZoneArray, hour));
                }
            }

            return zoneData;
        }

        private static FakeSimulationData Simulation(int firstDay, int lastDay, params FakeZoneData[] zones)
        {
            FakeSimulationData simulationData = new FakeSimulationData() { firstDay = firstDay, lastDay = lastDay };
            simulationData.Building.Zones.AddRange(zones);
            return simulationData;
        }

        private static TSDDocument Document(SimulationData simulationData)
        {
            return ComProxy.Create<TSDDocument>((methodInfo, args) =>
            {
                if (methodInfo.Name == "get_SimulationData")
                {
                    return simulationData;
                }

                throw new NotSupportedException("TSDDocument." + methodInfo.Name);
            });
        }

        /// <summary>A space's converted series, as the TM59 calculator reads it off the space.</summary>
        private static double[] Series(Space space, SpaceDataType spaceDataType)
        {
            string name = spaceDataType.Text();
            ParameterSet parameterSet = space.GetParameterSets().Single(x => x.Contains(name));
            return parameterSet.ToJsonArray(name).Select(x => x.GetValue<double>()).ToArray();
        }

        /// <summary>What the zone-by-zone annual read gave, widened float to double.</summary>
        private static double[] Annual(FakeZoneData zoneData, SpaceDataType spaceDataType)
        {
            return ((float[])zoneData.GetAnnualZoneResult((int)spaceDataType.TsdZoneArray().Value)).Select(x => (double)x).ToArray();
        }

        // ================================================================================== TM59 conversion

        [Test]
        public void Tm59Conversion_ReadsDayMajor_AndGivesTheAnnualValues_ZoneForZone()
        {
            FakeZoneData[] zones = new[] { Zone(3), Zone(1), Zone(2) };
            FakeSimulationData simulationData = Simulation(1, 365, zones);

            AdjacencyCluster adjacencyCluster = Analytical.Tas.Convert.ToSAM_AdjacencyCluster(simulationData.Building, Tm59SpaceDataTypes);

            Assert.That(zones.Sum(x => x.AnnualReads), Is.EqualTo(0), "no zone-by-zone annual read");

            List<Space> spaces = adjacencyCluster.GetSpaces();
            Assert.That(spaces.Select(x => x.Name), Is.EquivalentTo(zones.Select(x => x.name)));

            foreach (FakeZoneData zone in zones)
            {
                //Each space carries ITS OWN zone's series (the zones' values all differ), bit for bit the annual ones.
                Space space = spaces.Single(x => x.Name == zone.name);
                foreach (SpaceDataType spaceDataType in Tm59SpaceDataTypes)
                {
                    double[] series = Series(space, spaceDataType);
                    Assert.That(series, Has.Length.EqualTo(8760));
                    Assert.That(series, Is.EqualTo(Annual(zone, spaceDataType)), zone.name + " " + spaceDataType);
                }
            }
        }

        [Test]
        public void Tm59Conversion_KeepsTheOldPath_ForASingleZone_AndTheSpaceNameFilter()
        {
            FakeZoneData zone = Zone(4);

            //The public single-zone conversion still reads the annual getter - it is not part of the TM59 pass.
            Space alone = zone.ToSAM(Tm59SpaceDataTypes);
            Assert.That(zone.AnnualReads, Is.EqualTo(2));
            zone.AnnualReads = 0;

            FakeSimulationData simulationData = Simulation(1, 365, Zone(5), zone);
            AdjacencyCluster adjacencyCluster = Analytical.Tas.Convert.ToSAM_AdjacencyCluster(simulationData.Building, Tm59SpaceDataTypes, null, new[] { zone.name });

            Space filtered = adjacencyCluster.GetSpaces().Single();
            Assert.That(filtered.Name, Is.EqualTo(zone.name));
            foreach (SpaceDataType spaceDataType in Tm59SpaceDataTypes)
            {
                Assert.That(Series(filtered, spaceDataType), Is.EqualTo(Series(alone, spaceDataType)), spaceDataType.ToString());
            }
        }

        /// <summary>
        /// A part-year TSD converts as it always did - the generic conversion is not the place for a full-year rule -
        /// and its padding comes through exactly as the annual getter gave it: 8760 values, -1 past the last simulated
        /// hour. Which is why no length check downstream could ever see the part year.
        /// </summary>
        [Test]
        public void Tm59Conversion_OfAPartYearTsd_GivesTheAnnualPadding_AndIsNotRefused()
        {
            FakeZoneData zone = Zone(6, lastHour: 48);
            FakeSimulationData simulationData = Simulation(1, 2, zone);

            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(Document(simulationData), new TSDConversionSettings() { SpaceDataTypes = new HashSet<SpaceDataType>(Tm59SpaceDataTypes), ConvertWeaterData = false }, out string refusal);

            Assert.That(refusal, Is.Null);
            Space space = analyticalModel.AdjacencyCluster.GetSpaces().Single();
            double[] resultant = Series(space, SpaceDataType.ResultantTemperature);

            Assert.That(resultant, Has.Length.EqualTo(8760));
            Assert.That(resultant.Skip(48), Is.All.EqualTo(-1.0));
            Assert.That(resultant.Take(48), Is.All.Not.EqualTo(-1.0));
            Assert.That(resultant, Is.EqualTo(Annual(zone, SpaceDataType.ResultantTemperature)));
        }

        // ============================================================================ the full-year guard

        [Test]
        public void FullYearRefusal_AcceptsOnlyDays1To365()
        {
            Assert.That(Analytical.Tas.Query.FullYearRefusal(Simulation(1, 365)), Is.Null);

            foreach ((int firstDay, int lastDay) in new[] { (1, 364), (1, 2), (2, 365), (0, 365), (1, 366), (121, 273) })
            {
                string refusal = Analytical.Tas.Query.FullYearRefusal(Simulation(firstDay, lastDay));
                Assert.That(refusal, Does.Contain(string.Format("days {0}..{1}", firstDay, lastDay)).And.Contain("not the full year 1..365"), firstDay + ".." + lastDay);
            }

            Assert.That(Analytical.Tas.Query.FullYearRefusal((SimulationData)null), Does.Contain("no simulation data"));
        }

        /// <summary>
        /// <see cref="TSDConversionSettings.RequireFullYear"/>: a whole-year caller's TM59 conversion accepts 1..365
        /// and refuses a part year - from the stated day range, before a single result is read - while the default
        /// keeps part-year TSDs convertible for every other consumer.
        /// </summary>
        [Test]
        public void RequireFullYear_AcceptsAFullYear_AndRefusesAPartYear_BeforeReadingAnything()
        {
            TSDConversionSettings settings = new TSDConversionSettings() { SpaceDataTypes = new HashSet<SpaceDataType>(Tm59SpaceDataTypes), ConvertWeaterData = true, RequireFullYear = true };

            FakeZoneData zone_Full = Zone(7);
            FakeSimulationData full = Simulation(1, 365, zone_Full);
            AnalyticalModel analyticalModel = Analytical.Tas.Convert.ToSAM(Document(full), settings, out string refusal_Full);
            Assert.That(refusal_Full, Is.Null);
            Assert.That(analyticalModel, Is.Not.Null);
            Assert.That(Series(analyticalModel.AdjacencyCluster.GetSpaces().Single(), SpaceDataType.ResultantTemperature), Is.EqualTo(Annual(zone_Full, SpaceDataType.ResultantTemperature)));

            foreach (int lastDay in new[] { 364, 2 })
            {
                FakeZoneData zone = Zone(8, lastHour: lastDay * 24);
                FakeSimulationData part = Simulation(1, lastDay, zone);

                AnalyticalModel refused = Analytical.Tas.Convert.ToSAM(Document(part), settings, out string refusal);

                Assert.That(refused, Is.Null, "lastDay " + lastDay);
                Assert.That(refusal, Does.Contain(string.Format("days 1..{0}", lastDay)), "lastDay " + lastDay);
                Assert.That(zone.AnnualReads, Is.EqualTo(0));
                Assert.That(part.Building.DailyReads, Is.Empty, "no weather read either");

                //The same file without the opt-in converts, padding and all - partial-year consumers stay supported.
                AnalyticalModel converted = Analytical.Tas.Convert.ToSAM(Document(part), new TSDConversionSettings(settings) { RequireFullYear = false }, out string refusal_Default);
                Assert.That(refusal_Default, Is.Null);
                Assert.That(converted.AdjacencyCluster.GetSpaces(), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void RequireFullYear_IsCopied_AndSerialisedOnlyWhenSet()
        {
            TSDConversionSettings plain = new TSDConversionSettings();
            Assert.That(plain.RequireFullYear, Is.False);
            Assert.That(plain.ToJsonObject().ContainsKey("RequireFullYear"), Is.False, "existing serialised settings are unchanged");

            TSDConversionSettings set = new TSDConversionSettings() { RequireFullYear = true };
            Assert.That(new TSDConversionSettings(set).RequireFullYear, Is.True);

            JsonObject jsonObject = JsonNode.Parse(set.ToJsonObject().ToJsonString()).AsObject();
            Assert.That(new TSDConversionSettings(jsonObject).RequireFullYear, Is.True);
            Assert.That(new TSDConversionSettings(JsonNode.Parse(plain.ToJsonObject().ToJsonString()).AsObject()).RequireFullYear, Is.False);
        }

        /// <summary>The generic reader keeps reading any day range - the full-year rule is not in it.</summary>
        [Test]
        public void ZoneResultSeries_StillReadsAPartYear_AndAnyRange()
        {
            FakeZoneData zone = Zone(9, lastHour: 48);

            float[] days1To2 = new ZoneData[] { zone }.ZoneResultSeries(1, 2, new[] { tsdZoneArray.resultantTemp })[0][tsdZoneArray.resultantTemp];
            Assert.That(days1To2.Take(48), Is.EqualTo(Enumerable.Range(1, 48).Select(h => Value(9, (int)tsdZoneArray.resultantTemp, h))));
            Assert.That(days1To2.Skip(48), Is.All.EqualTo(0f), "hours outside the range read stay 0, as always");

            //Over the whole calendar year it is the annual answer, padding included.
            float[] year = new ZoneData[] { zone }.ZoneResultSeries(1, 365, new[] { tsdZoneArray.resultantTemp })[0][tsdZoneArray.resultantTemp];
            Assert.That(year, Is.EqualTo((float[])zone.GetAnnualZoneResult((int)tsdZoneArray.resultantTemp)));
            Assert.That(year.Skip(48), Is.All.EqualTo(-1f));
        }

        // ========================================================================================== weather

        private static readonly tsdBuildingArray[] WeatherArrays = new[]
        {
            tsdBuildingArray.cloudCover, tsdBuildingArray.diffuseRadiation, tsdBuildingArray.externalHumidity, tsdBuildingArray.externalTemperature,
            tsdBuildingArray.globalRadiation, tsdBuildingArray.windDirection, tsdBuildingArray.windSpeed,
        };

        private static FakeBuildingData WeatherBuilding(int lastHour = 8760)
        {
            FakeBuildingData buildingData = new FakeBuildingData() { LastHour = lastHour };
            foreach (tsdBuildingArray tsdBuildingArray in WeatherArrays)
            {
                for (int hour = 1; hour <= 8760; hour++)
                {
                    buildingData.Set(hour, tsdBuildingArray, (float)Math.Abs(Math.Cos((int)tsdBuildingArray * 0.7 + hour * 0.017)) * 90 + 0.1f);
                }
            }

            return buildingData;
        }

        /// <summary>The WeatherYear the seven annual reads built, which the day-major read must equal.</summary>
        private static string AnnualWeatherYear(FakeBuildingData buildingData, int year)
        {
            Dictionary<global::SAM.Weather.WeatherDataType, List<double>> dictionary = new Dictionary<global::SAM.Weather.WeatherDataType, List<double>>()
            {
                { global::SAM.Weather.WeatherDataType.CloudCover, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.cloudCover) },
                { global::SAM.Weather.WeatherDataType.DiffuseSolarRadiation, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.diffuseRadiation) },
                { global::SAM.Weather.WeatherDataType.RelativeHumidity, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.externalHumidity) },
                { global::SAM.Weather.WeatherDataType.DryBulbTemperature, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.externalTemperature) },
                { global::SAM.Weather.WeatherDataType.GlobalSolarRadiation, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.globalRadiation) },
                { global::SAM.Weather.WeatherDataType.WindDirection, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.windDirection) },
                { global::SAM.Weather.WeatherDataType.WindSpeed, global::SAM.Weather.Tas.Query.AnnualBuildingResult<double>(buildingData, tsdBuildingArray.windSpeed) },
            };

            return global::SAM.Weather.Create.WeatherYear(year, dictionary).ToJsonObject().ToJsonString();
        }

        [Test]
        public void Weather_ReadsTheSevenArraysDayMajor_AndEqualsTheAnnualReads([Values(8760, 48)] int lastHour)
        {
            FakeBuildingData buildingData = WeatherBuilding(lastHour);

            global::SAM.Weather.WeatherYear weatherYear = global::SAM.Weather.Tas.Query.WeatherYear(buildingData, 2018);

            Assert.That(buildingData.AnnualReads, Is.EqualTo(0), "no annual weather read");

            //All seven arrays for a day before the next day.
            Assert.That(buildingData.DailyReads, Has.Count.EqualTo(365 * 7));
            Assert.That(buildingData.DailyReads.Select(x => x.day), Is.Ordered);
            Assert.That(buildingData.DailyReads.Take(7).Select(x => x.param), Is.EquivalentTo(WeatherArrays.Select(x => (int)x)));

            Assert.That(weatherYear.ToJsonObject().ToJsonString(), Is.EqualTo(AnnualWeatherYear(buildingData, 2018)), "lastHour " + lastHour);
        }
    }
}
