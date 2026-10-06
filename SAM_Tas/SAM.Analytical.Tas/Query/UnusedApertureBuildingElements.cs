// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Which aperture building elements the canonicalisation has left orphaned</b>, as a pure function
        /// of what was read off the building - no COM, so the decision is testable without an installed TAS.
        /// <para>
        /// TAS's own gbXML conversion creates one aperture element per aperture per part. Once every physical
        /// surface has been rebound onto the shared canonical definitions, the rest hold no surface at all -
        /// they are dead weight carrying a physical aperture GUID in their name, and they would still be
        /// listed in TAS's Building Elements. <c>TBD.Building</c> has no <c>RemoveBuildingElement</c>, so they
        /// are marked (<c>markDelete</c>) and swept by <c>Building.DeleteMarkedBuildingElements</c>.
        /// </para>
        /// <para><b>Three gates, all of which must pass before an element is marked:</b></para>
        /// <list type="bullet">
        /// <item>it is an APERTURE element - a pane or a frame. A panel's element is never touched, whatever
        /// its surface count;</item>
        /// <item>it holds NO physical surface after the rebind. An element still standing for a real window
        /// is never a candidate;</item>
        /// <item>it is not one of the CANONICAL elements this pass resolved to. An aperture whose rebind was
        /// refused leaves its canonical element momentarily surface-less, and deleting the definition a
        /// refusal was conservative about would turn a safe refusal into a loss.</item>
        /// </list>
        /// <para>
        /// The name takes no part in the decision. This runs only on the gbXML route, immediately after
        /// <c>T3DDocument.ExportNew</c> has just (re)written every building element from the current T3D -
        /// licensed-verified (see <c>APERTURE_DEFINITION_REUSE_GBXML.md</c>, "Licensed verification:
        /// ExportNew always replaces building elements") to hold with <c>WorkflowSettings.RemoveExistingTBD</c>
        /// either <c>true</c> or its default <c>false</c>: a sentinel aperture element injected into a
        /// pre-existing <c>.tbd</c> at that path does not survive a second <c>ExportNew</c> call under either
        /// setting, so there is no hand-authored or otherwise foreign element left in the building by the
        /// time this sweep runs to mistakenly protect - and deciding by name would mean either trusting TAS's
        /// naming or refusing to clean up the very elements this exists to clean up.
        /// </para>
        /// </summary>
        /// <param name="apertureBuildingElementUsages">Every building element in the building, with its surface count read AFTER the rebind.</param>
        /// <param name="canonicalGuids">The GUIDs of the elements this pass resolved apertures onto.</param>
        /// <returns>The GUIDs to mark for deletion, in input order. Never null.</returns>
        public static List<string> UnusedApertureBuildingElementGuids(IEnumerable<ApertureBuildingElementUsage> apertureBuildingElementUsages, IEnumerable<string> canonicalGuids)
        {
            List<string> result = new List<string>();

            if (apertureBuildingElementUsages == null)
            {
                return result;
            }

            HashSet<string> canonical = new HashSet<string>(canonicalGuids == null ? Enumerable.Empty<string>() : canonicalGuids.Where(x => !string.IsNullOrWhiteSpace(x)));

            foreach (ApertureBuildingElementUsage apertureBuildingElementUsage in apertureBuildingElementUsages)
            {
                if (apertureBuildingElementUsage == null || string.IsNullOrWhiteSpace(apertureBuildingElementUsage.Guid))
                {
                    continue;
                }

                if (!apertureBuildingElementUsage.IsAperture)
                {
                    continue;
                }

                if (apertureBuildingElementUsage.ZoneSurfaceCount != 0)
                {
                    continue;
                }

                if (canonical.Contains(apertureBuildingElementUsage.Guid))
                {
                    continue;
                }

                result.Add(apertureBuildingElementUsage.Guid);
            }

            return result;
        }
    }
}
