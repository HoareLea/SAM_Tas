// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>The one place an aperture GUID is turned back into the AUTHORITATIVE <see cref="Aperture"/> and the
    /// <see cref="Panel"/> that owns it.</b>
    /// <para>
    /// An <see cref="AdjacencyCluster"/> can hold the same aperture in TWO shapes: on its panel (which is
    /// what <see cref="AdjacencyCluster.GetApertures()"/>, <see cref="Query.AperturePhysicalIndex"/> and the
    /// export's own membership map all read) and, independently, as a cluster OBJECT in its own right. Real
    /// models carry both - every aperture in the licensed fixture does - and the two copies are NOT kept in
    /// step: the cluster object is whatever was last written through <c>AddObject</c>, while an edit made the
    /// ordinary way (<c>panel.RemoveAperture</c> / <c>panel.AddAperture</c>) reaches only the panel copy.
    /// </para>
    /// <para>
    /// <b><see cref="AdjacencyCluster.GetAperture(Guid)"/> returns the WRONG one of the two.</b> It tries
    /// <c>GetObject&lt;Aperture&gt;</c> first and returns as soon as that hits, so on such a model it hands
    /// back the stale cluster object and never looks at the panel - and its <c>out panel</c> overload leaves
    /// that panel null for the same reason. Anything reading aperture STATE through it (colour, opening
    /// properties, <see cref="Analytical.ApertureParameter.FeatureShade"/>) therefore reads a copy that
    /// predates the user's edit, and anything needing the owning panel gets nothing.
    /// </para>
    /// <para>
    /// This index answers from the panel walk only, so a caller always gets the copy an edit actually lands
    /// on. It deliberately has NO fallback to <c>GetObject&lt;Aperture&gt;</c>: an aperture no panel holds is
    /// not part of the model's physical fabric, and inventing one here would put the stale copy back.
    /// Writers that must keep BOTH shapes consistent (a re-stamp) read through this and then write the
    /// cluster object separately - see <c>Modify.UpdateApertureDefinitions</c>.
    /// </para>
    /// <para>COM-free.</para>
    /// </summary>
    public sealed class AperturePanelIndex
    {
        private readonly Dictionary<Guid, Panel> panelsByApertureGuid = new Dictionary<Guid, Panel>();

        private readonly Dictionary<Guid, Aperture> aperturesByGuid = new Dictionary<Guid, Aperture>();

        /// <param name="panels">
        /// Every panel in the model. A subset narrows what can be resolved; it never makes a wrong answer
        /// right, so callers pass the whole cluster.
        /// </param>
        public AperturePanelIndex(IEnumerable<Panel> panels)
        {
            if (panels == null)
            {
                return;
            }

            foreach (Panel panel in panels)
            {
                List<Aperture> apertures = panel?.Apertures;
                if (apertures == null)
                {
                    continue;
                }

                foreach (Aperture aperture in apertures)
                {
                    if (aperture == null)
                    {
                        continue;
                    }

                    //Last one wins, exactly as the panel walk that fed the membership map before this type
                    //existed. One aperture GUID held by two panels is a corrupted model, not something to
                    //arbitrate here.
                    panelsByApertureGuid[aperture.Guid] = panel;
                    aperturesByGuid[aperture.Guid] = aperture;
                }
            }
        }

        /// <summary>How many apertures a panel holds.</summary>
        public int Count
        {
            get { return aperturesByGuid.Count; }
        }

        /// <summary>The panel-held aperture, or null when no panel holds it.</summary>
        public Aperture GetAperture(Guid apertureGuid)
        {
            return aperturesByGuid.TryGetValue(apertureGuid, out Aperture aperture) ? aperture : null;
        }

        /// <summary>The panel that owns the aperture, or null when none does.</summary>
        public Panel GetPanel(Guid apertureGuid)
        {
            return panelsByApertureGuid.TryGetValue(apertureGuid, out Panel panel) ? panel : null;
        }

        /// <summary>Both at once, so a caller about to write through them makes one lookup and one check.</summary>
        public bool TryGetValue(Guid apertureGuid, out Aperture aperture, out Panel panel)
        {
            aperture = GetAperture(apertureGuid);
            panel = GetPanel(apertureGuid);
            return aperture != null && panel != null;
        }
    }
}
