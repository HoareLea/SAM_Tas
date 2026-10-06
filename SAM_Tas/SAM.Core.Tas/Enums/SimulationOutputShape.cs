// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Tas
{
    /// <summary>
    /// Where a TAS run puts its results, which decides what counts as evidence that it actually ran.
    /// <para>
    /// The two runs Part O Iteration 3 makes are structurally different and one evidence rule cannot cover
    /// both. A TBD simulation writes a <b>separate</b> TSD, so that file can be deleted beforehand and its
    /// post-run existence, timestamp and size are meaningful. A TPD simulation writes results <b>back into
    /// the document it simulated</b>, so there is no separate output to delete or inspect - deleting it would
    /// destroy the input - and a saved file proves nothing, because <c>Save()</c> writes whether or not any
    /// result was produced.
    /// </para>
    /// </summary>
    public enum SimulationOutputShape
    {
        /// <summary>Not stated. Always a refusal: evidence cannot be judged without knowing the shape.</summary>
        Undefined,

        /// <summary>
        /// The run writes its results to a file distinct from the document it simulated - TBD to TSD. The
        /// output may be deleted before the run, and "this run produced it" is provable from its timestamp
        /// and size.
        /// </summary>
        SeparateOutputFile,

        /// <summary>
        /// The run writes its results back into the document it simulated - TPD. The document must not be
        /// deleted first. A changed timestamp is necessary but <b>not sufficient</b>; success rests on
        /// reading the results back and reconciling them against the expected set.
        /// </summary>
        InPlaceDocument,
    }
}
