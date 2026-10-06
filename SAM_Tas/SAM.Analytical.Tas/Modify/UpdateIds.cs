// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateIds(this AdjacencyCluster adjacencyCluster, TBD.Building building, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (building == null || adjacencyCluster == null)
            {
                return false;
            }

            //Every physical surface each aperture part was matched to, collected over the whole pass and
            //stamped canonically afterwards - one slot per ZONE, ordered by zone GUID - rather than
            //_1-then-_2 in the order the zones happened to be walked. See Query.ApertureZoneSurfaceSides.
            Dictionary<System.Guid, ApertureSurfaceCollector> dictionary_ApertureSurfaces = new Dictionary<System.Guid, ApertureSurfaceCollector>();

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces != null && spaces.Count != 0)
            {
                //Capture every space's existing zone identity BEFORE clearing it. The clearing stays: a stamp
                //that resolves to no zone must never survive (TAS need not have kept the same zone GUIDs, so a
                //stale stamp points somewhere real and wrong). But the refresh below must read the identity
                //captured here - not the just-cleared parameter - so a space whose stamp is still valid, and
                //whose name no longer equals the TAS zone name, still finds its zone. GUID first, exact name
                //only as the compatibility fallback: see Query.ResolvedZone.
                Dictionary<System.Guid, string> zoneGuids_Spaces = Query.SpaceZoneGuids(spaces);

                foreach(Space space in spaces)
                {
                    space.RemoveValue(SpaceParameter.ZoneGuid);
                    adjacencyCluster.AddObject(space);
                }

                List<Panel> panels = adjacencyCluster.GetPanels();
                if(panels != null && panels.Count != 0)
                {
                    foreach (Panel panel in panels)
                    {
                        panel.RemoveValue(PanelParameter.ZoneSurfaceReference_1);
                        panel.RemoveValue(PanelParameter.ZoneSurfaceReference_2);

                        //The APERTURE stamps are cleared too. Only the panel ones were, while the aperture
                        //write below filled _1 when it was empty and _2 otherwise - so a second UpdateIds pass
                        //found _1 already occupied by the previous run, wrote side 1 into _2, and then wrote
                        //side 2 over the top of it. The stale _1 is not merely redundant: TAS need not have
                        //reassigned the same surface number, so it points somewhere real and wrong.
                        List<Aperture> apertures_Panel = panel.Apertures;
                        if (apertures_Panel != null && apertures_Panel.Count != 0)
                        {
                            foreach (Aperture aperture_Panel in apertures_Panel)
                            {
                                if (aperture_Panel == null)
                                {
                                    continue;
                                }

                                //Stamps AND definition bindings, through the one mutator that owns the pairing:
                                //see Modify.RemoveApertureTasIdentity. The binding used to survive this pass
                                //because it was refreshed only where the match below succeeded, so a part this
                                //pass could not re-match carried the PREVIOUS TBD's binding forward as though
                                //it were current state.
                                aperture_Panel.RemoveApertureTasIdentity();

                                panel.RemoveAperture(aperture_Panel.Guid);
                                panel.AddAperture(aperture_Panel);

                                //BOTH shapes an aperture can be held in, cleared together - see
                                //AperturePanelIndex's own note and Modify.UpdateApertureDefinitions'
                                //RestampApertureBinding, which re-stamps both shapes on a SUCCESSFUL match for
                                //exactly this reason. Leaving the standalone cluster object uncleared here
                                //meant that whenever the match below could not re-resolve this aperture, its
                                //panel copy read UNSTAMPED (the honest, current state) while
                                //AdjacencyCluster.GetAperture(guid)/GetObject<Aperture> kept handing back the
                                //stale copy still carrying the previous TBD's binding - the very state this
                                //whole pass exists to eliminate.
                                Aperture aperture_Object = adjacencyCluster.GetObject<Aperture>(aperture_Panel.Guid);
                                if (aperture_Object != null)
                                {
                                    aperture_Object.RemoveApertureTasIdentity();
                                    adjacencyCluster.AddObject(aperture_Object);
                                }
                            }
                        }

                        adjacencyCluster.AddObject(panel);
                    }
                }

                
                List<TBD.zone> zones = building.Zones();
                if (zones != null && zones.Count != 0)
                {
                    //Index and resolve before deriving the translation: only space/zone pairs present on
                    //BOTH sides may contribute to either bounding box. A SAM space added after the TBD was
                    //exported is non-shade, but its panels have no TAS counterpart and would skew an
                    //all-non-shade SAM centroid.
                    Dictionary<string, TBD.zone> zonesByGuid = new Dictionary<string, TBD.zone>(zones.Count);
                    Dictionary<string, TBD.zone> zonesByName = new Dictionary<string, TBD.zone>(zones.Count);
                    foreach (TBD.zone z in zones)
                    {
                        if (z == null) continue;
                        if (!string.IsNullOrWhiteSpace(z.GUID))
                            zonesByGuid[z.GUID] = z;
                        if (!string.IsNullOrWhiteSpace(z.name))
                            zonesByName[z.name] = z;
                    }

                    Dictionary<System.Guid, TBD.zone> zonesBySpaceGuid = new Dictionary<System.Guid, TBD.zone>(spaces.Count);
                    foreach (Space space in spaces)
                    {
                        zoneGuids_Spaces.TryGetValue(space.Guid, out string spaceZoneGuid);
                        TBD.zone zone = Query.ResolvedZone(spaceZoneGuid, space?.Name, zonesByGuid, zonesByName);
                        if (zone != null)
                        {
                            zonesBySpaceGuid[space.Guid] = zone;
                        }
                    }

                    //On the gbXML route TAS's own gbXML import/ExportNew recentres the building footprint on
                    //the origin, so a TBD surface's geometry sits at SAM-coordinates-minus-footprint-centre
                    //and no geometric match below could ever land. Compensate once per pass with the
                    //difference of the two sides' SHARED-zone panel bounding-box centroids - the same
                    //compensation Modify.SetApertureTypes applies for the same reason. Null when either side
                    //has no panels: the match below is then exactly what it was before this compensation.
                    Vector3D translation = null;
                    AdjacencyCluster adjacencyCluster_TBD = building.ToSAM();
                    if (adjacencyCluster_TBD != null)
                    {
                        List<Space> spaces_SAM_Shared = spaces.FindAll(x => x != null && zonesBySpaceGuid.ContainsKey(x.Guid));
                        HashSet<string> zoneGuids_Shared = new HashSet<string>();
                        foreach (TBD.zone zone in zonesBySpaceGuid.Values)
                        {
                            if (!string.IsNullOrWhiteSpace(zone?.GUID))
                            {
                                zoneGuids_Shared.Add(zone.GUID);
                            }
                        }
                        List<Space> spaces_TBD_Shared = adjacencyCluster_TBD.GetSpaces()?.FindAll(x =>
                            x != null && x.TryGetValue(SpaceParameter.ZoneGuid, out string zoneGuid) && zoneGuids_Shared.Contains(zoneGuid));

                        BoundingBox3D boundingBox3D_TBD = UpdateIdsTranslationPanels(adjacencyCluster_TBD, spaces_TBD_Shared).BoundingBox3D();
                        BoundingBox3D boundingBox3D_SAM = UpdateIdsTranslationPanels(adjacencyCluster, spaces_SAM_Shared).BoundingBox3D();
                        if (boundingBox3D_TBD != null && boundingBox3D_SAM != null)
                        {
                            translation = new Vector3D(boundingBox3D_TBD.GetCentroid(), boundingBox3D_SAM.GetCentroid());
                        }
                    }

                    foreach(Space space in spaces)
                    {
                        //The captured pre-clearing stamp is authoritative when it still identifies a zone;
                        //the exact name is the compatibility fallback; no match is a refusal, never a guess.
                        if (!zonesBySpaceGuid.TryGetValue(space.Guid, out TBD.zone zone) || zone == null)
                        {
                            continue;
                        }

                        space.SetValue(SpaceParameter.ZoneGuid, zone.GUID);
                        adjacencyCluster.AddObject(space);

                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if(panels_Space == null || panels_Space.Count == 0)
                        {
                            continue;
                        }

                        List<TBD.IZoneSurface> zoneSurfaces = zone?.ZoneSurfaces();
                        if(zoneSurfaces == null || zoneSurfaces.Count == 0)
                        {
                            continue;
                        }

                        foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces)
                        {
                            Panel panel = zoneSurface.Match(panels_Space, zone.GUID, tolerance, translation);
                            if(panel == null)
                            {
                                continue;
                            }

                            Core.Tas.ZoneSurfaceReference zoneSurfaceReference = new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID);
                            panel.SetValue(PanelParameter.BuildingElementGuid, zoneSurface.buildingElement?.GUID);

                            Core.Tas.ZoneSurfaceReference zoneSurfaceReference_1;

                            List<Aperture> apertures = panel.Apertures;
                            if (apertures != null && apertures.Count != 0)
                            {
                                Aperture aperture = zoneSurface.Match(apertures, zone.GUID, out AperturePart aperturePart, tolerance, translation);
                                if (aperture != null)
                                {
                                    //The physical stamp is COLLECTED here and written after the pass, so both
                                    //sides of an internal aperture are in hand before either takes a slot.
                                    ZoneSurfaceKey zoneSurfaceKey = Query.ZoneSurfaceKey(zone.GUID, zoneSurface.number);
                                    if (zoneSurfaceKey != null && aperturePart != AperturePart.Undefined)
                                    {
                                        if (!dictionary_ApertureSurfaces.TryGetValue(aperture.Guid, out ApertureSurfaceCollector collector))
                                        {
                                            collector = new ApertureSurfaceCollector { PanelGuid = panel.Guid };
                                            dictionary_ApertureSurfaces[aperture.Guid] = collector;
                                        }

                                        collector.Add(aperturePart, zoneSurfaceKey, zoneSurfaceReference);
                                    }

                                    // The definition-binding stamp: which TBD building element this physical
                                    // surface's own element currently is. Re-stamped every pass, unlike the
                                    // ZoneSurfaceReference pair above, because it names a DEFINITION rather
                                    // than a physical instance - many apertures may legitimately share it,
                                    // and it is what lets a later Modify.UpdateBuildingElements pass resolve
                                    // an aperture's element WITHOUT decoding a GUID out of the element's own
                                    // name (which a shared, definition-derived name no longer carries).
                                    ApertureParameter buildingElementGuidParameter = aperturePart == AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;
                                    aperture.SetValue(buildingElementGuidParameter, zoneSurface.buildingElement?.GUID);

                                    panel.RemoveAperture(aperture.Guid);
                                    panel.AddAperture(aperture);
                                    adjacencyCluster.AddObject(panel);
                                    continue;
                                }
                            }

                            if (!panel.TryGetValue(PanelParameter.ZoneSurfaceReference_1, out zoneSurfaceReference_1) || zoneSurfaceReference_1 == null)
                            {
                                panel.SetValue(PanelParameter.ZoneSurfaceReference_1, zoneSurfaceReference);
                            }
                            else
                            {
                                panel.SetValue(PanelParameter.ZoneSurfaceReference_2, zoneSurfaceReference);
                            }

                            adjacencyCluster.AddObject(panel);
                        }
                    }

                }
            }

            // ---------------------------------------------------------------------------------------------
            // The collected physical stamps, written canonically. A slot is a SIDE and a side is a ZONE, so
            // an aperture two surfaces are ordered by zone GUID rather than by whichever zone this pass
            // reached first - which is what makes a repeated UpdateIds a no-op instead of a reshuffle.
            // ---------------------------------------------------------------------------------------------
            foreach (KeyValuePair<System.Guid, ApertureSurfaceCollector> keyValuePair in dictionary_ApertureSurfaces)
            {
                Panel panel = adjacencyCluster.GetObject<Panel>(keyValuePair.Value.PanelGuid);
                Aperture aperture = panel?.GetAperture(keyValuePair.Key);
                if (aperture == null)
                {
                    continue;
                }

                foreach (KeyValuePair<AperturePart, Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference>> keyValuePair_Part in keyValuePair.Value.References)
                {
                    aperture.SetApertureZoneSurfaceReferences(keyValuePair_Part.Key, keyValuePair_Part.Value.Values, out string _);
                }

                panel.RemoveAperture(aperture.Guid);
                panel.AddAperture(aperture);
                adjacencyCluster.AddObject(panel);
            }

            return true;
        }

        private static List<Panel> UpdateIdsTranslationPanels(AdjacencyCluster adjacencyCluster, List<Space> spaces)
        {
            if (adjacencyCluster == null || spaces == null || spaces.Count == 0)
            {
                return null;
            }

            return adjacencyCluster.GetPanels(Core.LogicalOperator.Or, spaces.ToArray());
        }

        /// <summary>
        /// One aperture's physical surfaces as <c>UpdateIds</c> meets them - one entry per part, keyed by the
        /// physical <see cref="ZoneSurfaceKey"/> so the same surface met twice counts once, holding the
        /// reference with the zone GUID spelt as TAS spelt it.
        /// <para>
        /// Collected rather than stamped in place because both sides of an internal aperture have to be in hand
        /// before either can be told which slot it occupies.
        /// </para>
        /// </summary>
        private sealed class ApertureSurfaceCollector
        {
            public System.Guid PanelGuid;

            public Dictionary<AperturePart, Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference>> References { get; } = new Dictionary<AperturePart, Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference>>();

            public void Add(AperturePart aperturePart, ZoneSurfaceKey zoneSurfaceKey, Core.Tas.ZoneSurfaceReference zoneSurfaceReference)
            {
                if (!References.TryGetValue(aperturePart, out Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference> dictionary))
                {
                    dictionary = new Dictionary<ZoneSurfaceKey, Core.Tas.ZoneSurfaceReference>();
                    References[aperturePart] = dictionary;
                }

                dictionary[zoneSurfaceKey] = zoneSurfaceReference;
            }
        }
    }
}
