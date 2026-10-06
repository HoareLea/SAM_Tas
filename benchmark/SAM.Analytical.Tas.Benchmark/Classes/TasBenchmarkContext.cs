// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using SAM.Analytical.Benchmark;

namespace SAM.Analytical.Tas.Benchmark
{
    /// <summary>
    /// Everything the <see cref="Modify.ToBenchmark(AnalyticalModel, TasBenchmarkContext)"/> mapping
    /// needs that cannot be derived from the source <see cref="AnalyticalModel"/> alone: the
    /// engine-neutral SAM results the TAS route produced (the model-level
    /// <see cref="AnalyticalModelSimulationResult"/> carrying annual energies and coincident peaks,
    /// and the per-space <see cref="SpaceSimulationResult"/> list carrying peak loads, peak hours and
    /// unmet hours), plus the run provenance (both model hashes computed by the B1a helpers, weather
    /// identity/hash, TAS engine version, commits, timing and state).
    /// <para>
    /// The context is engine-artefact-free: it holds already-read SAM result objects and pre-computed
    /// hashes, never a TAS <c>TBD</c>/<c>TSD</c> handle, COM object or working directory. That keeps
    /// the mapping a pure function of SAM-neutral inputs and lets it be exercised with no TAS install.
    /// </para>
    /// <para>
    /// Unit note (mirrors the SAM_Tas GH results reader): the TAS
    /// <see cref="AnalyticalModelSimulationResult"/> stores annual consumption in <b>Wh</b> and model
    /// peak loads in <b>W</b>; the mapping converts these to the schema's canonical <b>kWh</b>/<b>kW</b>
    /// (÷1000). Per-space loads are already in <b>W</b> and are emitted unscaled.
    /// </para>
    /// </summary>
    public sealed class TasBenchmarkContext
    {
        /// <summary>Portable model label (not a filesystem path).</summary>
        public string SourceModelName { get; set; }

        /// <summary>Source SAM model GUID in 32-character lowercase hexadecimal form; null only for a pre-model failure.</summary>
        public string SourceModelGuid { get; set; }

        /// <summary>SHA-256 of the exact input model-file bytes (<c>sha256:</c> + 64 hex); null only when a failure prevented hashing.</summary>
        public string SourceFileHash { get; set; }

        /// <summary>SHA-256 of the canonical neutral SAM model (<c>sha256:</c> + 64 hex); null only when a failure prevented canonicalization.</summary>
        public string CanonicalModelHash { get; set; }

        /// <summary>Canonicalization rules version; <see cref="BenchmarkCanonicalization.CurrentVersion"/> for a v1 producer.</summary>
        public string CanonicalizationVersion { get; set; }

        /// <summary>SAM source revision used for the run (7-64 lowercase hex).</summary>
        public string SamCommit { get; set; }

        /// <summary>Producer repository revision used for the run (7-64 lowercase hex).</summary>
        public string RunnerCommit { get; set; }

        /// <summary>Simulation engine name, e.g. <c>Tas</c>.</summary>
        public string EngineName { get; set; }

        /// <summary>TAS engine version (EDSL Tas product version); null only when unavailable and explained in <see cref="Warnings"/>/<see cref="Notes"/>.</summary>
        public string EngineVersion { get; set; }

        /// <summary>Optional SDK/toolkit version. TAS exposes a single engine version, so this is normally null.</summary>
        public string SdkVersion { get; set; }

        /// <summary>Portable weather identity (station/file identity, not an absolute path).</summary>
        public string WeatherIdentity { get; set; }

        /// <summary>SHA-256 of the exact weather-file bytes (<c>sha256:</c> + 64 hex).</summary>
        public string WeatherHash { get; set; }

        /// <summary>Design-day source used for the run.</summary>
        public DesignDaySource DesignDaySource { get; set; } = DesignDaySource.None;

        /// <summary>ISO 8601 UTC instant the run started/completed.</summary>
        public DateTimeOffset RunTimestampUtc { get; set; }

        /// <summary>Non-negative elapsed wall-clock seconds.</summary>
        public double DurationSeconds { get; set; }

        /// <summary>Run outcome.</summary>
        public RunState State { get; set; } = RunState.Success;

        /// <summary>Deterministically ordered warnings; no machine-specific paths.</summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>Deterministically ordered material assumptions or limitations.</summary>
        public List<string> Notes { get; } = new List<string>();

        /// <summary>
        /// The model-level result supplying annual consumption and coincident peaks (Wh/W as TAS
        /// stores them). Null for a failure document, in which case those metrics are unavailable.
        /// Per-space results are NOT carried here: the mapping reads them from the source model's
        /// adjacency cluster (the TAS route relates each <see cref="SpaceSimulationResult"/> to its
        /// space), which is robust to the TAS zone-GUID keying of a result's own <c>Reference</c>.
        /// </summary>
        public AnalyticalModelSimulationResult ModelResult { get; set; }
    }
}
