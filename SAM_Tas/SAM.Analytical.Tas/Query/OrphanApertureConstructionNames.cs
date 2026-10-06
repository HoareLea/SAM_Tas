// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Which aperture constructions the canonicalisation has left orphaned</b>, as a pure function of
        /// names - no COM.
        /// <para>
        /// TAS's gbXML conversion names the constructions it creates after the T3D window, which carries the
        /// physical aperture GUID. Once every surface points at a shared canonical element carrying a shared
        /// construction, those per-aperture constructions are referenced by nothing.
        /// </para>
        /// <para>
        /// <b>An unreferenced aperture construction is removed on exactly two grounds.</b>
        /// </para>
        /// <list type="number">
        /// <item><b>It names a physical aperture</b>
        /// (<see cref="NamesContainingApertureGuid(IEnumerable{string}, IEnumerable{Guid})"/>) - an exact test
        /// against the model's own aperture GUIDs, not a heuristic.</item>
        /// <item><b>This pass SUPERSEDED it.</b> A construction the pass resolved under a
        /// signature-qualified name (<c>SIM_EXT_GLZ_CEAB27C2 -frame</c>) had its preferred plain name
        /// occupied by content it could not adopt. On the gbXML route that squatter is
        /// <c>Modify.UpdateConstructions</c>'s own earlier write, which differs from the Stage 2 definition in
        /// one field: <c>Modify.UpdateConstruction</c> sets <c>material.width</c> only for a TRANSPARENT
        /// material, so an opaque frame layer keeps the library default there while
        /// <c>construction.materialWidth</c> carries the real thickness - and a
        /// <see cref="ConstructionLayerDefinition"/> compares BOTH widths. Once the pass has bound every
        /// surface to its own correctly-stated construction, the squatter is referenced by nothing and its
        /// only remaining effect is an extra row in the TAS construction list.</item>
        /// </list>
        /// <para>
        /// <b>Anything else unreferenced is KEPT.</b> An unreferenced construction that names no aperture and
        /// superseded nothing is a reusable definition with no window using it right now - a library
        /// template, or an <c>ApertureConstruction</c> in the model with no windows built from it - and the
        /// export has always kept those. Removing one would be a behaviour change nothing here asks for, so
        /// those are reported through <paramref name="unreferenced_Kept"/> instead.
        /// </para>
        /// </summary>
        /// <param name="constructionNames">Every construction name in the building.</param>
        /// <param name="referencedNames">The names carried by building elements that survived the sweep.</param>
        /// <param name="apertureGuids">The model's physical aperture GUIDs.</param>
        /// <param name="supersededNames">The preferred names the pass could not use, because content it could not adopt already held them.</param>
        /// <param name="unreferenced_Kept">Unreferenced aperture constructions that were neither instance-named nor superseded, and so were left in place.</param>
        /// <returns>The construction names to remove, in input order. Never null.</returns>
        public static List<string> OrphanApertureConstructionNames(IEnumerable<string> constructionNames, IEnumerable<string> referencedNames, IEnumerable<Guid> apertureGuids, IEnumerable<string> supersededNames, out List<string> unreferenced_Kept)
        {
            List<string> result = new List<string>();
            unreferenced_Kept = new List<string>();

            if (constructionNames == null)
            {
                return result;
            }

            HashSet<string> referenced = new HashSet<string>(referencedNames == null ? Enumerable.Empty<string>() : referencedNames.Where(x => x != null));

            //Only this export's own pane/frame convention is considered at all. A panel construction has no
            //business in an aperture sweep, whether anything references it or not.
            List<string> candidates = constructionNames
                .Where(x => !string.IsNullOrWhiteSpace(x) && !referenced.Contains(x) && TryDecomposeConstructionName(x, out string _, out Analytical.AperturePart _))
                .ToList();

            HashSet<string> instanceNamed = new HashSet<string>(NamesContainingApertureGuid(candidates, apertureGuids));
            HashSet<string> superseded = new HashSet<string>(supersededNames == null ? Enumerable.Empty<string>() : supersededNames.Where(x => !string.IsNullOrWhiteSpace(x)));

            foreach (string candidate in candidates)
            {
                if (instanceNamed.Contains(candidate) || superseded.Contains(candidate))
                {
                    result.Add(candidate);
                }
                else
                {
                    unreferenced_Kept.Add(candidate);
                }
            }

            return result;
        }
    }
}
