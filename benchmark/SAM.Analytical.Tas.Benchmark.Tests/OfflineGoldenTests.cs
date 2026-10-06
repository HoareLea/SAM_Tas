// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Tas.Benchmark.Tests
{
    /// <summary>
    /// Offline producer coverage with no TAS install: the TAS SAM results → B1a benchmark document
    /// mapping (including the Wh→kWh / W→kW model conversion), the route/engine provenance, full
    /// schema validation and byte-stable serialization against a committed golden. The model-level
    /// results are built in TAS storage units (Wh/W) so the mapping's unit conversion is exercised
    /// and the emitted magnitudes match the OpenStudio producer.
    /// </summary>
    [TestFixture]
    public class OfflineGoldenTests
    {
        private static BenchmarkDocument Golden()
        {
            return BenchmarkFixture.GoldenDocument();
        }

        [Test]
        public void Golden_ModelMetrics_ConvertedToKilowattUnits()
        {
            BenchmarkModelResult model = Golden().Model;

            // Wh/W in, kWh/kW out: the mapping divides the TAS model values by 1000.
            AssertAvailable(model.ConsumptionHeating, BenchmarkFixture.AnnualHeatingWh / 1000.0, MetricUnit.KilowattHour);
            AssertAvailable(model.ConsumptionCooling, BenchmarkFixture.AnnualCoolingWh / 1000.0, MetricUnit.KilowattHour);
            AssertAvailable(model.PeakHeatingLoad, BenchmarkFixture.ModelPeakHeatingW / 1000.0, MetricUnit.Kilowatt);
            AssertAvailable(model.PeakHeatingHour, BenchmarkFixture.PeakHeatingHour, MetricUnit.HourOfYear);
            AssertAvailable(model.PeakCoolingLoad, BenchmarkFixture.ModelPeakCoolingW / 1000.0, MetricUnit.Kilowatt);
            AssertAvailable(model.PeakCoolingHour, BenchmarkFixture.PeakCoolingHour, MetricUnit.HourOfYear);
            AssertAvailable(model.FloorArea, BenchmarkFixture.FloorArea, MetricUnit.SquareMetre);
            AssertAvailable(model.Volume, BenchmarkFixture.SpaceVolume, MetricUnit.CubicMetre);
        }

        [Test]
        public void Golden_SpaceMetrics_LoadsInWatts_UnmetZeroIsMeasured()
        {
            BenchmarkDocument document = Golden();
            Assert.That(document.Spaces, Has.Count.EqualTo(1));

            BenchmarkSpaceResult space = document.Spaces.Single();
            Assert.That(space.Guid, Is.EqualTo(BenchmarkFixture.SpaceGuid.ToString("N")));
            Assert.That(space.Name, Is.EqualTo(BenchmarkFixture.SpaceName));
            AssertAvailable(space.Area, BenchmarkFixture.FloorArea, MetricUnit.SquareMetre);
            AssertAvailable(space.Volume, BenchmarkFixture.SpaceVolume, MetricUnit.CubicMetre);

            // Space peaks and design loads are Watts (unscaled), coupled with their hour-of-year.
            AssertAvailable(space.Heating.PeakLoad, BenchmarkFixture.SpacePeakHeatingW, MetricUnit.Watt);
            AssertAvailable(space.Heating.PeakHour, BenchmarkFixture.PeakHeatingHour, MetricUnit.HourOfYear);
            AssertAvailable(space.Heating.DesignLoad, BenchmarkFixture.SpaceDesignHeatingW, MetricUnit.Watt);
            AssertAvailable(space.Cooling.PeakLoad, BenchmarkFixture.SpacePeakCoolingW, MetricUnit.Watt);
            AssertAvailable(space.Cooling.PeakHour, BenchmarkFixture.PeakCoolingHour, MetricUnit.HourOfYear);
            AssertAvailable(space.Cooling.DesignLoad, BenchmarkFixture.SpaceDesignCoolingW, MetricUnit.Watt);

            // A measured zero stays a value of 0 and available; a genuinely non-zero unmet is kept.
            AssertAvailable(space.Heating.UnmetHours, BenchmarkFixture.UnmetHeatingHours, MetricUnit.Hour);
            AssertAvailable(space.Cooling.UnmetHours, BenchmarkFixture.UnmetCoolingHours, MetricUnit.Hour);
        }

        [Test]
        public void ForeignNonTasResults_AreIgnored_OnlyTasValuesEmitted()
        {
            // The model carries an OpenStudio-sourced heating result (wrong load 99999 W) related to
            // the space BEFORE the TAS results. The producer must select the TAS result by source and
            // must not leak the foreign source into provenance.
            BenchmarkDocument document = BenchmarkFixture.DocumentWithForeignResult();

            BenchmarkSpaceResult space = document.Spaces.Single();
            AssertAvailable(space.Heating.PeakLoad, BenchmarkFixture.SpacePeakHeatingW, MetricUnit.Watt);
            AssertAvailable(space.Heating.DesignLoad, BenchmarkFixture.SpaceDesignHeatingW, MetricUnit.Watt);
            AssertAvailable(space.Heating.UnmetHours, BenchmarkFixture.UnmetHeatingHours, MetricUnit.Hour);

            Assert.That(document.Provenance.ResultSources, Is.EqualTo(new[] { BenchmarkFixture.TasSource }),
                "only the TAS result source may appear in provenance");
        }

        [Test]
        public void StalePreviousTasResult_IsStripped_GateFails_AndNotEmitted()
        {
            // The input carries an OLD TAS-sourced heating result (load 88888 W) and nothing fresh —
            // the source filter cannot distinguish it because it shares the TAS source.
            AnalyticalModel model = BenchmarkFixture.SingleSpaceModelWithOnlyStaleTasResult();
            Assert.That(Producer.HasSpaceLoad(model, BenchmarkFixture.TasSource), Is.True, "precondition: the stale result is present before stripping");

            // StripPreviousResults removes it, so a run that produced no new space result fails the gate.
            AnalyticalModel stripped = Producer.StripPreviousResults(model);
            Assert.That(Producer.HasSpaceLoad(stripped, BenchmarkFixture.TasSource), Is.False, "the stale result must not satisfy the success gate");

            // ...and the stale load is never emitted (no fresh model result either → all unavailable).
            TasBenchmarkContext context = BenchmarkFixture.GoldenContext();
            context.ModelResult = null;
            BenchmarkSpaceResult space = stripped.ToBenchmark(context).Spaces.Single();
            Assert.That(space.Heating.PeakLoad.Available, Is.False, "the stale TAS load must not be emitted");
            Assert.That(space.Heating.DesignLoad.Available, Is.False);
        }

        [Test]
        public void Golden_Provenance_IsNativeTasRoute()
        {
            BenchmarkProvenance provenance = Golden().Provenance;

            Assert.That(provenance.Route, Is.EqualTo(BenchmarkRoute.NativeTas));
            Assert.That(provenance.Engine.Kind, Is.EqualTo(EngineKind.Tas));
            Assert.That(provenance.Engine.Name, Is.EqualTo("Tas"));
            Assert.That(provenance.CanonicalizationVersion, Is.EqualTo(BenchmarkCanonicalization.CurrentVersion));
            Assert.That(provenance.ResultSources, Is.EqualTo(new[] { BenchmarkFixture.TasSource }));
            Assert.That(provenance.State, Is.EqualTo(RunState.Success));
        }

        [Test]
        public void Golden_Document_ValidatesWithZeroErrorsAndWarnings()
        {
            BenchmarkValidationResult validation = BenchmarkValidator.Validate(Golden());

            Assert.That(validation.Errors, Is.Empty, "Errors: " + string.Join(" | ", validation.Errors.Select(x => x.ToString())));
            Assert.That(validation.Warnings, Is.Empty, "Warnings: " + string.Join(" | ", validation.Warnings.Select(x => x.ToString())));
            Assert.That(validation.IsValid, Is.True);
        }

        [Test]
        public void Golden_Serializes_AndRoundTrips()
        {
            BenchmarkDocument document = Golden();

            string json = BenchmarkSerializer.Serialize(document);
            Assert.That(json, Does.Contain("\"route\": \"Native-TAS\""));

            BenchmarkDocument reloaded = BenchmarkSerializer.Deserialize(json);
            Assert.That(reloaded.Model.ConsumptionHeating.Value, Is.EqualTo(BenchmarkFixture.AnnualHeatingWh / 1000.0).Within(1e-9));
            Assert.That(reloaded.Spaces.Single().Heating.PeakLoad.Value, Is.EqualTo(BenchmarkFixture.SpacePeakHeatingW).Within(1e-9));
        }

        /// <summary>
        /// The producer's serialized bytes must equal a committed golden fixture, exactly — the
        /// byte-stable contract the whole benchmark rests on. Any drift in the TAS mapping, unit
        /// conversion, unit tokens or serializer layout fails here. Regenerate intentionally with
        /// <c>SAM_BENCHMARK_UPDATE_GOLDEN=1</c> after reviewing the diff.
        /// </summary>
        [Test]
        public void Golden_Bytes_MatchCommittedFixture()
        {
            byte[] produced = BenchmarkSerializer.SerializeToUtf8(Golden());
            string goldenPath = GoldenFixturePath();

            if (Environment.GetEnvironmentVariable("SAM_BENCHMARK_UPDATE_GOLDEN") == "1")
            {
                File.WriteAllBytes(goldenPath, produced);
                TestContext.Out.WriteLine("Rewrote golden fixture: " + goldenPath);
            }

            Assert.That(File.Exists(goldenPath), Is.True, "Committed golden fixture missing: " + goldenPath);

            byte[] committed = File.ReadAllBytes(goldenPath);
            Assert.That(produced, Is.EqualTo(committed),
                "Producer output drifted from the committed golden fixture. If the mapping change was intentional, regenerate with SAM_BENCHMARK_UPDATE_GOLDEN=1 and review the diff.");
        }

        /// <summary>
        /// Resolves the committed golden fixture in the source tree by walking up from the test
        /// output directory to the Tests project root (identified by its Fixtures folder).
        /// </summary>
        private static string GoldenFixturePath()
        {
            DirectoryInfo directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                string fixtures = Path.Combine(directory.FullName, "Fixtures");
                if (File.Exists(Path.Combine(fixtures, "BenchmarkFixture.cs")))
                {
                    return Path.Combine(fixtures, "golden-tas-benchmark.json");
                }

                directory = directory.Parent;
            }

            return Path.GetFullPath(Path.Combine("Fixtures", "golden-tas-benchmark.json"));
        }

        private static void AssertAvailable(MetricValue metric, double expected, MetricUnit unit)
        {
            Assert.That(metric, Is.Not.Null);
            Assert.That(metric.Available, Is.True, "expected available");
            Assert.That(metric.Unit, Is.EqualTo(unit));
            Assert.That(metric.Value, Is.Not.Null);
            Assert.That(metric.Value.Value, Is.EqualTo(expected).Within(1e-9));
        }
    }
}
