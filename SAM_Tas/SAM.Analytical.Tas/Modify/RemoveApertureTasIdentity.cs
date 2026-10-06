// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>Forget everything an aperture states about the TBD it was last stamped against</b> - both parts'
        /// physical <c>ZoneSurfaceReference</c> stamps AND both parts' <c>Pane</c>/<c>FrameBuildingElementGuid</c>
        /// definition bindings.
        /// <para>
        /// The single clearing step of <see cref="UpdateIds(AdjacencyCluster, TBD.Building, double)"/>, which
        /// clears unconditionally and refills only what it re-matches. <b>The two must be cleared together.</b>
        /// The stamps were already cleared on their own, and the binding was not - so a part the refresh could
        /// not match kept the binding it was given against the PREVIOUS TBD, and every later pass read it as
        /// the current one.
        /// </para>
        /// <para>
        /// Across a full round trip - <c>TBD -> FromTBD -> SAM -> gbXML -> a NEW TBD</c> - that binding is
        /// always stale. A <c>BuildingElementGuid</c> only ever says "this part was bound to definition X in
        /// the file it was last stamped against", and TAS mints its own aperture elements on every gbXML/T3D
        /// conversion, so the imported GUID names an element of the old file while the surface it claims now
        /// sits on a new one. <see cref="UpdateApertureDefinitions(TBD.Building, AdjacencyCluster, Core.MaterialLibrary, out System.Collections.Generic.List{string})"/>
        /// then counted the part as bound and <see cref="Query.ApertureRebindKeys"/> refused it - correctly,
        /// but about state that was never current, which is how a whole model could report
        /// "40 aperture part(s) considered; 0 rebound".
        /// </para>
        /// <para>
        /// <b>Nothing here relaxes a gate.</b> A part that IS re-matched is restamped and rebound in the same
        /// pass, so its outcome is unchanged. A part that is not now reads as unstamped, which is the honest
        /// record of a refresh that could not resolve it - and is what the already-strict refusals are
        /// entitled to be given.
        /// </para>
        /// </summary>
        public static void RemoveApertureTasIdentity(this Aperture aperture)
        {
            if (aperture == null)
            {
                return;
            }

            foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Pane, AperturePart.Frame })
            {
                aperture.RemoveApertureZoneSurfaceReferences(aperturePart);
                aperture.RemoveApertureBuildingElementGuid(aperturePart);
            }
        }
    }
}
