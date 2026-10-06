// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// What an aperture-control write must do about the aperture types a TBD building element ALREADY
    /// carried before this export touched it. Decided by
    /// <see cref="Query.ApertureTypeReconciliation(string, System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{string, ApertureTypeDefinition}}, ApertureTypeDefinition, int, out int, out string)"/>.
    /// </summary>
    public enum ApertureTypeReconciliation
    {
        [Description("Undefined")] Undefined = 0,

        /// <summary>
        /// Nothing on the element provides this control and nothing on it forbids adding one: go to the
        /// building-level reuse lookup, and create only if that misses too.
        /// </summary>
        [Description("Create")] Create = 1,

        /// <summary>
        /// The element already carries a type whose definition IS this control. It is returned as it
        /// stands and NOTHING is written to it - not even rewritten to the same value.
        /// </summary>
        [Description("Reuse")] Reuse = 2,

        /// <summary>
        /// Every type on the element is named after the element itself, so each is exclusive to it. The
        /// previous per-element write applies unchanged - the one place an existing aperture type is still
        /// written to, and safe precisely because no other element can reference it.
        /// </summary>
        [Description("Legacy")] Legacy = 3,

        /// <summary>
        /// The element carries a SHARED type this export authored which does not provide the requested
        /// control. Adding a second would double the ventilation and rewriting the first would change every
        /// other element referencing it, so the write is refused and the stale type is named.
        /// </summary>
        [Description("Refuse")] Refuse = 4
    }
}
