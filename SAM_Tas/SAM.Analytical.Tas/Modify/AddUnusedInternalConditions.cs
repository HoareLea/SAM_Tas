// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Backwards-compatible overload - forwards to the <c>ProfileReuseIndex</c> overload with no index, so
        /// callers compiled against the previous <c>AddUnusedInternalConditions(AdjacencyCluster, TBD.Building)</c>
        /// signature keep working. An added optional parameter changes that signature's arity and breaks them
        /// at runtime.
        /// </summary>
        public static List<InternalCondition> AddUnusedInternalConditions(this AdjacencyCluster adjacencyCluster, TBD.Building building)
        {
            return AddUnusedInternalConditions(adjacencyCluster, building, null);
        }

        /// <summary>
        /// Imports the TBD building's <b>full</b> internal-condition list — including conditions not
        /// assigned to any zone — into <paramref name="adjacencyCluster"/> as standalone template
        /// objects.
        /// <para/>
        /// The geometry-driven import (<see cref="Convert.ToSAM(TBD.Building, System.Collections.Generic.Dictionary{string, SAM.Geometry.Spatial.Polygon3D}, ProfileReuseIndex)"/>)
        /// only reaches internal conditions reachable from a zone, so building-level conditions that
        /// aren't assigned to any zone never become SAM objects. This adds the missing ones as
        /// templates so the library is complete in the model.
        /// <para/>
        /// Conditions already present (matched by name — whether a space's assigned condition or an
        /// already-imported zone condition) are left untouched; only the missing ones are added.
        /// They surface back through <c>AdjacencyCluster.GetInternalConditions</c>.
        /// <para/>
        /// NOTE: this is the import (model) half only. The TBD exporter writes internal conditions
        /// per <see cref="Space"/> (<see cref="UpdateZones(TBD.Building, AdjacencyCluster, ProfileLibrary, bool)"/>),
        /// reconstructing gains from each space's area/volume/occupancy — there is no
        /// template-based internal-condition export — so a template that is never assigned to a
        /// space is not written back out. Round-tripping those would require a separate export-side
        /// change.
        /// </summary>
        /// <param name="adjacencyCluster">The cluster the templates are added to.</param>
        /// <param name="building">The TBD building whose full internal-condition list is read.</param>
        /// <param name="profileReuseIndex">
        /// The conversion-wide profile reuse index, or null.
        /// <para>
        /// <b>This must be the same instance the model's <c>ProfileLibrary</c> and its zone-owned internal
        /// conditions were built from.</b> A template converted without it keeps the legacy
        /// <c>"{internal condition} [{profile}]"</c> references, which name nothing once the library carries the
        /// canonical shared definitions - a dangling reference on exactly the conditions no zone owns, i.e. the
        /// ones least likely to be noticed.
        /// </para>
        /// </param>
        /// <returns>The internal-condition templates added, or null on invalid input.</returns>
        public static List<InternalCondition> AddUnusedInternalConditions(this AdjacencyCluster adjacencyCluster, TBD.Building building, ProfileReuseIndex profileReuseIndex)
        {
            if (adjacencyCluster == null || building == null)
            {
                return null;
            }

            List<TBD.InternalCondition> internalConditions_TBD = Query.InternalConditions(building);
            if (internalConditions_TBD == null || internalConditions_TBD.Count == 0)
            {
                return null;
            }

            // Names already represented in the cluster (a space's assigned condition, an imported
            // zone condition, or a template added by an earlier call).
            HashSet<string> internalConditionNames = new HashSet<string>();
            IEnumerable<InternalCondition> internalConditions_Existing = adjacencyCluster.GetInternalConditions();
            if (internalConditions_Existing != null)
            {
                foreach (InternalCondition internalCondition in internalConditions_Existing)
                {
                    if (!string.IsNullOrWhiteSpace(internalCondition?.Name))
                    {
                        internalConditionNames.Add(internalCondition.Name);
                    }
                }
            }

            List<InternalCondition> result = new List<InternalCondition>();

            foreach (TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
            {
                if (internalCondition_TBD == null || string.IsNullOrWhiteSpace(internalCondition_TBD.name))
                {
                    continue;
                }

                if (internalConditionNames.Contains(internalCondition_TBD.name))
                {
                    continue;
                }

                // No owning zone, so no floor area — convert with area = NaN; the per-area gains are
                // kept raw rather than derived against a space (see Convert.ToSAM(TBD.InternalCondition, double, ProfileReuseIndex)).
                InternalCondition internalCondition = internalCondition_TBD.ToSAM(double.NaN, profileReuseIndex);
                if (internalCondition == null || string.IsNullOrWhiteSpace(internalCondition.Name))
                {
                    continue;
                }

                if (adjacencyCluster.AddObject(internalCondition))
                {
                    internalConditionNames.Add(internalCondition.Name);
                    result.Add(internalCondition);
                }
            }

            return result;
        }
    }
}
