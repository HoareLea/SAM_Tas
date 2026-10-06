// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Imports the TBD building's <b>full</b> construction library — including constructions
        /// not attached to any imported surface — into <paramref name="adjacencyCluster"/> as
        /// standalone template objects.
        /// <para/>
        /// The geometry-driven import (<see cref="Convert.ToSAM(TBD.Building, System.Collections.Generic.Dictionary{string, SAM.Geometry.Spatial.Polygon3D}, ProfileReuseIndex)"/>)
        /// only reaches constructions referenced by a zone surface, so unused entries — e.g. the
        /// transparent <c>Air_Glass</c> placeholder created during HDD sizing, or the Null building
        /// element's construction — are silently dropped on round-trip. This restores them so the
        /// exported TBD keeps the same library it started with.
        /// <para/>
        /// Constructions already present (matched by name) are left untouched; only the missing
        /// ones are added. Glazing parts — TBD's "… -pane" / "… -frame" pairs — become
        /// <see cref="ApertureConstruction"/> templates (the two sides sharing a base name are
        /// combined); every other construction, including unsuffixed transparent ones such as the
        /// <c>Air_Glass</c> sizing placeholder, becomes a <see cref="Construction"/> template so the
        /// export preserves its exact name (transparency is re-derived from its materials). Both
        /// surface back through <c>AdjacencyCluster.GetConstructions</c>/<c>GetApertureConstructions</c>,
        /// which the exporter (<see cref="UpdateConstructions(TBD.TBDDocument, AnalyticalModel)"/>)
        /// already writes out in full.
        /// </summary>
        /// <returns>The construction/aperture-construction templates added, or null on invalid input.</returns>
        public static List<SAMObject> AddUnusedConstructions(this AdjacencyCluster adjacencyCluster, TBD.Building building)
        {
            if (adjacencyCluster == null || building == null)
            {
                return null;
            }

            List<TBD.Construction> constructions_TBD = Query.Constructions(building);
            if (constructions_TBD == null || constructions_TBD.Count == 0)
            {
                return null;
            }

            // Names already represented in the cluster (attached to a panel/aperture, or added by
            // an earlier call). Aperture constructions are keyed by their base name (the import
            // strips the "-pane"/"-frame" suffix), so dedup against the same stripped form below.
            HashSet<string> constructionNames = new HashSet<string>();
            List<Construction> constructions_Existing = adjacencyCluster.GetConstructions();
            if (constructions_Existing != null)
            {
                foreach (Construction construction in constructions_Existing)
                {
                    if (!string.IsNullOrWhiteSpace(construction?.Name))
                    {
                        constructionNames.Add(construction.Name);
                    }
                }
            }

            HashSet<string> apertureConstructionNames = new HashSet<string>();
            List<ApertureConstruction> apertureConstructions_Existing = adjacencyCluster.GetApertureConstructions();
            if (apertureConstructions_Existing != null)
            {
                foreach (ApertureConstruction apertureConstruction in apertureConstructions_Existing)
                {
                    if (!string.IsNullOrWhiteSpace(apertureConstruction?.Name))
                    {
                        apertureConstructionNames.Add(apertureConstruction.Name);
                    }
                }
            }

            List<SAMObject> result = new List<SAMObject>();

            // Transparent constructions come over as two TBD constructions sharing a base name
            // ("… -pane" / "… -frame"); combine them into one ApertureConstruction template keyed
            // by that base name before adding, so neither side is dropped or double-counted.
            Dictionary<string, ApertureConstruction> apertureConstructions_New = new Dictionary<string, ApertureConstruction>();

            foreach (TBD.Construction construction_TBD in constructions_TBD)
            {
                if (construction_TBD == null || string.IsNullOrWhiteSpace(construction_TBD.name))
                {
                    continue;
                }

                // Only genuine glazing parts — TBD stores them as a "… -pane" / "… -frame" pair —
                // become ApertureConstruction templates. An unsuffixed construction (even a
                // transparent one like the Air_Glass sizing placeholder) is kept as an ordinary
                // Construction template: the opaque export path writes it back under its exact name
                // and re-derives transparent/opaque from its materials, whereas the aperture export
                // path would rename it to a pane/frame construction and lose the original name.
                string name_TBD = construction_TBD.name.Trim();
                bool aperturePart = name_TBD.EndsWith(AperturePart.Pane.Sufix()) || name_TBD.EndsWith(AperturePart.Frame.Sufix());

                if (aperturePart)
                {
                    ApertureConstruction apertureConstruction = construction_TBD.ToSAM_ApertureConstruction(ApertureType.Window);
                    if (apertureConstruction == null || string.IsNullOrWhiteSpace(apertureConstruction.Name))
                    {
                        continue;
                    }

                    if (apertureConstructionNames.Contains(apertureConstruction.Name))
                    {
                        continue;
                    }

                    if (apertureConstructions_New.TryGetValue(apertureConstruction.Name, out ApertureConstruction apertureConstruction_Existing))
                    {
                        List<ConstructionLayer> paneConstructionLayers = apertureConstruction_Existing.HasPaneConstructionLayers() ? apertureConstruction_Existing.PaneConstructionLayers : apertureConstruction.PaneConstructionLayers;
                        List<ConstructionLayer> frameConstructionLayers = apertureConstruction_Existing.HasFrameConstructionLayers() ? apertureConstruction_Existing.FrameConstructionLayers : apertureConstruction.FrameConstructionLayers;

                        apertureConstruction = new ApertureConstruction(apertureConstruction_Existing.Guid, apertureConstruction_Existing.Name, apertureConstruction_Existing.ApertureType, paneConstructionLayers, frameConstructionLayers);
                    }

                    apertureConstructions_New[apertureConstruction.Name] = apertureConstruction;
                }
                else
                {
                    Construction construction = construction_TBD.ToSAM();
                    if (construction == null || string.IsNullOrWhiteSpace(construction.Name))
                    {
                        continue;
                    }

                    if (constructionNames.Contains(construction.Name))
                    {
                        continue;
                    }

                    if (adjacencyCluster.AddObject(construction))
                    {
                        constructionNames.Add(construction.Name);
                        result.Add(construction);
                    }
                }
            }

            foreach (ApertureConstruction apertureConstruction in apertureConstructions_New.Values)
            {
                if (adjacencyCluster.AddObject(apertureConstruction))
                {
                    result.Add(apertureConstruction);
                }
            }

            return result;
        }

        /// <summary>
        /// Writes the unused (library) construction templates held in <paramref name="adjacencyCluster"/>
        /// into <paramref name="building"/>. Export-side counterpart to
        /// <see cref="AddUnusedConstructions(AdjacencyCluster, TBD.Building)"/>.
        /// <para/>
        /// <see cref="Convert.ToTBD(AnalyticalModel, TBD.TBDDocument, bool, string, string)"/> only
        /// emits constructions referenced by a panel/aperture, so standalone templates (e.g.
        /// <c>Air_Glass</c>, the Null building element's construction) would otherwise be dropped on
        /// this export path — unlike the gbXML workflow, which writes the full library via
        /// <see cref="UpdateConstructions(TBD.TBDDocument, AnalyticalModel)"/>. Standalone templates
        /// are exactly the <c>Construction</c>/<c>ApertureConstruction</c> objects held in the cluster
        /// (used constructions live on their panels), so they are written here through the existing
        /// per-name <see cref="UpdateConstructions(TBD.Building, System.Collections.Generic.IEnumerable{Construction}, MaterialLibrary)"/>
        /// overloads, which reuse a same-named TBD construction or add a new one — no duplicates.
        /// </summary>
        public static void AddUnusedConstructions(this TBD.Building building, AdjacencyCluster adjacencyCluster, Core.MaterialLibrary materialLibrary)
        {
            if (building == null || adjacencyCluster == null)
            {
                return;
            }

            List<Construction> constructions = adjacencyCluster.GetObjects<Construction>();
            if (constructions != null && constructions.Count != 0)
            {
                building.UpdateConstructions(constructions, materialLibrary);
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetObjects<ApertureConstruction>();
            if (apertureConstructions != null && apertureConstructions.Count != 0)
            {
                building.UpdateConstructions(apertureConstructions, materialLibrary);
            }
        }
    }
}
