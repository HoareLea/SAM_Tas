// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Aperture GUID -> the panel-held <see cref="Aperture"/> and its owning <see cref="Panel"/>, built
        /// once from the cluster's own panel walk. See <see cref="AperturePanelIndex"/> for why this exists
        /// rather than <see cref="AdjacencyCluster.GetAperture(System.Guid)"/>.
        /// <para>
        /// Shared by <c>Modify.UpdateBuildingElements</c> and <c>Modify.UpdateApertureDefinitions</c> so both
        /// passes resolve an aperture GUID to the same object.
        /// </para>
        /// </summary>
        public static AperturePanelIndex AperturePanelIndex(this AdjacencyCluster adjacencyCluster)
        {
            return new AperturePanelIndex(adjacencyCluster?.GetPanels());
        }
    }
}
