// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>Gives the gbXML/T3D route the same reusable aperture definitions the direct export has.</b>
        /// <para>
        /// On the direct route (<c>Modify.Update</c>) SAM_Tas writes the TBD itself, so it resolves one shared
        /// <c>TBD.Construction</c> and one shared aperture <c>TBD.buildingElement</c> per DEFINITION as it
        /// goes. On the gbXML route it does not: TAS's own <c>T3DDocument.ExportNew</c> writes the TBD, from a
        /// T3D in which every aperture is its own <c>window</c> - it has to be, because the gbXML opening name
        /// carries the aperture GUID and <c>Query.UpdateT3D</c> decodes it back to find the SAM aperture. TAS
        /// therefore creates one aperture element and one construction PER APERTURE PER PART, named after that
        /// aperture. Nothing afterwards collapsed them; <c>Modify.UpdateBuildingElements</c> only ever SPLITS a
        /// diverged aperture off a shared element, never merges.
        /// </para>
        /// <para>
        /// This pass closes that gap by rebinding each physical surface onto the definition its SAM aperture
        /// actually states, and then sweeping up what TAS left behind. Twenty identical windows go from forty
        /// elements and forty constructions to two and two, while all forty physical <c>zoneSurface</c>s
        /// remain.
        /// </para>
        /// <para>
        /// <b>It creates no new rules.</b> Definition resolution is
        /// <see cref="ResolveApertureDefinition(Building, BuildingReuseCache, Aperture, AperturePart, MaterialLibrary, IDictionary{string, TBD.Construction}, IDictionary{string, buildingElement}, out TBD.Construction, out buildingElement, out ConstructionDefinition, out BuildingElementDefinition, out bool, out bool, out string)"/>,
        /// the very code the direct export runs. Physical resolution is Stage 3's
        /// <c>Query.AperturePhysicalIndex</c> and <c>Query.ApertureRebindKeys</c>, unchanged - so a
        /// two-sided aperture moves as one complete set or not at all, and a physical surface claimed by two
        /// apertures REFUSES rather than being guessed at.
        /// </para>
        /// <para>
        /// <b>Run it AFTER <c>Modify.UpdateIds</c>.</b> That is what makes it small: <c>UpdateIds</c> has
        /// already stamped every aperture's <c>Pane/FrameZoneSurfaceReference</c> and its current
        /// <c>Pane/FrameBuildingElementGuid</c>, so this pass reads which surfaces an aperture owns and which
        /// element they are currently on rather than re-deriving either from geometry. A later
        /// <c>UpdateIds</c> re-derives the same stamps from the actual bindings, so the order stays idempotent.
        /// </para>
        /// <para>
        /// <b>Physical instances are never merged.</b> <c>{ZoneGuid, SurfaceNumber}</c> remains physical
        /// identity and is not touched; <c>BuildingElementGuid</c> remains a reusable-definition binding that
        /// many apertures legitimately share, and is re-stamped to the definition each aperture now points at.
        /// </para>
        /// </summary>
        /// <param name="building">The open TBD building, as TAS's gbXML conversion produced it.</param>
        /// <param name="adjacencyCluster">The SAM model. Its apertures are re-stamped in place.</param>
        /// <param name="materialLibrary">The model's material library, for the construction content.</param>
        /// <param name="notes">What happened, one sentence each; problems carry <see cref="NotePrefix_Issue"/>.</param>
        /// <returns>True when the pass ran.</returns>
        public static bool UpdateApertureDefinitions(this Building building, AdjacencyCluster adjacencyCluster, MaterialLibrary materialLibrary, out List<string> notes)
        {
            notes = [];

            if (building == null || adjacencyCluster == null)
            {
                notes.Add(NotePrefix_Issue + "Aperture definitions: no TBD building or SAM adjacency cluster, so no aperture definition was reused.");
                return false;
            }

            List<Aperture> apertures = adjacencyCluster.GetApertures();
            if (apertures == null || apertures.Count == 0)
            {
                notes.Add("Aperture definitions: the model states no apertures, so there was nothing to reuse.");
                return true;
            }

            List<Guid> apertureGuids = apertures.ConvertAll(x => x.Guid);

            List<buildingElement> buildingElements = building.BuildingElements() ?? new List<buildingElement>();
            List<TBD.Construction> constructions = building.Constructions() ?? new List<TBD.Construction>();

            //The name namespaces, so a definition this pass creates can never be given a name something else
            //in the building already holds - including the panel constructions and elements.
            Dictionary<string, buildingElement> buildingElementsByName = new Dictionary<string, buildingElement>(buildingElements.Count);
            foreach (buildingElement buildingElement_Temp in buildingElements)
            {
                if (!string.IsNullOrEmpty(buildingElement_Temp?.name))
                {
                    buildingElementsByName[buildingElement_Temp.name] = buildingElement_Temp;
                }
            }

            Dictionary<string, TBD.Construction> constructionsByName = new Dictionary<string, TBD.Construction>(constructions.Count);
            foreach (TBD.Construction construction_Temp in constructions)
            {
                if (!string.IsNullOrEmpty(construction_Temp?.name))
                {
                    constructionsByName[construction_Temp.name] = construction_Temp;
                }
            }

            BuildingReuseCache buildingReuseCache = new BuildingReuseCache(building);

            // -----------------------------------------------------------------------------------------------
            // Nothing named after a physical aperture may be ADOPTED as a reusable definition. TAS's own
            // per-aperture constructions and elements can pass every content gate, so without this the cache
            // would hand one over and twenty windows would end up sharing a definition named after whichever
            // one of them was enumerated first. Their NAMES stay reserved, so a definition created below
            // cannot collide with one.
            // -----------------------------------------------------------------------------------------------
            List<string> names_InstanceNamed = Query.NamesContainingApertureGuid(buildingReuseCache.ConstructionNames().Concat(buildingReuseCache.BuildingElementNames()), apertureGuids);
            int count_RefusedSeeds = buildingReuseCache.RefuseSeededDefinitions(names_InstanceNamed);

            Dictionary<ZoneSurfaceKey, TBD.IZoneSurface> surfaceIndex = building.ZoneSurfaceIndex();

            //A COM-free mirror of the current bindings, advanced after each rebind so a later aperture in this
            //same pass sees the current state - and the input Query.ApertureRebindKeys validates against.
            Dictionary<ZoneSurfaceKey, string> surfaceBindings = surfaceIndex.ToDictionary(x => x.Key, x => x.Value?.buildingElement?.GUID);

            AperturePhysicalIndex aperturePhysicalIndex = Query.AperturePhysicalIndex(apertures);

            //Which panel owns each aperture, built once. NOT read from
            //AdjacencyCluster.GetAperture(guid, out panel): that overload returns EARLY when the aperture is
            //also held as a cluster object in its own right, and leaves its out panel null - which on the
            //gbXML route is every aperture, so every re-stamp would refuse. Shared with
            //Modify.UpdateBuildingElements, which resolves apertures the same way for the same reason.
            AperturePanelIndex aperturePanelIndex = adjacencyCluster.AperturePanelIndex();

            List<KeyValuePair<ZoneSurfaceKey, string>> ambiguities = aperturePhysicalIndex.Ambiguities();
            foreach (KeyValuePair<ZoneSurfaceKey, string> ambiguity in ambiguities)
            {
                notes.Add(NotePrefix_Issue + "Aperture definitions: " + ambiguity.Value);
            }

            //Every element this pass resolved an aperture onto. The sweep below never removes one of these,
            //even if a refusal has momentarily left it carrying no surface.
            HashSet<string> canonicalGuids = new HashSet<string>();

            //Preferred construction name -> the name the construction actually took, for every construction
            //that could NOT have its preferred name because content the resolver may not adopt already held
            //it. See Query.OrphanApertureConstructionNames for what squats them on this route, and
            //Query.SupersededConstructionRenames for why the plain name is then reclaimed rather than left
            //qualified - that base is what the round-tripped ApertureConstruction is NAMED after.
            List<KeyValuePair<string, string>> names_SupersededBy = new List<KeyValuePair<string, string>>();

            int count_Parts = 0;
            int count_Rebound = 0;
            int count_AlreadyCanonical = 0;
            int count_NoStamp = 0;
            int count_RefusedRebind = 0;
            int count_RefusedResolve = 0;
            int count_ShadeStated = 0;

            foreach (Aperture aperture in apertures)
            {
                if (aperture == null)
                {
                    continue;
                }

                //Read ONCE, before either part is rebound: the stamps this pass is about to change are the
                //very ones it reads which element each part is currently on.
                AperturePhysicalIdentity aperturePhysicalIdentity = aperture.AperturePhysicalIdentity();
                if (aperturePhysicalIdentity == null)
                {
                    continue;
                }

                //Frame before pane, the direct export's own ordering, so a name budget is consumed in the
                //same order on both routes.
                foreach (AperturePart aperturePart in new AperturePart[] { AperturePart.Frame, AperturePart.Pane })
                {
                    string buildingElementGuid_From = aperturePhysicalIdentity.BuildingElementGuid(aperturePart);
                    if (string.IsNullOrWhiteSpace(buildingElementGuid_From))
                    {
                        //This aperture states no such part in the TBD at all, or UpdateIds could not match
                        //it. Either way there is no binding to move and nothing to create.
                        count_NoStamp++;
                        continue;
                    }

                    //A pane stating a feature shade is left exactly where it is. Modify.UpdateBuildingElements
                    //runs earlier in this same workflow and already gave such a pane its OWN dedicated,
                    //non-shareable element (its Stage 3 seed gate refuses a shade-carrying element as
                    //reusable) - but BuildingElementDefinition carries no shade field, so this pass's
                    //definition equality cannot see one. Rebinding the pane's surfaces onto a shared
                    //definition here would silently drop its shade, and the sweep below would then delete the
                    //now-surface-less shaded element outright. Skipping the part entirely - no rebind, no
                    //resolve - leaves its surface in place, which is what keeps it out of the sweep too.
                    if (aperturePart == AperturePart.Pane && aperture.TryGetValue(Analytical.ApertureParameter.FeatureShade, out FeatureShade featureShade_Stated) && featureShade_Stated != null)
                    {
                        count_ShadeStated++;
                        continue;
                    }

                    count_Parts++;

                    //The COMPLETE physical set, validated before any definition is created, reserved or
                    //written - so a refusal creates no orphan and moves no surface.
                    List<ZoneSurfaceKey> rebindKeys = Query.ApertureRebindKeys(
                        aperturePhysicalIdentity,
                        aperturePart,
                        aperturePhysicalIndex,
                        surfaceBindings,
                        buildingElementGuid_From,
                        out string refusal_Rebind);

                    if (rebindKeys == null)
                    {
                        notes.Add(NotePrefix_Issue + string.Format("Aperture definitions: SAM aperture '{0}' ({1}) could not have its {2} rebound onto a shared definition; no definition was created and none of its surfaces moved - {3}",
                            aperture.Name, aperture.Guid, aperturePart, refusal_Rebind));
                        count_RefusedRebind++;
                        continue;
                    }

                    if (!building.ResolveApertureDefinition(
                        buildingReuseCache,
                        aperture,
                        aperturePart,
                        materialLibrary,
                        constructionsByName,
                        buildingElementsByName,
                        out TBD.Construction construction_Target,
                        out buildingElement buildingElement_Target,
                        out ConstructionDefinition _,
                        out BuildingElementDefinition _,
                        out bool created_Construction,
                        out bool _,
                        out string refusal_Resolve) || buildingElement_Target == null)
                    {
                        notes.Add(NotePrefix_Issue + string.Format("Aperture definitions: SAM aperture '{0}' ({1}) resolved no shared {2} definition, so it was left on the element TAS created for it - {3}",
                            aperture.Name, aperture.Guid, aperturePart, refusal_Resolve ?? "no reason was reported."));
                        count_RefusedResolve++;
                        continue;
                    }

                    canonicalGuids.Add(buildingElement_Target.GUID);

                    //The name this construction WANTED. Resolving under any other name means the preferred
                    //one is held by content the resolver may not adopt - and once every surface is bound to
                    //this one, that squatter is referenced by nothing.
                    //
                    //Gated on created_Construction: a name mismatch is only evidence of a REAL collision when
                    //this call is the one that FORCED it, by creating a new object because the preferred name
                    //was already occupied by foreign content. A cache HIT under a different name is ORDINARY
                    //cross-family content reuse - two distinct SAM ApertureConstructions happening to state
                    //identical layers for this part, which Stage 2 has always shared regardless of whose
                    //family created the construction. Treating that as superseded would rename a construction
                    //another aperture already correctly points at, breaking ITS pane/frame base-name pairing
                    //to fix a mismatch that was never there.
                    string name_Preferred = string.Format("{0} {1}", Query.ConstructionNameBase(aperture.ApertureConstruction?.Name), aperturePart.Sufix());
                    if (created_Construction && construction_Target != null && !string.Equals(construction_Target.name, name_Preferred, StringComparison.Ordinal))
                    {
                        if (!names_SupersededBy.Exists(x => x.Key == name_Preferred && x.Value == construction_Target.name))
                        {
                            names_SupersededBy.Add(new KeyValuePair<string, string>(name_Preferred, construction_Target.name));
                        }
                    }

                    if (string.Equals(buildingElement_Target.GUID, buildingElementGuid_From, StringComparison.Ordinal))
                    {
                        //Already on the definition it asks for - a repeated run, or the aperture whose own
                        //element became the canonical one. Zero writes, exactly as a shared definition
                        //requires.
                        count_AlreadyCanonical++;
                        continue;
                    }

                    List<TBD.IZoneSurface> zoneSurfaces_ToRebind = rebindKeys.ConvertAll(x => surfaceIndex[x]);

                    for (int index = 0; index < zoneSurfaces_ToRebind.Count; index++)
                    {
                        zoneSurfaces_ToRebind[index].buildingElement = buildingElement_Target;
                        surfaceBindings[rebindKeys[index]] = buildingElement_Target.GUID;
                    }

                    //The definition binding follows the surfaces. The physical ZoneSurfaceReference stamps are
                    //NOT touched - they name an instance, and this pass moves no instance anywhere.
                    if (!RestampApertureBinding(adjacencyCluster, aperturePanelIndex, aperture.Guid, aperturePart, buildingElement_Target.GUID))
                    {
                        notes.Add(NotePrefix_Issue + string.Format("Aperture definitions: SAM aperture '{0}' ({1}) had its {2} surfaces rebound but could not be found to re-stamp, so its BuildingElementGuid still names the element TAS created for it.",
                            aperture.Name, aperture.Guid, aperturePart));
                    }

                    count_Rebound++;
                }
            }

            // -----------------------------------------------------------------------------------------------
            // THE SWEEP. What TAS created per aperture now holds no surface. TBD has no RemoveBuildingElement,
            // so an orphan is marked and DeleteMarkedBuildingElements does the removal.
            // -----------------------------------------------------------------------------------------------
            Dictionary<string, int> surfaceCounts = new Dictionary<string, int>();
            foreach (KeyValuePair<ZoneSurfaceKey, string> surfaceBinding in surfaceBindings)
            {
                if (string.IsNullOrWhiteSpace(surfaceBinding.Value))
                {
                    continue;
                }

                surfaceCounts.TryGetValue(surfaceBinding.Value, out int count);
                surfaceCounts[surfaceBinding.Value] = count + 1;
            }

            List<buildingElement> buildingElements_Now = building.BuildingElements() ?? new List<buildingElement>();

            List<ApertureBuildingElementUsage> apertureBuildingElementUsages = new List<ApertureBuildingElementUsage>(buildingElements_Now.Count);
            List<string> guids_MarkedAlready = new List<string>();
            foreach (buildingElement buildingElement_Temp in buildingElements_Now)
            {
                if (buildingElement_Temp == null)
                {
                    continue;
                }

                if (buildingElement_Temp.markDelete != 0)
                {
                    guids_MarkedAlready.Add(buildingElement_Temp.GUID);
                }

                surfaceCounts.TryGetValue(buildingElement_Temp.GUID ?? string.Empty, out int count);
                apertureBuildingElementUsages.Add(new ApertureBuildingElementUsage(buildingElement_Temp.GUID, buildingElement_Temp.name, buildingElement_Temp.BEType, count));
            }

            if (guids_MarkedAlready.Count != 0)
            {
                notes.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} building element(s) were ALREADY marked for deletion by something else before this pass, and the sweep below deletes those too: {1}.",
                    guids_MarkedAlready.Count, string.Join(", ", guids_MarkedAlready)));
            }

            List<string> guids_ToDelete = Query.UnusedApertureBuildingElementGuids(apertureBuildingElementUsages, canonicalGuids);

            int count_Deleted = 0;
            int count_Survived = 0;

            //Run the sweep whenever there is EITHER a new orphan of this pass's own OR a pre-existing mark
            //from something else - the note above promises both are swept together, and skipping the call
            //whenever this pass happens to find no new orphan of its own would leave a foreign mark sitting
            //in the TBD despite that promise.
            if (guids_ToDelete.Count != 0 || guids_MarkedAlready.Count != 0)
            {
                HashSet<string> guids_ToDelete_Set = new HashSet<string>(guids_ToDelete);
                foreach (buildingElement buildingElement_Temp in buildingElements_Now)
                {
                    if (buildingElement_Temp != null && buildingElement_Temp.GUID != null && guids_ToDelete_Set.Contains(buildingElement_Temp.GUID))
                    {
                        buildingElement_Temp.markDelete = 1;
                    }
                }

                //DeleteMarkedBuildingElements returns a STATUS, not a count - licensed TAS returns -1 after
                //successfully sweeping 28 elements - so what actually happened is established by re-reading
                //the building and seeing which of the marked GUIDs are gone. Only THIS pass's own orphans are
                //verified this way; a foreign pre-existing mark is swept by the same call but is not this
                //pass's outcome to report a survival count for.
                building.DeleteMarkedBuildingElements();

                if (guids_ToDelete_Set.Count != 0)
                {
                    foreach (buildingElement buildingElement_Temp in building.BuildingElements() ?? new List<buildingElement>())
                    {
                        if (buildingElement_Temp != null && buildingElement_Temp.GUID != null && guids_ToDelete_Set.Contains(buildingElement_Temp.GUID))
                        {
                            count_Survived++;
                        }
                    }

                    count_Deleted = guids_ToDelete.Count - count_Survived;
                }
            }

            //Constructions last, because which ones are still referenced can only be read once the elements
            //that referenced them are gone.
            List<string> names_Referenced = new List<string>();
            foreach (buildingElement buildingElement_Temp in building.BuildingElements() ?? new List<buildingElement>())
            {
                string name_Construction = buildingElement_Temp?.GetConstruction()?.name;
                if (!string.IsNullOrWhiteSpace(name_Construction))
                {
                    names_Referenced.Add(name_Construction);
                }
            }

            List<string> names_Construction = (building.Constructions() ?? new List<TBD.Construction>()).ConvertAll(x => x?.name);

            List<string> names_Orphan = Query.OrphanApertureConstructionNames(names_Construction, names_Referenced, apertureGuids, names_SupersededBy.ConvertAll(x => x.Key), out List<string> names_UnreferencedKept);

            List<string> names_Removed = names_Orphan.Count == 0 ? new List<string>() : (RemoveConstructions(building, names_Orphan) ?? new List<string>());

            // -----------------------------------------------------------------------------------------------
            // RECLAIM the plain names the sweep has just freed. A construction that had to take a
            // signature-qualified name leaves the two halves of one window with DIFFERENT base names. Since
            // APERTURE_HARDENING.md the import no longer PAIRS by that base - it groups geometrically and
            // keys the family on the construction PAIR - but the base is still what the reconstructed
            // ApertureConstruction is NAMED after, so a qualified frame beside a plain pane brings the model's
            // own construction name back mangled.
            // Renaming is safe here and nowhere else: the construction is one this pass created moments ago,
            // elements reference it by COM identity rather than by name, and the document has not been saved.
            // -----------------------------------------------------------------------------------------------
            List<KeyValuePair<string, string>> renames = Query.SupersededConstructionRenames(names_SupersededBy, names_Removed);
            int count_Renamed = 0;
            if (renames.Count != 0)
            {
                foreach (TBD.Construction construction_Temp in building.Constructions() ?? new List<TBD.Construction>())
                {
                    if (construction_Temp?.name == null)
                    {
                        continue;
                    }

                    int index = renames.FindIndex(x => x.Key == construction_Temp.name);
                    if (index != -1)
                    {
                        construction_Temp.name = renames[index].Value;
                        count_Renamed++;
                    }
                }
            }

            // -----------------------------------------------------------------------------------------------
            // The summary, at the front, so a reader sees what the pass achieved before the individual lines.
            // -----------------------------------------------------------------------------------------------
            List<string> notes_Summary = [];
            notes_Summary.Add(string.Format("Aperture definitions: {0} aperture part(s) considered; {1} rebound onto a shared definition, {2} already on one. {3} aperture building element(s) and {4} per-aperture construction(s) removed afterwards.",
                count_Parts, count_Rebound, count_AlreadyCanonical, count_Deleted, names_Removed.Count));

            if (count_RefusedSeeds != 0)
            {
                notes_Summary.Add(string.Format("Aperture definitions: {0} definition(s) already in the TBD are named after a physical aperture and so were not reused as shared definitions; definition-named ones were created instead.", count_RefusedSeeds));
            }

            if (count_NoStamp != 0)
            {
                notes_Summary.Add(string.Format("Aperture definitions: {0} aperture part(s) carry no building element stamp - they state no such part in the TBD, or Updating Ids did not match them - and were left alone.", count_NoStamp));
            }

            if (count_ShadeStated != 0)
            {
                notes_Summary.Add(string.Format("Aperture definitions: {0} pane(s) state a feature shade and were left on their own dedicated element rather than considered for a shared definition.", count_ShadeStated));
            }

            if (count_RefusedRebind != 0)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} aperture part(s) could not be rebound safely and were left on the element TAS created for them; the lines above name each one.", count_RefusedRebind));
            }

            if (count_RefusedResolve != 0)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} aperture part(s) resolved no shared definition at all; the lines above name each one.", count_RefusedResolve));
            }

            if (ambiguities.Count != 0)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} physical surface(s) are claimed by more than one SAM aperture; those apertures were not rebound rather than one of them being picked.", ambiguities.Count));
            }

            if (names_UnreferencedKept.Count != 0)
            {
                notes_Summary.Add(string.Format("Aperture definitions: {0} aperture construction(s) are referenced by nothing but name no physical aperture, so they were KEPT as library definitions: {1}.",
                    names_UnreferencedKept.Count, string.Join(", ", names_UnreferencedKept)));
            }

            if (count_Survived != 0)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} of {1} orphaned aperture building element(s) were marked for deletion but are still in the TBD afterwards.", count_Survived, guids_ToDelete.Count));
            }

            if (names_SupersededBy.Count != 0)
            {
                notes_Summary.Add(string.Format("Aperture definitions: {0} preferred construction name(s) were already held by content this pass may not adopt; the shared construction took its signature-qualified name, the superseded one was removed once nothing referenced it, and {1} plain name(s) were then reclaimed so the import still pairs each window's two halves: {2}.",
                    names_SupersededBy.Count, count_Renamed, string.Join(", ", names_SupersededBy.ConvertAll(x => x.Key))));
            }

            if (count_Renamed != renames.Count)
            {
                notes_Summary.Add(NotePrefix_Issue + string.Format("Aperture definitions: {0} of {1} freed construction name(s) could not be reclaimed, so a window's pane and frame constructions carry different base names; the import still pairs them, but the reconstructed aperture construction takes whichever base name was free rather than the model's own.", renames.Count - count_Renamed, renames.Count));
            }

            notes.InsertRange(0, notes_Summary);

            return true;
        }

        /// <summary>
        /// Re-stamps one aperture part's <c>Pane/FrameBuildingElementGuid</c> to the definition its surfaces
        /// now point at.
        /// <para>
        /// <b>Both shapes an aperture can be held in are updated</b>, because a cluster can hold it either
        /// way and a stale copy is a stamp that names a deleted element. It lives on its PANEL (which is what
        /// <c>panel.Apertures</c>, <c>Query.AperturePhysicalIndex</c> and the membership map all read), and it
        /// may ALSO be a cluster object in its own right.
        /// </para>
        /// <para>
        /// This is deliberately not <c>AdjacencyCluster.GetAperture(guid, out panel)</c>: that overload
        /// returns as soon as the aperture is found as a cluster OBJECT and leaves its out panel null, so on
        /// a model whose apertures are stored that way - every one on this route - the panel copy would never
        /// be written and every re-stamp would refuse.
        /// </para>
        /// </summary>
        /// <returns>True when at least one copy was re-stamped.</returns>
        private static bool RestampApertureBinding(AdjacencyCluster adjacencyCluster, AperturePanelIndex aperturePanelIndex, System.Guid apertureGuid, AperturePart aperturePart, string buildingElementGuid)
        {
            ApertureParameter apertureParameter = aperturePart == AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;

            bool result = false;

            if (aperturePanelIndex != null && aperturePanelIndex.TryGetValue(apertureGuid, out Aperture aperture, out Panel panel))
            {
                aperture.SetValue(apertureParameter, buildingElementGuid);
                panel.RemoveAperture(apertureGuid);
                panel.AddAperture(aperture);
                adjacencyCluster.AddObject(panel);
                result = true;
            }

            Aperture aperture_Object = adjacencyCluster.GetObject<Aperture>(apertureGuid);
            if (aperture_Object != null)
            {
                aperture_Object.SetValue(apertureParameter, buildingElementGuid);
                adjacencyCluster.AddObject(aperture_Object);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// The same pass, taking the model rather than its parts.
        /// </summary>
        public static bool UpdateApertureDefinitions(this Building building, AnalyticalModel analyticalModel, out List<string> notes)
        {
            if (analyticalModel == null)
            {
                notes = [NotePrefix_Issue + "Aperture definitions: no SAM analytical model, so no aperture definition was reused."];
                return false;
            }

            return UpdateApertureDefinitions(building, analyticalModel.AdjacencyCluster, analyticalModel.MaterialLibrary, out notes);
        }
    }
}
