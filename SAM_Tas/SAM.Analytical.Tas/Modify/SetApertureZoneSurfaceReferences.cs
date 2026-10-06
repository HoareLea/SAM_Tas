// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>Write one aperture part's physical stamps: clear both slots, then fill them canonically.</b>
        /// The single mutator for <c>Pane</c>/<c>FrameZoneSurfaceReference_1</c>/<c>_2</c>, shared by the direct
        /// export, <see cref="UpdateIds(AdjacencyCluster, TBD.Building, double)"/> and the TBD import.
        /// <para>
        /// <b>Clear-then-fill, not fill-if-empty.</b> All three paths used to write the slot that happened to
        /// be free, which meant a second run over an already-stamped model kept the previous run's <c>_1</c>
        /// and overwrote <c>_2</c>. The old <c>_1</c> is not harmlessly redundant: TAS does not promise to
        /// reassign the same surface numbers on a fresh export, so a stale stamp points at a surface that
        /// exists and belongs to something else.
        /// </para>
        /// <para>
        /// <b>Which slot is decided by the zone, not by arrival order</b> - see
        /// <see cref="Query.ApertureZoneSurfaceSides(IEnumerable{ZoneSurfaceKey}, out string)"/>. Because all
        /// three paths call this, they agree, so a model can be exported, imported and updated without its
        /// sides ever swapping.
        /// </para>
        /// <para>
        /// <b>The caller's own spelling of the zone GUID is preserved.</b> Ordering normalises the GUID;
        /// writing does not. A re-exported model therefore still diffs clean against its source instead of
        /// showing every stamp rewritten into canonical case.
        /// </para>
        /// </summary>
        /// <param name="zoneSurfaceReferences">
        /// Every physical surface this part occupies, in any order. Nulls, references that do not locate a
        /// surface, and duplicates are dropped. An EMPTY set clears both slots and succeeds - that is the
        /// correct record of a part with no surfaces.
        /// </param>
        /// <param name="refusal">Why nothing was written, or null on success. Both slots are left cleared on a refusal.</param>
        /// <returns>False when <paramref name="aperture"/> is null or the surfaces span more than the two zones an aperture can separate.</returns>
        public static bool SetApertureZoneSurfaceReferences(this Aperture aperture, AperturePart aperturePart, IEnumerable<Core.Tas.ZoneSurfaceReference> zoneSurfaceReferences, out string refusal)
        {
            refusal = null;

            if (aperture == null)
            {
                return false;
            }

            //Cleared first and unconditionally: a refusal below must not leave last run's stamps standing as
            //though they had been confirmed.
            aperture.RemoveApertureZoneSurfaceReferences(aperturePart);

            Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference> referencesByKey = new Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference>();

            if (zoneSurfaceReferences != null)
            {
                foreach (Core.Tas.ZoneSurfaceReference zoneSurfaceReference in zoneSurfaceReferences)
                {
                    ZoneSurfaceKey zoneSurfaceKey = Query.ZoneSurfaceKey(zoneSurfaceReference);
                    if (zoneSurfaceKey == null || referencesByKey.ContainsKey(zoneSurfaceKey))
                    {
                        continue;
                    }

                    referencesByKey[zoneSurfaceKey] = zoneSurfaceReference;
                }
            }

            List<ZoneSurfaceKey> zoneSurfaceKeys = Query.ApertureZoneSurfaceSides(referencesByKey.Keys, out refusal);
            if (zoneSurfaceKeys == null)
            {
                return false;
            }

            //The side slots deliberately retain one representative per zone. Rebinding is different: every
            //physical face must move together, so preserve the complete set separately, in the same stable
            //zone/surface order. This is not another identity scheme - every entry is still exactly the
            //{ZoneGuid, SurfaceNumber} physical key used everywhere else.
            List<ZoneSurfaceKey> allKeys = referencesByKey.Keys.ToList();
            allKeys.Sort(Query.CompareZoneSurfaceKeys);
            if (allKeys.Count != 0)
            {
                aperture.SetValue(
                    Query.ApertureZoneSurfaceReferencesParameter(aperturePart),
                    new Core.SAMCollection<Core.Tas.ZoneSurfaceReference>(allKeys.ConvertAll(x => referencesByKey[x])));
            }

            for (int side = 1; side <= zoneSurfaceKeys.Count; side++)
            {
                aperture.SetValue(Query.ApertureZoneSurfaceReferenceParameter(aperturePart, side), referencesByKey[zoneSurfaceKeys[side - 1]]);
            }

            return true;
        }

        /// <summary>
        /// Add one physical surface to an aperture part and rewrite both slots canonically.
        /// <para>
        /// For the import, which meets an internal aperture twice - once per zone - and creates it on the
        /// first meeting. On the second it has one new surface and whatever the first pass already stamped, and
        /// the two together decide the slots. Adding a surface therefore re-canonicalises rather than appending
        /// to the first free slot, so which side is <c>_1</c> does not depend on which zone the import walked
        /// first.
        /// </para>
        /// </summary>
        public static bool AddApertureZoneSurfaceReference(this Aperture aperture, AperturePart aperturePart, Core.Tas.ZoneSurfaceReference zoneSurfaceReference, out string refusal)
        {
            refusal = null;

            if (aperture == null)
            {
                return false;
            }

            List<Core.Tas.ZoneSurfaceReference> zoneSurfaceReferences = Query.ApertureZoneSurfaceReferences(aperture, aperturePart);

            if (zoneSurfaceReference != null)
            {
                zoneSurfaceReferences.Add(zoneSurfaceReference);
            }

            return SetApertureZoneSurfaceReferences(aperture, aperturePart, zoneSurfaceReferences, out refusal);
        }
    }

    public static partial class Query
    {
        /// <summary>
        /// Every physical surface one aperture part currently states. New stamps read the complete canonical
        /// collection; older stamps fall back to the representative <c>_1</c>/<c>_2</c> slots. Never null.
        /// </summary>
        public static List<Core.Tas.ZoneSurfaceReference> ApertureZoneSurfaceReferences(this Aperture aperture, AperturePart aperturePart)
        {
            List<Core.Tas.ZoneSurfaceReference> result = new List<Core.Tas.ZoneSurfaceReference>();

            if (aperture == null)
            {
                return result;
            }

            if (aperture.TryGetValue(ApertureZoneSurfaceReferencesParameter(aperturePart), out Core.SAMCollection<Core.Tas.ZoneSurfaceReference> collection)
                && collection != null
                && collection.Count != 0)
            {
                foreach (Core.Tas.ZoneSurfaceReference zoneSurfaceReference in collection)
                {
                    if (zoneSurfaceReference != null)
                    {
                        result.Add(new Core.Tas.ZoneSurfaceReference(zoneSurfaceReference));
                    }
                }

                return result;
            }

            for (int side = 1; side <= 2; side++)
            {
                if (aperture.TryGetValue(ApertureZoneSurfaceReferenceParameter(aperturePart, side), out Core.Tas.ZoneSurfaceReference zoneSurfaceReference) && zoneSurfaceReference != null)
                {
                    result.Add(zoneSurfaceReference);
                }
            }

            return result;
        }
    }
}
