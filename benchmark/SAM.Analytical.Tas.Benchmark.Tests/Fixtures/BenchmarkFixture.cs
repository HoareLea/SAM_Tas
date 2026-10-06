// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using SAM.Analytical;
using SAM.Analytical.Benchmark;
using SAM.Core;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.Tas.Benchmark.Tests
{
    /// <summary>
    /// Deterministic fixtures for the TAS producer tests, built entirely from SAM core objects with
    /// no TAS COM: a one-space <see cref="AnalyticalModel"/> (fixed GUID, area and volume), a
    /// model-level <see cref="AnalyticalModelSimulationResult"/> carrying annual energies/peaks in the
    /// TAS storage units (<b>Wh</b>/<b>W</b>), and per-space <see cref="SpaceSimulationResult"/>
    /// objects carrying peak loads, hours, unmet hours and design loads (<b>W</b>). The mapping
    /// converts the model-level Wh/W to the schema's kWh/kW, so the golden magnitudes match the
    /// OpenStudio producer (B1b) exactly — the whole point of the benchmark.
    /// </summary>
    public static class BenchmarkFixture
    {
        public const string TasSource = "SAM.Analytical.Tas";

        public static readonly Guid SpaceGuid = new Guid("cccccccc-0000-0000-0000-000000000001");

        // TAS keys a SpaceSimulationResult's Reference by the TAS zone GUID, NOT the SAM space GUID.
        // The fixture uses a distinct value so the tests prove the mapping reads per-space results via
        // the adjacency-cluster relation, not by matching the reference to the SAM space GUID.
        public const string TasZoneReference = "tas-zone-00000001";

        public const string SpaceName = "Space Single";

        public const double FloorArea = 20.0;

        public const double SpaceVolume = 60.0;

        // TAS storage units: consumption in Wh, model peaks in W. After the mapping's ÷1000 these
        // become 1234.5 kWh / 678.25 kWh and 3.6 kW / 2.4 kW — the same magnitudes as B1b.
        public const double AnnualHeatingWh = 1_234_500.0;

        public const double AnnualCoolingWh = 678_250.0;

        public const double ModelPeakHeatingW = 3600.0;

        public const double ModelPeakCoolingW = 2400.0;

        public const int PeakHeatingHour = 205;

        public const int PeakCoolingHour = 4602;

        // Per-space loads are already W; the mapping emits them unscaled.
        public const double SpacePeakHeatingW = 3600.0;

        public const double SpacePeakCoolingW = 2400.0;

        public const double SpaceDesignHeatingW = 4200.0;

        public const double SpaceDesignCoolingW = 2800.0;

        public const double UnmetHeatingHours = 3.0;

        public const double UnmetCoolingHours = 0.0;

        /// <summary>
        /// One conditioned office space (fixed GUID, 20 m² / 60 m³) with the per-space heating and
        /// cooling results RELATED to it in the adjacency cluster, exactly as the TAS route attaches
        /// them. The results are constructed before the model so the model's deep copy preserves the
        /// space→result relations the mapping reads.
        /// </summary>
        public static AnalyticalModel SingleSpaceModel()
        {
            return BuildModel(SpaceResults());
        }

        /// <summary>
        /// The same one-space model but with a FOREIGN (OpenStudio-sourced) heating result related to
        /// the space BEFORE the TAS results — used to prove the producer selects the TAS result by
        /// source, never a pre-existing non-TAS result attached to the input model.
        /// </summary>
        public static AnalyticalModel SingleSpaceModelWithForeignResultFirst()
        {
            List<SpaceSimulationResult> results = new List<SpaceSimulationResult> { ForeignHeatingResult() };
            results.AddRange(SpaceResults());
            return BuildModel(results);
        }

        /// <summary>
        /// A model whose ONLY per-space result is an OLD TAS-sourced heating result (same source as a
        /// fresh run) and no new results — the case the source filter alone cannot catch. Used to
        /// prove <c>StripPreviousResults</c> removes it so it cannot be emitted or satisfy the gate.
        /// </summary>
        public static AnalyticalModel SingleSpaceModelWithOnlyStaleTasResult()
        {
            SpaceSimulationResult stale = new SpaceSimulationResult(SpaceName, TasSource, "old-tas-zone-ref");
            stale.SetValue(SpaceSimulationResultParameter.LoadType, LoadType.Heating.Text());
            stale.SetValue(SpaceSimulationResultParameter.Load, 88888.0);
            stale.SetValue(SpaceSimulationResultParameter.LoadIndex, 50);
            stale.SetValue(SpaceSimulationResultParameter.DesignLoad, 88888.0);
            return BuildModel(new List<SpaceSimulationResult> { stale });
        }

        /// <summary>An OpenStudio-sourced heating result with a distinct (wrong) load, to be ignored by the TAS producer.</summary>
        public static SpaceSimulationResult ForeignHeatingResult()
        {
            SpaceSimulationResult foreign = new SpaceSimulationResult(SpaceName, "SAM.Analytical.OpenStudio", "os-zone-ref");
            foreign.SetValue(SpaceSimulationResultParameter.LoadType, LoadType.Heating.Text());
            foreign.SetValue(SpaceSimulationResultParameter.Load, 99999.0);
            foreign.SetValue(SpaceSimulationResultParameter.LoadIndex, 100);
            foreign.SetValue(SpaceSimulationResultParameter.DesignLoad, 99999.0);
            foreign.SetValue(SpaceSimulationResultParameter.UnmetHours, 999.0);
            return foreign;
        }

        private static AnalyticalModel BuildModel(IEnumerable<SpaceSimulationResult> resultsToAttach)
        {
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Space space = new Space(SpaceGuid, SpaceName, new Point3D(2.5, 2, 1.5));
            space.SetValue(SpaceParameter.Area, FloorArea);
            space.SetValue(SpaceParameter.Volume, SpaceVolume);
            adjacencyCluster.AddObject(space);

            foreach (SpaceSimulationResult spaceSimulationResult in resultsToAttach)
            {
                adjacencyCluster.AddObject(spaceSimulationResult);
                adjacencyCluster.AddRelation(space, spaceSimulationResult);
            }

            return new AnalyticalModel(
                "Single Box Model",
                "Benchmark producer offline fixture",
                null,
                null,
                adjacencyCluster,
                new MaterialLibrary("Benchmark Material Library"),
                new ProfileLibrary("Benchmark Profile Library"));
        }

        /// <summary>The TAS model-level result (Wh/W), as <c>ToSAM_AnalyticalModelSimulationResult</c> would produce it.</summary>
        public static AnalyticalModelSimulationResult ModelResult()
        {
            AnalyticalModelSimulationResult result = new AnalyticalModelSimulationResult(SpaceName, TasSource, "fixture.tsd");
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionHeating, AnnualHeatingWh);
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionCooling, AnnualCoolingWh);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingLoad, ModelPeakHeatingW);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingHour, PeakHeatingHour);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingLoad, ModelPeakCoolingW);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingHour, PeakCoolingHour);
            result.SetValue(AnalyticalModelSimulationResultParameter.FloorArea, FloorArea);
            result.SetValue(AnalyticalModelSimulationResultParameter.Volume, SpaceVolume);
            return result;
        }

        /// <summary>The per-space heating and cooling results (W), referenced by the TAS zone GUID (as TAS attaches them).</summary>
        public static List<SpaceSimulationResult> SpaceResults()
        {
            string reference = TasZoneReference;

            SpaceSimulationResult heating = new SpaceSimulationResult(SpaceName, TasSource, reference);
            heating.SetValue(SpaceSimulationResultParameter.LoadType, LoadType.Heating.Text());
            heating.SetValue(SpaceSimulationResultParameter.Load, SpacePeakHeatingW);
            heating.SetValue(SpaceSimulationResultParameter.LoadIndex, PeakHeatingHour);
            heating.SetValue(SpaceSimulationResultParameter.DesignLoad, SpaceDesignHeatingW);
            heating.SetValue(SpaceSimulationResultParameter.UnmetHours, UnmetHeatingHours);

            SpaceSimulationResult cooling = new SpaceSimulationResult(SpaceName, TasSource, reference);
            cooling.SetValue(SpaceSimulationResultParameter.LoadType, LoadType.Cooling.Text());
            cooling.SetValue(SpaceSimulationResultParameter.Load, SpacePeakCoolingW);
            cooling.SetValue(SpaceSimulationResultParameter.LoadIndex, PeakCoolingHour);
            cooling.SetValue(SpaceSimulationResultParameter.DesignLoad, SpaceDesignCoolingW);
            cooling.SetValue(SpaceSimulationResultParameter.UnmetHours, UnmetCoolingHours);

            return new List<SpaceSimulationResult> { heating, cooling };
        }

        /// <summary>
        /// The full offline golden document: the one-space model mapped through the TAS-shaped
        /// results and a fixed-provenance context (pinned <see cref="TasBenchmarkContext.RunTimestampUtc"/>
        /// and fixed model-identity literals), so its serialization is byte-stable and can be asserted
        /// against a committed fixture. Model + libraries take fresh random GUIDs each construction,
        /// so the two model-identity provenance fields are pinned rather than derived.
        /// </summary>
        public static BenchmarkDocument GoldenDocument()
        {
            return SingleSpaceModel().ToBenchmark(GoldenContext());
        }

        /// <summary>The golden document produced from a model that also carries a foreign (non-TAS) result.</summary>
        public static BenchmarkDocument DocumentWithForeignResult()
        {
            return SingleSpaceModelWithForeignResultFirst().ToBenchmark(GoldenContext());
        }

        /// <summary>The fixed-provenance context shared by the golden and the foreign-result document.</summary>
        public static TasBenchmarkContext GoldenContext()
        {
            return new TasBenchmarkContext
            {
                SourceModelName = "Single Box Model",
                SourceModelGuid = "cccccccc000000000000000000000009",
                SourceFileHash = BenchmarkHash.ComputeSha256(new byte[] { 1, 2, 3, 4 }),
                CanonicalModelHash = BenchmarkCanonicalJson.ComputeSha256("{\"model\":\"benchmark-golden-fixture\"}"),
                CanonicalizationVersion = BenchmarkCanonicalization.CurrentVersion,
                SamCommit = "0123456789abcdef",
                RunnerCommit = "fedcba9876543210",
                EngineName = "Tas",
                EngineVersion = "9.5.3",
                SdkVersion = null,
                WeatherIdentity = "USA_TEST_Weather",
                WeatherHash = BenchmarkHash.ComputeSha256(new byte[] { 5, 6, 7, 8 }),
                DesignDaySource = DesignDaySource.EmbeddedModel,
                RunTimestampUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero),
                DurationSeconds = 42.0,
                State = RunState.Success,
                ModelResult = ModelResult(),
            };
        }
    }
}
