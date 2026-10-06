// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateBuildingElements(this string path_TBD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateBuildingElements(sAMTBDDocument, analyticalModel);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static bool UpdateBuildingElements(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel)
        {
            if (sAMTBDDocument == null)
                return false;

            return UpdateBuildingElements(sAMTBDDocument.TBDDocument, analyticalModel);
        }

        public static bool UpdateBuildingElements(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            return UpdateBuildingElements(tBDDocument, analyticalModel, out List<string> _);
        }

        /// <summary>
        /// One member of a shared aperture building element: the SAM aperture, and which half of it this
        /// element is (fixed to the element's own <c>BEType</c>-derived part - a stamp claiming the wrong
        /// part is a data inconsistency this never acts on).
        /// </summary>
        private readonly struct ApertureMember
        {
            public readonly Aperture Aperture;
            public readonly AperturePart AperturePart;

            public ApertureMember(Aperture aperture, AperturePart aperturePart)
            {
                Aperture = aperture;
                AperturePart = aperturePart;
            }
        }

        /// <summary>
        /// The same update, reporting the aperture-type refusals the write would otherwise drop silently.
        /// <para>
        /// <b>This is the FIRST of the two aperture-control write paths</b>, and the one that matters most
        /// to a Part O diagnosis: it identifies the SAM aperture(s) a TBD building element stands for, then
        /// writes colour, opening controls and feature shades - reaching apertures the later geometric
        /// <see cref="SetApertureTypes(Building, AdjacencyCluster, out List{string}, double)"/> step can
        /// miss entirely. A schedule written here is already on the aperture type by the time that step
        /// runs, which then reuses it by value. Knowing which of the two paths wrote a schedule - or that
        /// neither did - is the point of reporting from both.
        /// </para>
        /// <para>
        /// <b>Resolution order, per element: (1) the definition-membership map, (2) legacy GUID-in-name
        /// decoding.</b> An aperture stamps <see cref="Analytical.Tas.ApertureParameter.PaneBuildingElementGuid"/>/
        /// <see cref="Analytical.Tas.ApertureParameter.FrameBuildingElementGuid"/> with the GUID of the TBD
        /// element its export bound it to (<c>Modify.UpdateIds</c>) - many apertures may legitimately stamp
        /// the SAME element under Stage 2's sharing, so an element can have more than one member. An element
        /// no aperture stamps (every TAS-authored/legacy TBD element, and any Stage-2 element from before
        /// this stamping existed) falls back to the ORIGINAL single-aperture name decode, unchanged, so
        /// every legacy TBD behaves exactly as it always has.
        /// </para>
        /// <para>
        /// <b>A shared element is never mutated once resolved with more than one current member.</b> Each
        /// member's own required colour, opening-control and feature-shade state is compared against what
        /// the element ALREADY carries (read before this pass writes anything). A member matching stays
        /// with the element - zero writes, since every other member would see them. A member that no longer
        /// matches (its SAM aperture changed since the export that bound it) is SPLIT onto its own
        /// element - reused if an equivalent one already exists, created otherwise (always created when the
        /// member states a feature shade: a shade-carrying element is never shareable) - and only that
        /// member's own physical pane/frame <c>zoneSurface</c>s, resolved from its
        /// <c>Pane/FrameZoneSurfaceReference_1/2</c> stamps, are rebound to it, validated as a complete set
        /// before any one of them moves. The original element is never written to and, if every member
        /// diverges, is simply left in place unused.
        /// </para>
        /// </summary>
        public static bool UpdateBuildingElements(this TBDDocument tBDDocument, AnalyticalModel analyticalModel, out List<string> notes)
        {
            notes = [];

            int count_ScheduleRequested = 0;
            int count_ScheduleWritten = 0;
            int count_GlazingWithoutConstruction = 0;
            int count_GlazingWithoutAperture = 0;
            int count_MembersSplit = 0;
            int count_FeatureShadeStated = 0;
            int count_FeatureShadeWritten = 0;

            if (tBDDocument == null || analyticalModel == null)
                return false;

            Building building = tBDDocument.Building;
            if (building == null)
                return false;

            //RemoveConstructions(building); //Added 05.06.2024 -> Requested By Michal D. to clean existing consructions from TBD file

            UpdateConstructions(tBDDocument, analyticalModel);

            List<buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
                return false;

            List<TBD.Construction> constructions = building.Constructions();
            if (constructions == null || constructions.Count == 0)
                return false;

            // Index constructions once so the per-element loop avoids three O(N) scans + a worst-case O(N*W) word match.
            Dictionary<string, TBD.Construction> constructionByName = new Dictionary<string, TBD.Construction>(constructions.Count);
            List<KeyValuePair<TBD.Construction, HashSet<string>>> constructionWordSets = new List<KeyValuePair<TBD.Construction, HashSet<string>>>(constructions.Count);
            foreach (TBD.Construction c in constructions)
            {
                if (string.IsNullOrWhiteSpace(c?.name))
                    continue;
                constructionByName[c.name] = c;
                HashSet<string> wordSet = new HashSet<string>();
                foreach (string w in c.name.Split(' '))
                {
                    if (!string.IsNullOrWhiteSpace(w))
                        wordSet.Add(w);
                }
                if (wordSet.Count > 0)
                    constructionWordSets.Add(new KeyValuePair<TBD.Construction, HashSet<string>>(c, wordSet));
            }

            // The building's reusable definitions - schedules and aperture types - read once for the whole
            // pass, so an element's opening control is found by definition instead of by re-scanning every
            // aperture type in the building per opening child. Its lifetime is this one open document.
            BuildingReuseCache buildingReuseCache = new BuildingReuseCache(building);

            // The DEFINITION-MEMBERSHIP MAP: TBD element GUID -> every aperture (and which half of it) that
            // currently stamps that element as its binding. Built once from the SAM side; an element absent
            // here was never bound by this stamping (a TAS-authored/legacy element, or a Stage-2 element
            // exported before UpdateIds stamped this) and resolves through the legacy fallback instead.
            Dictionary<string, List<ApertureMember>> membershipByElementGuid = BuildMembershipMap(analyticalModel.AdjacencyCluster);

            // The SURFACE INDEX: (ZoneGuid, SurfaceNumber) -> the physical zoneSurface, for resolving a
            // divergent member's OWN pane/frame surfaces off its ZoneSurfaceReference stamps, so a rebind
            // touches only that member's surfaces and never another aperture sharing the same element.
            Dictionary<ZoneSurfaceKey, TBD.IZoneSurface> surfaceIndex = building.ZoneSurfaceIndex();

            //A COM-free mirror of the current bindings, used to validate a complete rebind plan before a
            //replacement element is looked up, reserved, created, or written. It is advanced after each
            //successful rebind so a later member in this same pass sees the current state.
            Dictionary<ZoneSurfaceKey, string> surfaceBindings = surfaceIndex.ToDictionary(x => x.Key, x => x.Value?.buildingElement?.GUID);

            // The PHYSICAL INDEX, over the SAM side: which aperture, part and side each physical surface
            // belongs to - and, crucially, which physical surfaces MORE THAN ONE aperture claims. The
            // membership map above is keyed by building-element GUID, which many apertures share by design,
            // so it cannot see that; and the surface index is last-wins, so it cannot either. A surface two
            // apertures stamp identifies neither, and rebinding it would move a surface that is arguably the
            // other aperture's. AperturePhysicalIndex detects the collision when it is built and refuses that
            // key from then on, which is what RebindMemberSurfaces consults below.
            AperturePhysicalIndex aperturePhysicalIndex = Query.AperturePhysicalIndex(analyticalModel.AdjacencyCluster?.GetApertures());

            // Aperture GUID -> the PANEL-HELD aperture and its panel. Every aperture read below comes from
            // here rather than AdjacencyCluster.GetAperture: that method answers from GetObject<Aperture>
            // FIRST, so on a model whose apertures are ALSO cluster objects - every one in the licensed
            // fixture - it returns a stale copy carrying neither the colour, the opening properties nor the
            // FeatureShade the user actually set, and its out-panel overload leaves the panel null.
            AperturePanelIndex aperturePanelIndex = analyticalModel.AdjacencyCluster.AperturePanelIndex();

            List<KeyValuePair<ZoneSurfaceKey, string>> ambiguities = aperturePhysicalIndex.Ambiguities();
            foreach (KeyValuePair<ZoneSurfaceKey, string> ambiguity in ambiguities)
            {
                notes.Add(Modify.NotePrefix_Issue + "Building elements: " + ambiguity.Value);
            }

            foreach (buildingElement buildingElement in buildingElements)
            {
                string name = Query.Name(buildingElement);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                TBD.Construction construction;
                if (!constructionByName.TryGetValue(name, out construction))
                {
                    List<TBD.Construction> constructions_Temp = constructions.FindAll(x => !string.IsNullOrWhiteSpace(x?.name) && name.EndsWith(x.name));
                    if(constructions_Temp == null || constructions_Temp.Count == 0)
                    {
                        constructions_Temp = constructions.FindAll(x => !string.IsNullOrWhiteSpace(x?.name) && x.name.EndsWith(name));
                    }

                    if(constructions_Temp != null && constructions_Temp.Count != 0)
                    {
                        constructions_Temp.Sort((x, y) => System.Math.Abs(x.name.Length - name.Length).CompareTo(System.Math.Abs(y.name.Length - name.Length)));
                        construction = constructions_Temp.First();
                    }
                }

                if (construction == null)
                {
                    //The legacy word-set fallback, kept for element names that carry every word of a
                    //construction's name without either side being a literal suffix of the other (a naming
                    //case TAS-authored TBDs genuinely produce). Without it such a glazing element falls
                    //through to the null check below and misses its construction, colour, opening controls
                    //and availability schedules even though the index was built.
                    HashSet<string> elementWords = new HashSet<string>();
                    foreach (string w in name.Split(' '))
                    {
                        if (!string.IsNullOrWhiteSpace(w))
                            elementWords.Add(w);
                    }

                    if (elementWords.Count != 0)
                    {
                        foreach (KeyValuePair<TBD.Construction, HashSet<string>> entry in constructionWordSets)
                        {
                            //Original behaviour: a construction matches when every word of its name appears
                            //in the element's name (the construction's words are a subset of the element's).
                            if (entry.Value.IsSubsetOf(elementWords))
                            {
                                construction = entry.Key;
                                break;
                            }
                        }
                    }
                }

                if (construction == null)
                {
                    //LAST RESORT: the construction the element ITSELF already carries.
                    //
                    //Every match above derives the construction from the element's NAME, and an element this
                    //path created by SPLITTING a shared definition carries a collision-discriminated name -
                    //"Windows: SIM_EXT_GLZ_1F3A0C21 -pane" - which no construction is named and which the
                    //word-set test cannot match either, since the discriminated base is a different word.
                    //Such an element was therefore skipped before the aperture block on every SUBSEQUENT
                    //pass, which meant a split aperture could never be updated again, and in particular could
                    //never MERGE BACK when its definition became equivalent to the shared one once more.
                    //
                    //It always has a construction to fall back on - the split assigned it one when it was
                    //created - so asking the element beats re-deriving from a name. The name matches stay
                    //FIRST, because re-deriving is how an updated construction reaches an element at all;
                    //this only rescues the elements for which no name answer exists.
                    construction = buildingElement.GetConstruction();
                }

                if(construction == null)
                {
                    //A glazing element that never finds a construction leaves this loop before the aperture
                    //block below, so it silently receives no aperture control either - a Part O schedule on
                    //that aperture would never be written on this path at all.
                    if ((TBD.BuildingElementType)buildingElement.BEType == TBD.BuildingElementType.GLAZING)
                    {
                        count_GlazingWithoutConstruction++;
                    }

                    continue;
                }

                TBD.BuildingElementType buildingElementType = (TBD.BuildingElementType)buildingElement.BEType;
                if(buildingElementType == TBD.BuildingElementType.GLAZING || buildingElementType == TBD.BuildingElementType.FRAMEELEMENT)
                {
                    AperturePart aperturePart = AperturePart.Undefined;
                    switch (buildingElementType)
                    {
                        case TBD.BuildingElementType.GLAZING:
                            aperturePart = AperturePart.Pane;
                            break;

                        case TBD.BuildingElementType.FRAMEELEMENT:
                            aperturePart = AperturePart.Frame;
                            break;
                    }

                    List<ApertureMember> members = null;
                    if (!string.IsNullOrWhiteSpace(buildingElement.GUID) && membershipByElementGuid.TryGetValue(buildingElement.GUID, out List<ApertureMember> members_Temp) && members_Temp != null && members_Temp.Count != 0)
                    {
                        //Defensive: a member's own part must agree with this element's part. A mismatch is a
                        //stale or corrupted stamp, not something to act on.
                        members = members_Temp.FindAll(x => x.AperturePart == aperturePart);
                    }

                    if (members != null && members.Count != 0)
                    {
                        // ---------------------------------------------------------------------------------
                        // STAGE 3 PATH: one or more apertures stamp this element as their binding. Split any
                        // that no longer match what the element currently carries onto their own element;
                        // the element itself is never written to on this path.
                        // ---------------------------------------------------------------------------------

                        uint colour_Current = buildingElement.colour;
                        List<ApertureTypeDefinition> assignments_Current = buildingReuseCache.ExistingAssignments(buildingElement).ConvertAll(x => x.Value);

                        //The element's current feature shade, read once like its colour - a pane whose
                        //stated shade no longer matches this has diverged and must be split, or it would
                        //keep the stale shade and never reach SetFeatureShades.
                        FeatureShade featureShade_Current = Convert.ToSAM(buildingElement.GetFeatureShade(1));

                        ConstructionDefinition constructionDefinition_Element = null;

                        foreach (ApertureMember member in members)
                        {
                            if (Query.ApertureMatchesExistingAssignment(member.Aperture, member.AperturePart, colour_Current, assignments_Current, buildingReuseCache.DayTypeNames, featureShade_Current))
                            {
                                //Already what this member asks for - zero writes, exactly as every other
                                //member sharing this element would see them.
                                continue;
                            }

                            //Validate the COMPLETE physical set before any replacement definition can be
                            //created/reserved or receive colour, construction, controls or shade. _1/_2 remain
                            //representative side stamps; AperturePhysicalIdentity.AllKeys carries every face.
                            List<ZoneSurfaceKey> rebindKeys = Query.ApertureRebindKeys(
                                member.Aperture.AperturePhysicalIdentity(),
                                member.AperturePart,
                                aperturePhysicalIndex,
                                surfaceBindings,
                                buildingElement.GUID,
                                out string refusal_Rebind);

                            if (rebindKeys == null)
                            {
                                notes.Add(Modify.NotePrefix_Issue + string.Format("Building elements: SAM aperture '{0}' ({1}) could not be rebound; no replacement definition was created and none of its surfaces moved - {2}",
                                    member.Aperture.Name, member.Aperture.Guid, refusal_Rebind));
                                continue;
                            }

                            if (!aperturePanelIndex.TryGetValue(member.Aperture.Guid, out Aperture aperture_ToRestamp, out Panel panel_ToRestamp))
                            {
                                notes.Add(Modify.NotePrefix_Issue + string.Format("Building elements: SAM aperture '{0}' ({1}) could not be found in its panel before rebinding; no replacement definition was created and none of its surfaces moved.",
                                    member.Aperture.Name, member.Aperture.Guid));
                                continue;
                            }

                            List<TBD.IZoneSurface> zoneSurfaces_ToRebind = rebindKeys.ConvertAll(x => surfaceIndex[x]);

                            //The element's own construction, read once and reused for every divergent
                            //member of this element - construction identity on this route comes from the
                            //by-name match above, not from anything an individual aperture states.
                            if (constructionDefinition_Element == null)
                            {
                                constructionDefinition_Element = construction.ConstructionDefinition(out string _);
                            }

                            BuildingElementDefinition buildingElementDefinition_Required = member.Aperture.BuildingElementDefinition(member.AperturePart, constructionDefinition_Element, buildingReuseCache.DayTypeNames, out string _);

                            //A pane stating a feature shade must land on its OWN newly created element: a
                            //shade-carrying element is never shareable (the seed gate refuses it), so a
                            //cache hit would be a shade-less element the shade could not be written to
                            //without spreading it to every other member sharing it.
                            FeatureShade featureShade_Required = null;
                            bool featureShade_Stated = member.AperturePart == AperturePart.Pane && member.Aperture.TryGetValue(Analytical.ApertureParameter.FeatureShade, out featureShade_Required) && featureShade_Required != null;

                            buildingElement buildingElement_Target = featureShade_Stated ? null : buildingReuseCache.FindApertureBuildingElement(buildingElementDefinition_Required);
                            bool created = false;
                            if (buildingElement_Target == null)
                            {
                                string name_ApertureConstruction = member.Aperture.ApertureConstruction?.Name;

                                //A shade-stated member names its own element shade-aware: the plain
                                //two-name budget derives IDENTICAL names for every shade split of one
                                //definition (the signature excludes the shade), so a second split - or a
                                //re-split after another shade change - would come back null and leave the
                                //pane bound to an element whose shade no longer matches.
                                string name_Target = featureShade_Stated
                                    ? Query.ShadedBuildingElementName(buildingReuseCache.BuildingElementNames(), buildingElementDefinition_Required, name_ApertureConstruction, featureShade_Required)
                                    : Query.BuildingElementName(buildingReuseCache.BuildingElementNames(), buildingElementDefinition_Required, name_ApertureConstruction, out string _);
                                if (name_Target != null)
                                {
                                    buildingElement_Target = building.AddBuildingElement();
                                    buildingElement_Target.name = name_Target;

                                    buildingReuseCache.ReserveApertureBuildingElement(buildingElement_Target);

                                    buildingElement_Target.SetColor(member.Aperture, member.AperturePart);
                                    buildingElement_Target.BEType = Query.BEType(member.AperturePart);
                                    buildingElement_Target.AssignConstruction(construction);
                                    created = true;

                                    int count_ApertureTypes = 0;
                                    if (member.AperturePart == AperturePart.Pane)
                                    {
                                        count_ApertureTypes = WriteOpeningControl(building, buildingElement_Target, member.Aperture, "the definition-membership map", buildingReuseCache, notes, ref count_ScheduleRequested, ref count_ScheduleWritten);
                                    }

                                    //A shade-stated member's element is never registered: it is about to
                                    //carry a feature shade, and the reuse invariant (the seed gate) is that
                                    //a shade-carrying element is never shared.
                                    if (!featureShade_Stated && buildingElementDefinition_Required != null && buildingElementDefinition_Required.Proven && count_ApertureTypes == buildingElementDefinition_Required.ApertureTypeCount)
                                    {
                                        buildingReuseCache.RegisterApertureBuildingElement(buildingElement_Target, buildingElementDefinition_Required);
                                    }
                                }
                            }

                            if (buildingElement_Target == null)
                            {
                                notes.Add(Modify.NotePrefix_Issue + string.Format("Building elements: SAM aperture '{0}' ({1}) no longer matches TBD building element '{2}' ({3}) it is stamped to, and no name could be chosen for a replacement; it was left bound to the element it no longer matches.",
                                    member.Aperture.Name, member.Aperture.Guid, buildingElement.name, buildingElement.GUID));
                                continue;
                            }

                            //A NEWLY CREATED element's own feature shade follows its one founding member.
                            //(The shade-stated case above never takes the cache, so a found element - which
                            //already has whatever members share it - is never written to here: exactly the
                            //mutation this whole path exists to avoid.)
                            if (created && featureShade_Stated)
                            {
                                count_FeatureShadeStated++;
                                SetFeatureShades(building, buildingElement_Target, featureShade_Required);
                                if (buildingElement_Target.GetFeatureShade(1) != null)
                                {
                                    count_FeatureShadeWritten++;
                                }
                            }

                            RebindMemberSurfaces(
                                analyticalModel.AdjacencyCluster,
                                surfaceBindings,
                                member,
                                buildingElement_Target,
                                rebindKeys,
                                zoneSurfaces_ToRebind,
                                aperture_ToRestamp,
                                panel_ToRestamp);

                            count_MembersSplit++;
                        }

                        //Construction is assigned to every element every pass, exactly as before - it is not
                        //part of what a member can diverge on within this route.
                        buildingElement.AssignConstruction(construction);
                        continue;
                    }

                    // -------------------------------------------------------------------------------------
                    // LEGACY PATH: no aperture stamps this element. Unchanged from before Stage 3 - every
                    // TAS-authored/legacy TBD resolves exactly as it always has, one element to one aperture
                    // decoded from the element's own name.
                    // -------------------------------------------------------------------------------------

                    Aperture aperture = null;
                    if(Query.UniqueNameDecomposition(buildingElement.name, out string prefix, out string name_Temp, out System.Guid? guid, out int id) && guid != null && guid.HasValue)
                    {
                        //The PANEL-HELD aperture. Everything written below - colour, opening controls and the
                        //feature shade - is read off whatever this returns, so resolving through
                        //AdjacencyCluster.GetAperture (cluster object first) silently wrote a stale copy's
                        //state and dropped a stated FeatureShade entirely.
                        aperture = aperturePanelIndex.GetAperture(guid.Value);
                    }

                    if (aperture == null && buildingElementType == TBD.BuildingElementType.GLAZING)
                    {
                        //The name did not decode to a GUID, or it did and the SAM model has no such
                        //aperture. Either way this pane gets no aperture control from this path.
                        count_GlazingWithoutAperture++;
                    }

                    if(aperturePart != AperturePart.Undefined)
                    {
                        if (aperture != null)
                        {
                            buildingElement.SetColor(aperture, aperturePart);
                        }
                        else
                        {
                            buildingElement.colour = Core.Convert.ToUint(Analytical.Query.Color(ApertureType.Window, aperturePart));
                        }
                    }

                    if(aperture != null && aperturePart == AperturePart.Pane)
                    {
                        WriteOpeningControl(building, buildingElement, aperture, "GUID", buildingReuseCache, notes, ref count_ScheduleRequested, ref count_ScheduleWritten);

                        if (aperture.TryGetValue(Analytical.ApertureParameter.FeatureShade, out FeatureShade featureShade) && featureShade != null)
                        {
                            count_FeatureShadeStated++;
                            SetFeatureShades(building, buildingElement, featureShade);

                            //Established by RE-READING the element, not by trusting the writer's return
                            //value - the same discipline the element sweep uses for
                            //DeleteMarkedBuildingElements. A shade that did not land is the whole defect
                            //this counter exists to make visible.
                            if (buildingElement.GetFeatureShade(1) != null)
                            {
                                count_FeatureShadeWritten++;
                            }
                        }
                    }
                }



                buildingElement.AssignConstruction(construction);
            }

            //Summarised at the front, so a reader sees what this path achieved before the individual lines.
            List<string> notes_Summary = [];
            notes_Summary.Add(string.Format("Building elements: {0} opening(s) requested an availability schedule, {1} of those read a schedule back off the TBD profile.", count_ScheduleRequested, count_ScheduleWritten));

            if (count_ScheduleRequested != count_ScheduleWritten)
            {
                notes_Summary.Add(Modify.NotePrefix_Issue + string.Format("Building elements: {0} of {1} requested availability schedules did NOT read one back off the TBD on this path.", count_ScheduleRequested - count_ScheduleWritten, count_ScheduleRequested));
            }

            if (count_GlazingWithoutConstruction != 0)
            {
                notes_Summary.Add(Modify.NotePrefix_Issue + string.Format("Building elements: {0} GLAZING element(s) matched no construction and were skipped before any aperture control was written for them.", count_GlazingWithoutConstruction));
            }

            if (count_GlazingWithoutAperture != 0)
            {
                notes_Summary.Add(Modify.NotePrefix_Issue + string.Format("Building elements: {0} GLAZING element(s) did not resolve to a SAM aperture from their own name, so no aperture control was written for them on this path.", count_GlazingWithoutAperture));
            }

            if (ambiguities.Count != 0)
            {
                notes_Summary.Add(Modify.NotePrefix_Issue + string.Format("Building elements: {0} physical surface(s) are claimed by more than one SAM aperture; those apertures were not rebound rather than one of them being picked.", ambiguities.Count));
            }

            if (count_FeatureShadeStated != 0 || count_FeatureShadeWritten != 0)
            {
                notes_Summary.Add(string.Format("Building elements: {0} pane(s) stated a feature shade, {1} of those had one written onto their TBD element.", count_FeatureShadeStated, count_FeatureShadeWritten));
            }

            if (count_FeatureShadeStated != count_FeatureShadeWritten)
            {
                notes_Summary.Add(Modify.NotePrefix_Issue + string.Format("Building elements: {0} of {1} stated feature shade(s) were NOT written; those panes carry no shade in the TBD.", count_FeatureShadeStated - count_FeatureShadeWritten, count_FeatureShadeStated));
            }

            if (count_MembersSplit != 0)
            {
                notes_Summary.Add(string.Format("Building elements: {0} aperture(s) no longer matched the shared element they were stamped to and were split onto their own element.", count_MembersSplit));
            }

            notes.InsertRange(0, notes_Summary);

            return true;
        }

        /// <summary>
        /// Writes a pane's opening control and narrates any availability-schedule request that did not
        /// arrive, under whichever resolution method identified <paramref name="aperture"/> - reused by both
        /// the definition-membership path and the legacy GUID-decode path so the diagnosis reads the same
        /// way regardless of which one found the aperture. Returns how many aperture types the write
        /// produced.
        /// </summary>
        private static int WriteOpeningControl(Building building, buildingElement buildingElement, Aperture aperture, string resolvedBy, BuildingReuseCache buildingReuseCache, List<string> notes, ref int count_ScheduleRequested, ref int count_ScheduleWritten)
        {
            if (!aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties))
            {
                return 0;
            }

            List<TBD.ApertureType> apertureTypes = SetApertureTypes(building, buildingElement, openingProperties, out List<string> notes_Temp, out List<int> childIndices, null, buildingReuseCache);
            notes.AddRange(notes_Temp ?? []);

            //Only an aperture that states an availability schedule is tracked here, and only a child whose
            //schedule did NOT end up on the aperture type its own write returned is narrated. A successful
            //write contributes to the counters and says nothing, so an ordinary run does not put one remark
            //on the canvas per window.
            if (TryDescribeScheduleRequest(openingProperties, out string description_Request))
            {
                List<bool> scheduleRequests = openingProperties.OpeningScheduleRequests();
                int count_Requested = scheduleRequests.FindAll(x => x).Count;
                count_ScheduleRequested += count_Requested;

                ScheduleDeliveryByChild(apertureTypes, childIndices, scheduleRequests.Count, out bool[] delivered, out TBD.ApertureType[] apertureTypesByChild);

                List<int> undelivered = openingProperties.UndeliveredOpeningScheduleRequests(delivered);
                count_ScheduleWritten += count_Requested - undelivered.Count;

                foreach (int childIndex in undelivered)
                {
                    TBD.ApertureType apertureType = apertureTypesByChild[childIndex];
                    notes.Add(Modify.NotePrefix_Issue + string.Format("Building elements: TBD building element '{0}' ({1}) resolved SAM aperture '{2}' ({3}) by {4}; {5}; requested {6}; but the schedule for {7} did not arrive - {8}",
                        buildingElement.name,
                        buildingElement.GUID,
                        aperture.Name,
                        aperture.Guid,
                        resolvedBy,
                        DescribeOpeningProperties(openingProperties),
                        description_Request,
                        scheduleRequests.Count == 1 ? "the opening" : string.Format("opening {0} of {1}", childIndex + 1, scheduleRequests.Count),
                        apertureType == null
                            ? "its write was refused - the refusal reported alongside this line names the reason."
                            : string.Format("aperture type '{0}' carries no schedule afterwards - it read back as {1}", apertureType.name, DescribeApertureTypeProfile(apertureType) ?? "NO PROFILE - the aperture type came back without a TBD profile to read.")));
                }
            }

            return apertureTypes == null ? 0 : apertureTypes.Count;
        }

        /// <summary>
        /// Rebinds ONLY <paramref name="member"/>'s prevalidated complete physical pane/frame surface set to
        /// <paramref name="buildingElement_To"/>, then
        /// re-stamps the member's own <c>Pane/FrameBuildingElementGuid</c> to the new binding.
        /// <para>
        /// <b>The complete intended surface set is resolved and validated before anything is rebound.</b>
        /// A two-sided member whose second surface is missing or stale must not leave its first surface
        /// already moved while the second stays behind - the aperture would be split across the old and new
        /// elements, with the stamp below then calling the new element authoritative over a surface still
        /// bound to the old one. Any failure therefore rebinds NONE of the member's surfaces and leaves its
        /// stamp untouched. A surface it cannot resolve, or whose CURRENT element does not match what the
        /// member claims (a stale stamp), is refused rather than guessed at.
        /// </para>
        /// </summary>
        private static void RebindMemberSurfaces(
            AdjacencyCluster adjacencyCluster,
            Dictionary<ZoneSurfaceKey, string> surfaceBindings,
            ApertureMember member,
            buildingElement buildingElement_To,
            List<ZoneSurfaceKey> rebindKeys,
            List<TBD.IZoneSurface> zoneSurfaces_ToRebind,
            Aperture aperture_ToRestamp,
            Panel panel_ToRestamp)
        {
            for (int index = 0; index < zoneSurfaces_ToRebind.Count; index++)
            {
                zoneSurfaces_ToRebind[index].buildingElement = buildingElement_To;
                surfaceBindings[rebindKeys[index]] = buildingElement_To.GUID;
            }

            ApertureParameter guidParameter = member.AperturePart == AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;

            aperture_ToRestamp.SetValue(guidParameter, buildingElement_To.GUID);
            panel_ToRestamp.RemoveAperture(aperture_ToRestamp.Guid);
            panel_ToRestamp.AddAperture(aperture_ToRestamp);
            adjacencyCluster.AddObject(panel_ToRestamp);
        }

        /// <summary>
        /// TBD element GUID -> every aperture (and which half of it) currently stamping that element as its
        /// binding, read from <see cref="Analytical.Tas.ApertureParameter.PaneBuildingElementGuid"/>/
        /// <see cref="Analytical.Tas.ApertureParameter.FrameBuildingElementGuid"/> across every aperture in
        /// the model. An aperture stamping neither is absent from every entry - it resolves through the
        /// legacy fallback instead.
        /// </summary>
        private static Dictionary<string, List<ApertureMember>> BuildMembershipMap(AdjacencyCluster adjacencyCluster)
        {
            Dictionary<string, List<ApertureMember>> result = new Dictionary<string, List<ApertureMember>>();

            List<Panel> panels = adjacencyCluster?.GetPanels();
            if (panels == null)
            {
                return result;
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

                    Add(ApertureParameter.PaneBuildingElementGuid, AperturePart.Pane);
                    Add(ApertureParameter.FrameBuildingElementGuid, AperturePart.Frame);

                    void Add(ApertureParameter guidParameter, AperturePart aperturePart)
                    {
                        if (!aperture.TryGetValue(guidParameter, out string buildingElementGuid) || string.IsNullOrWhiteSpace(buildingElementGuid))
                        {
                            return;
                        }

                        if (!result.TryGetValue(buildingElementGuid, out List<ApertureMember> members))
                        {
                            members = new List<ApertureMember>();
                            result[buildingElementGuid] = members;
                        }

                        members.Add(new ApertureMember(aperture, aperturePart));
                    }
                }
            }

            return result;
        }

    }
}
