// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>Stage 3's split/rebind decision, resolved WITHOUT touching COM.</b> Whether
        /// <paramref name="aperture"/>'s own required colour, opening-control assignments and feature shade
        /// for <paramref name="aperturePart"/> are exactly what a shared TBD building element ALREADY
        /// carries - so the aperture may stay bound to it with zero writes - or whether they have diverged,
        /// meaning the aperture must be split onto its own element instead.
        /// <para>
        /// <b>Only colour, opening assignments and the feature shade are compared.</b> On the
        /// <c>UpdateBuildingElements</c> follow-up path the element's CONSTRUCTION is resolved once, by
        /// name, for the whole element - not per aperture - so it can never itself be a reason one member
        /// diverges from another; only what an individual aperture states about its own colour, its own
        /// openings and its own shade can drift after the export that bound it.
        /// </para>
        /// <para>
        /// <b>A frame never carries opening assignments or a feature shade</b> - only a pane's write reaches
        /// <c>SetApertureTypes</c>/<c>SetFeatureShades</c> - so <paramref name="aperturePart"/> being
        /// <see cref="AperturePart.Frame"/> compares colour alone; the aperture's required assignment list
        /// is always empty, and matches only an element that itself carries none.
        /// </para>
        /// </summary>
        /// <param name="aperture">The member aperture.</param>
        /// <param name="aperturePart">Which half of it this element is.</param>
        /// <param name="colour_Existing">What the element's <c>colour</c> already reads as.</param>
        /// <param name="apertureTypeAssignments_Existing">What the element's openings already resolve to, in child order - <see cref="BuildingReuseCache.ExistingAssignments(TBD.buildingElement)"/>'s values.</param>
        /// <param name="dayTypeNames">The day types every control this export writes applies on.</param>
        /// <param name="featureShade_Existing">What the element's feature shade already reads as, converted to SAM - null when the element carries none. Compared only for a pane.</param>
        public static bool ApertureMatchesExistingAssignment(this Aperture aperture, Analytical.AperturePart aperturePart, uint colour_Existing, IEnumerable<ApertureTypeDefinition> apertureTypeAssignments_Existing, IEnumerable<string> dayTypeNames, FeatureShade featureShade_Existing = null)
        {
            if (aperture == null)
            {
                return false;
            }

            System.Drawing.Color? color = Color(aperture, aperturePart);
            if (color == null || !color.HasValue)
            {
                return false;
            }

            if (Core.Convert.ToUint(color.Value) != colour_Existing)
            {
                return false;
            }

            if (aperturePart == Analytical.AperturePart.Pane)
            {
                //A pane whose stated shade differs from what the element carries - added, removed or
                //changed - has diverged exactly as a colour change has: left alone it would keep the
                //element's stale shade and never reach SetFeatureShades.
                aperture.TryGetValue(Analytical.ApertureParameter.FeatureShade, out FeatureShade featureShade_Required);
                if (!FeatureShadesMatch(featureShade_Required, featureShade_Existing))
                {
                    return false;
                }
            }

            List<ApertureTypeDefinition> required = aperturePart == Analytical.AperturePart.Pane
                ? ApertureTypeAssignments(aperture, Analytical.AperturePart.Pane, dayTypeNames).ConvertAll(x => x.ApertureTypeDefinition)
                : new List<ApertureTypeDefinition>();

            List<ApertureTypeDefinition> existing = apertureTypeAssignments_Existing == null ? new List<ApertureTypeDefinition>() : apertureTypeAssignments_Existing.ToList();

            return required.Count == existing.Count && required.SequenceEqual(existing);
        }
    }
}
