// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Core.Tas
{
    /// <summary>What a TAS simulate return string is known to mean.</summary>
    public enum SimulationDiagnosticKind
    {
        /// <summary>TAS returned nothing. Not proof of success on its own.</summary>
        Silent,

        /// <summary>Text measured to accompany a run that produced results - currently <c>"Done"</c>.</summary>
        KnownSuccess,

        /// <summary>Text measured to accompany a failed run.</summary>
        KnownFailure,

        /// <summary>
        /// Text that has not been observed on the licensed machine. Preserved as evidence and
        /// deliberately read as neither success nor failure.
        /// </summary>
        Unrecognised,
    }

    /// <summary>
    /// Classifies the string TAS's simulate entry points return.
    /// <para>
    /// <b>Measured, not assumed.</b> <c>ITPD.Simulate</c>, <c>IPlantRoom.Simulate</c>,
    /// <c>IPlantRoom.SimulateExx</c> and <c>ISystem.Simulate</c> all return a <c>String</c>, and
    /// production discarded it. Five distinct returns have been measured on the licensed machine:
    /// </para>
    /// <code>
    /// "Done"                          <- SUCCESS. ISystem.Simulate, with 24 finite ZoneTemperature
    ///                                    values coming back for a 24-hour request.
    /// "Sizing Flow Failed"            <- the plant side could not size a flow
    /// "&lt;plant room name&gt; Has Errors"  <- e.g. "Plant Room Has Errors", "PR Has Errors"
    /// "Plant room has no components"
    /// "Failed to open the TSD file"
    /// </code>
    /// <para>
    /// Note that the "Has Errors" message embeds the plant room's <b>name</b>, so it cannot be matched
    /// literally - which is why the failure vocabulary is fragment-based.
    /// </para>
    /// <para>
    /// A measured failure refuses. A measured success is recorded as such but is still <b>not</b> the
    /// gate. Anything else is preserved verbatim and decides nothing. That is safe because the decisive
    /// gate is the complete <c>ZoneTemperature</c> reconciliation, which refuses a run that produced no
    /// results whatever TAS said about it.
    /// </para>
    /// </summary>
    public static class SimulationDiagnostic
    {
        /// <summary>
        /// Measured on licensed TAS accompanying a run that produced complete, finite results.
        /// Compared whole (case-insensitively), not as a fragment, because a success word appearing
        /// inside a longer sentence is not evidence that the sentence means success.
        /// </summary>
        private static readonly string[] knownSuccessAnswers = new string[]
        {
            "Done",
        };

        /// <summary>
        /// Fragments measured accompanying runs that produced nothing. Matched case-insensitively as
        /// substrings, because TAS's casing varies ("Plant room has no components" against "Plant Room
        /// Has Errors") and because "Has Errors" is prefixed by the plant room's own name.
        /// </summary>
        private static readonly string[] knownFailureFragments = new string[]
        {
            "has no components",
            "has errors",
            "failed to open",
            "failed",
            "error",
            "cannot",
            "unable",
            "invalid",
            "missing",
        };

        /// <summary>The success answers this classifier recognises, for reporting and for tests.</summary>
        public static IEnumerable<string> KnownSuccessAnswers
        {
            get { return (string[])knownSuccessAnswers.Clone(); }
        }

        /// <summary>The failure fragments this classifier recognises, for reporting and for tests.</summary>
        public static IEnumerable<string> KnownFailureFragments
        {
            get { return (string[])knownFailureFragments.Clone(); }
        }

        /// <summary>Classifies a simulate return.</summary>
        public static SimulationDiagnosticKind Classify(string returned)
        {
            if (string.IsNullOrWhiteSpace(returned))
            {
                return SimulationDiagnosticKind.Silent;
            }

            string trimmed = returned.Trim();

            bool failure = ContainsFailureFragment(trimmed);

            // Success is matched WHOLE, and only when the answer carries no measured failure fragment.
            //
            // Matching whole is what stops "Sizing Flow Failed" being read as a success because some
            // future success word appears inside it. The second condition is the other direction, and
            // it is deliberately belt and braces: if a success answer were ever added to the vocabulary
            // that also contained a failure fragment - or if TAS answered something like "Done, sizing
            // failed" - the two vocabularies would be in conflict, and a conflict must never resolve
            // to success. It resolves to failure, which refuses, which is the safe direction.
            if (!failure)
            {
                foreach (string answer in knownSuccessAnswers)
                {
                    if (string.Equals(trimmed, answer, StringComparison.OrdinalIgnoreCase))
                    {
                        return SimulationDiagnosticKind.KnownSuccess;
                    }
                }
            }

            return failure ? SimulationDiagnosticKind.KnownFailure : SimulationDiagnosticKind.Unrecognised;
        }

        private static bool ContainsFailureFragment(string trimmed)
        {
            foreach (string fragment in knownFailureFragments)
            {
                if (trimmed.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether this return, on its own, is grounds to refuse the run. Only a measured failure is.
        /// </summary>
        public static bool IsFailure(string returned)
        {
            return Classify(returned) == SimulationDiagnosticKind.KnownFailure;
        }

        /// <summary>
        /// Whether TAS itself reported success. <b>Not</b> sufficient to accept a run - the results
        /// reconciliation still decides - but it is positive evidence rather than mere silence.
        /// </summary>
        public static bool IsSuccess(string returned)
        {
            return Classify(returned) == SimulationDiagnosticKind.KnownSuccess;
        }
    }
}
