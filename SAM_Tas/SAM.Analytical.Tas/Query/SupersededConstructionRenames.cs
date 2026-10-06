// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Which signature-qualified aperture constructions may reclaim the plain name they wanted</b>,
        /// now that the content squatting on it has been swept - a pure function of names, no COM.
        /// <para>
        /// <b>Why this matters.</b> The base name left after stripping <c>-pane</c>/<c>-frame</c> is what the
        /// round-tripped <c>ApertureConstruction</c> is NAMED after
        /// (<c>Query.ApertureConstructionName</c>, via <c>Convert.ToSAM_AdjacencyCluster</c>). If only ONE of
        /// the two parts had to take a qualified name, the two bases stop matching -
        /// <c>SIM_EXT_GLZ -pane</c> against <c>SIM_EXT_GLZ_CEAB27C2 -frame</c> - and the model's own
        /// construction name comes back mangled on every round trip. Reclaiming the plain name keeps it.
        /// <para>
        /// Until <c>APERTURE_HARDENING.md</c> this was STRUCTURAL, not just naming: the import used to pair a
        /// window's halves by that base, so a mismatch produced one aperture per SURFACE instead of one per
        /// window. It now groups a window's surfaces geometrically and keys the family on the pair of
        /// construction identities, so a mismatched base costs a name and nothing else.
        /// </para>
        /// </para>
        /// <para>
        /// <b>Two guards.</b> A rename is offered only when the plain name was actually REMOVED - so nothing
        /// is ever renamed onto a name still in use - and only when exactly one construction wanted it. Two
        /// different definitions can sanitise to the same base; handing the name to whichever came first
        /// would be arbitrary, so neither takes it.
        /// </para>
        /// </summary>
        /// <param name="supersededBy">Preferred name -> the name the construction actually took, for every construction that could not have its preferred name.</param>
        /// <param name="removedNames">The construction names the sweep has just removed.</param>
        /// <returns>The renames to perform, as actual -> preferred. Never null.</returns>
        public static List<KeyValuePair<string, string>> SupersededConstructionRenames(IEnumerable<KeyValuePair<string, string>> supersededBy, IEnumerable<string> removedNames)
        {
            List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();

            if (supersededBy == null)
            {
                return result;
            }

            HashSet<string> removed = new HashSet<string>(removedNames == null ? Enumerable.Empty<string>() : removedNames.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (removed.Count == 0)
            {
                return result;
            }

            //Grouped by the PREFERRED name, so a name two definitions both wanted is recognised as contested
            //and given to neither.
            foreach (IGrouping<string, KeyValuePair<string, string>> grouping in supersededBy
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value) && x.Key != x.Value)
                .GroupBy(x => x.Key))
            {
                if (!removed.Contains(grouping.Key))
                {
                    continue;
                }

                List<string> names_Actual = grouping.Select(x => x.Value).Distinct().ToList();
                if (names_Actual.Count != 1)
                {
                    continue;
                }

                result.Add(new KeyValuePair<string, string>(names_Actual[0], grouping.Key));
            }

            return result;
        }
    }
}
