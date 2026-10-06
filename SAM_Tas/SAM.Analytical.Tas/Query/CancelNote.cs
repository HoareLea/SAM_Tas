// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Stage names whose TAS COM call runs for minutes and cannot be interrupted once entered. Matched as
        /// case-insensitive substrings of the <c>Updating</c> description raised by
        /// <see cref="WorkflowCalculator"/>, so they survive wording changes like "Simulating Model" or
        /// "Calculating Unmet Hours".
        /// </summary>
        private static readonly string[] UninterruptibleSteps =
        {
            "Shading",
            "Sizing",
            "Simulating",
            "Importing gbXML",
            "Adding Results",
            "Unmet Hours",
            "Design Loads",

            // Not raised by WorkflowCalculator: this is the inline COM pre-step that WorkflowTBD and the Revit
            // Simulate command run themselves before handing over. It belongs in the same list because it is
            // the same kind of stage - one long COM call with nowhere to observe a cancel.
            "Converting to TBD",
        };

        /// <summary>
        /// The note to show under a progress dialog's main line while <paramref name="description"/> is the
        /// stage in progress. Cancellation of a <see cref="WorkflowCalculator"/> run is only ever observed
        /// between stages, so this says so plainly, and calls out the long stages (shading, sizing, simulation)
        /// where the wait after clicking Cancel can be minutes rather than seconds.
        /// <para>
        /// Lives here rather than beside a single UI caller because every front end running this workflow -
        /// Grasshopper and Revit - needs the same wording and the same stage list, and the list is knowledge
        /// about the workflow rather than about any one dialog.
        /// </para>
        /// </summary>
        /// <param name="description">The current stage, or null before the first stage begins.</param>
        public static string CancelNote(string description)
        {
            if (!string.IsNullOrWhiteSpace(description))
            {
                foreach (string uninterruptibleStep in UninterruptibleSteps)
                {
                    if (description.IndexOf(uninterruptibleStep, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return "Cannot cancel during '" + description + "' - this stage may run for several minutes.";
                    }
                }
            }

            return "Cancel takes effect once the current stage finishes - it cannot interrupt one in progress.";
        }
    }
}
