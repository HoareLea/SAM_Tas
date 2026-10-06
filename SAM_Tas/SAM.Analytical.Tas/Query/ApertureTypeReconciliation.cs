// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// What to do about the aperture types a TBD building element ALREADY carried, before this export
        /// looks anything up or creates anything. COM-free: the caller reads the element's existing
        /// assignments once and hands them here as (name, definition) pairs.
        /// <para>
        /// <b>Why an element's pre-existing assignments are reconciled at all.</b> Export runs against a TBD
        /// that may already be a user's model. Its aperture types may be ones a previous export authored,
        /// ones the previous per-element naming produced, or ones the user made in TAS. A shared definition
        /// is IMMUTABLE once created - anything else would rewrite the control of every other element
        /// referencing it - so the only outcomes available are reuse it, leave it, add alongside it, or
        /// refuse. There is deliberately no fifth outcome that edits one.
        /// </para>
        /// <para>The order below is the decision, and the order matters:</para>
        /// <list type="number">
        /// <item>
        /// <b>Nothing assigned</b> - the ordinary path: look the definition up at building level, create if
        /// it misses.
        /// </item>
        /// <item>
        /// <b>Every assigned type named after the element</b> - the previous per-element convention. Those
        /// names carry the SAM aperture's GUID, so each type is exclusive to this element and the previous
        /// in-place write is safe and is kept EXACTLY as it was. A legacy TBD therefore behaves as it always
        /// did, and converges on shared types only through a fresh export.
        /// </item>
        /// <item>
        /// <b>An assigned type whose definition IS the requested control</b> - reuse it, write nothing. The
        /// <paramref name="ordinal"/>-th such match is taken, so an element carrying two identical controls
        /// hands its first child the first and its second child the second instead of both children
        /// claiming the same one.
        /// </item>
        /// <item>
        /// <b>An assigned type this export's naming convention would have produced, but not this control</b>
        /// - refuse. It is a shared type: adding a second would give the element two openings where the
        /// model states one, and rewriting it would change every other element that references it.
        /// </item>
        /// <item>
        /// <b>Anything else assigned</b> - unrecognised, so somebody else's. It is left exactly as it is and
        /// the requested control is added alongside it, which is the coexistence the previous write already
        /// produced.
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="assignedApertureTypes">
        /// The element's PRE-EXISTING assignments in element order, each as its name paired with its
        /// definition - or a null definition where the type could not be read or must not be reused.
        /// Types assigned during the current export are not part of this: they are this export's own work,
        /// not state to reconcile against.
        /// </param>
        /// <param name="ordinal">The 1-based occurrence of this definition among the element's opening children.</param>
        /// <param name="index">The index into <paramref name="assignedApertureTypes"/> to reuse, or -1.</param>
        /// <param name="refusal">Why nothing may be written, or null.</param>
        public static ApertureTypeReconciliation ApertureTypeReconciliation(string buildingElementName, IEnumerable<KeyValuePair<string, ApertureTypeDefinition>> assignedApertureTypes, ApertureTypeDefinition apertureTypeDefinition, int ordinal, out int index, out string refusal)
        {
            index = -1;
            refusal = null;

            if (apertureTypeDefinition == null)
            {
                refusal = "No aperture control was resolved, so there was nothing to reconcile against the building element's existing aperture types.";
                return Analytical.Tas.ApertureTypeReconciliation.Refuse;
            }

            List<KeyValuePair<string, ApertureTypeDefinition>> assigned = assignedApertureTypes?.ToList();
            if (assigned == null || assigned.Count == 0)
            {
                return Analytical.Tas.ApertureTypeReconciliation.Create;
            }

            if (assigned.TrueForAll(x => IsLegacyApertureTypeName(x.Key, buildingElementName)))
            {
                return Analytical.Tas.ApertureTypeReconciliation.Legacy;
            }

            int ordinal_Temp = ordinal < 1 ? 1 : ordinal;
            int matches = 0;
            for (int i = 0; i < assigned.Count; i++)
            {
                if (assigned[i].Value == null || !assigned[i].Value.Equals(apertureTypeDefinition))
                {
                    continue;
                }

                matches++;
                if (matches == ordinal_Temp)
                {
                    index = i;
                    return Analytical.Tas.ApertureTypeReconciliation.Reuse;
                }
            }

            List<string> names_Shared = assigned
                .Where(x => !IsLegacyApertureTypeName(x.Key, buildingElementName) && TryDecomposeApertureTypeName(x.Key, out string _, out int _))
                .Select(x => x.Key)
                .ToList();

            if (names_Shared.Count != 0)
            {
                refusal = string.Format("TBD building element '{0}' already carries the shared aperture type(s) {1}, which do not match the opening control this export states{2}. A shared aperture type is referenced by every element that uses it, so it was neither rewritten (which would change all of them) nor added to (which would give this element more openings than the model states). Remove the stale aperture type in TAS, or export to a fresh TBD.",
                    buildingElementName,
                    string.Join(", ", names_Shared.Select(x => string.Format("'{0}'", x))),
                    ordinal_Temp >= 2 ? string.Format(" for opening occurrence {0}", ordinal_Temp) : string.Empty);

                return Analytical.Tas.ApertureTypeReconciliation.Refuse;
            }

            return Analytical.Tas.ApertureTypeReconciliation.Create;
        }
    }
}
