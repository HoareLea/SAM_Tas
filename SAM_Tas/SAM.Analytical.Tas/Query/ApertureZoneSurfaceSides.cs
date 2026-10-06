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
        /// Which of an aperture part's physical surfaces goes in the <c>_1</c> slot and which in <c>_2</c>.
        /// <para>
        /// <b>A slot is a SIDE, and a side is a zone.</b> That is the rule, and stating it here is the point of
        /// the function: an aperture between two zones has one surface per zone, and the two slots record which
        /// surface is which side. Where one side contributes several surfaces - an aperture whose pane is split
        /// into several faces does - they are all the SAME side, so they compete for one slot rather than
        /// filling both; the lowest surface number in that zone represents it. The previous behaviour, filling
        /// <c>_1</c> then <c>_2</c> in creation order, put two same-side surfaces in the two slots and lost the
        /// other side entirely.
        /// </para>
        /// <para>
        /// <b>Canonical, not incidental.</b> Every path that writes these stamps - the direct export, the import
        /// and <c>UpdateIds</c> - used to fill <c>_1</c> first and <c>_2</c> second in whatever order it happened
        /// to meet the two zones, and to fill a slot only when it was EMPTY. On a model that was already stamped
        /// from a previous run, that left a stale <c>_1</c> and overwrote <c>_2</c>; and even on a clean model it
        /// made which side is <c>_1</c> a property of a list rather than of the model, so re-running an update
        /// could swap them. Here the slot is decided by the ZONE GUID, ordinal: total, independent of
        /// enumeration order, and identical on all three paths, so the same aperture lands the same way round on
        /// every run. TAS states no side ordering of its own, and nothing in this codebase reads <c>_1</c> as
        /// "the outward side" - the result mapping and every resolver check both slots.
        /// </para>
        /// <para>
        /// Three or more zones is a REFUSAL, not a truncation. An aperture separates at most two; a third means
        /// the caller's grouping is wrong, and quietly dropping it would hide that.
        /// </para>
        /// </summary>
        /// <param name="zoneSurfaceKeys">The physical surfaces of ONE aperture part, in any order. Nulls, invalid and duplicate keys are dropped.</param>
        /// <param name="refusal">Why no assignment was made, or null on success.</param>
        /// <returns>One key per side, in slot order - index 0 is <c>_1</c>, index 1 is <c>_2</c> - or null when <paramref name="refusal"/> is set.</returns>
        public static List<ZoneSurfaceKey> ApertureZoneSurfaceSides(IEnumerable<ZoneSurfaceKey> zoneSurfaceKeys, out string refusal)
        {
            refusal = null;

            List<ZoneSurfaceKey> keys = new List<ZoneSurfaceKey>();

            if (zoneSurfaceKeys != null)
            {
                foreach (ZoneSurfaceKey zoneSurfaceKey in zoneSurfaceKeys)
                {
                    if (zoneSurfaceKey == null || !zoneSurfaceKey.IsValid || keys.Contains(zoneSurfaceKey))
                    {
                        continue;
                    }

                    keys.Add(zoneSurfaceKey);
                }
            }

            List<IGrouping<string, ZoneSurfaceKey>> groups = keys.GroupBy(x => x.ZoneGuid).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();

            if (groups.Count > 2)
            {
                refusal = string.Format("An aperture part states surfaces in {0} zones ({1}); an aperture separates at most two, so no side assignment was made.",
                    groups.Count,
                    string.Join(", ", groups.Select(x => x.Key)));

                return null;
            }

            List<ZoneSurfaceKey> result = new List<ZoneSurfaceKey>();

            foreach (IGrouping<string, ZoneSurfaceKey> group in groups)
            {
                result.Add(group.OrderBy(x => x.SurfaceNumber).First());
            }

            return result;
        }

        /// <summary>
        /// The total order the <c>_1</c>/<c>_2</c> slots follow: zone GUID (ordinal, already normalised), then
        /// surface number. Ordinal rather than culture-aware so the answer does not depend on the machine.
        /// </summary>
        public static int CompareZoneSurfaceKeys(ZoneSurfaceKey zoneSurfaceKey_1, ZoneSurfaceKey zoneSurfaceKey_2)
        {
            if (ReferenceEquals(zoneSurfaceKey_1, zoneSurfaceKey_2))
            {
                return 0;
            }

            if (zoneSurfaceKey_1 == null)
            {
                return -1;
            }

            if (zoneSurfaceKey_2 == null)
            {
                return 1;
            }

            int result = string.Compare(zoneSurfaceKey_1.ZoneGuid, zoneSurfaceKey_2.ZoneGuid, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            return zoneSurfaceKey_1.SurfaceNumber.CompareTo(zoneSurfaceKey_2.SurfaceNumber);
        }

        /// <summary>
        /// The <c>ApertureParameter</c> that holds one part in one slot - the single place the
        /// <c>Pane</c>/<c>Frame</c> x <c>_1</c>/<c>_2</c> mapping is written down.
        /// </summary>
        public static ApertureParameter ApertureZoneSurfaceReferenceParameter(AperturePart aperturePart, int side)
        {
            if (aperturePart == Analytical.AperturePart.Frame)
            {
                return side == 2 ? ApertureParameter.FrameZoneSurfaceReference_2 : ApertureParameter.FrameZoneSurfaceReference_1;
            }

            return side == 2 ? ApertureParameter.PaneZoneSurfaceReference_2 : ApertureParameter.PaneZoneSurfaceReference_1;
        }

        /// <summary>The <c>ApertureParameter</c> that holds one part's reusable definition binding.</summary>
        public static ApertureParameter ApertureBuildingElementGuidParameter(AperturePart aperturePart)
        {
            return aperturePart == Analytical.AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;
        }

        /// <summary>
        /// The collection parameter that preserves every physical surface of one part. The <c>_1</c>/<c>_2</c>
        /// parameters remain the representative side identities; this collection is used only where an
        /// operation, notably a definition rebind, must act on every face belonging to those sides.
        /// </summary>
        public static ApertureParameter ApertureZoneSurfaceReferencesParameter(AperturePart aperturePart)
        {
            return aperturePart == Analytical.AperturePart.Frame ? ApertureParameter.FrameZoneSurfaceReferences : ApertureParameter.PaneZoneSurfaceReferences;
        }

        /// <summary>
        /// Clear every physical stamp on an aperture part, both slots.
        /// <para>
        /// A write path must clear before it fills. The stamps are the model's record of where an aperture WAS,
        /// and a path that only ever fills empty slots carries a stale reference from the previous run forward
        /// for ever - the surface numbers TAS assigns on a fresh export need not be the ones it assigned last
        /// time, so a stale <c>_1</c> is not merely redundant, it points somewhere real and wrong.
        /// </para>
        /// </summary>
        public static void RemoveApertureZoneSurfaceReferences(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null)
            {
                return;
            }

            aperture.RemoveValue(ApertureZoneSurfaceReferenceParameter(aperturePart, 1));
            aperture.RemoveValue(ApertureZoneSurfaceReferenceParameter(aperturePart, 2));
            aperture.RemoveValue(ApertureZoneSurfaceReferencesParameter(aperturePart));
        }

        /// <summary>
        /// Clear one aperture part's reusable-definition binding.
        /// <para>
        /// The same rule <see cref="RemoveApertureZoneSurfaceReferences(Aperture, AperturePart)"/> states for the
        /// physical stamps, applied to the binding they travel with. A <c>Pane</c>/<c>FrameBuildingElementGuid</c>
        /// only ever means "this part was bound to definition X <b>in the TBD it was last stamped against</b>".
        /// Carried into a DIFFERENT TBD it is not merely redundant: TAS mints its own aperture elements on every
        /// gbXML/T3D conversion, so the stale GUID names an element that either does not exist or exists and
        /// belongs to something else - and the surface it claims is really bound to the new element.
        /// </para>
        /// <para>
        /// <b>Deliberately NOT called from <see cref="Modify.SetApertureZoneSurfaceReferences"/>.</b> That mutator
        /// owns the physical stamps alone; the binding is written by whichever pass resolved it. Only a pass that
        /// is about to re-resolve the binding against a new TBD - <see cref="Modify.UpdateIds"/> - may clear it,
        /// and it clears BOTH so a part it cannot re-match reports honestly as unstamped rather than presenting
        /// the previous TBD's binding as current state.
        /// </para>
        /// </summary>
        public static void RemoveApertureBuildingElementGuid(this Aperture aperture, AperturePart aperturePart)
        {
            if (aperture == null)
            {
                return;
            }

            aperture.RemoveValue(ApertureBuildingElementGuidParameter(aperturePart));
        }
    }
}
