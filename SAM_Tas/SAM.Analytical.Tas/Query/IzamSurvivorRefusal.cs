// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The no-IZAM refusal decision, stated once so the workflow and every caller agree on it.
        /// <para>
        /// When the IZAM sweep was requested (<paramref name="removeIZAMs"/>) and an IZAM survived it
        /// (<paramref name="izamSurvived"/>), the building did not accept the removal, and the TBD is
        /// <b>not</b> IZAM-free. The frozen Part O Iteration 3 contract makes that a refusal, not a
        /// warning: continuing would save, size and simulate a building still carrying its own
        /// mechanical ventilation, which the explicit Systems route then models a second time - and no
        /// result check downstream can see the double-count. The caller fails closed.
        /// </para>
        /// <para>
        /// Returns the refusal sentence, or null when there is nothing to refuse - either the sweep was
        /// not requested (an ordinary IZAM-bearing run, which is none of this decision's business) or it
        /// ran and the building is clean. The COM half - asking <c>Building.GetIZAM(0)</c> whether one
        /// survived - stays in <see cref="WorkflowCalculator"/>, which owns the document session; what
        /// lives here is the decision that answer forces, which is exactly the part a test can pin
        /// without licensed TAS.
        /// </para>
        /// </summary>
        /// <param name="removeIZAMs">Whether the run asked for the IZAM sweep.</param>
        /// <param name="izamSurvived">Whether an IZAM is still present after it.</param>
        /// <returns>The refusal when the no-IZAM contract is broken; otherwise null.</returns>
        public static string IzamSurvivorRefusal(bool removeIZAMs, bool izamSurvived)
        {
            if (!removeIZAMs || !izamSurvived)
                return null;

            return "Removing IZAMs: an IZAM survived the sweep, so this TBD is NOT IZAM-free. The run refuses to continue - saving, sizing and simulation would proceed on a building still carrying its own mechanical ventilation, and the explicit Systems route would model that ventilation twice.";
        }
    }
}
