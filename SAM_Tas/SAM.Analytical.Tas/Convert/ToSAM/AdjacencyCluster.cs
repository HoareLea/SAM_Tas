// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;
using SAM.Geometry.Spatial;
using System.Linq;
using System;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {      
        public static AdjacencyCluster ToSAM_AdjacencyCluster(this BuildingData buildingData, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable <PanelDataType> panelDataTypes = null, IEnumerable<string> spaceNames = null)
        {
            if (buildingData == null)
            {
                return null;
            }

            List<ZoneData> zoneDatas = buildingData.ZoneDatas();
            if (zoneDatas == null)
            {
                return null;
            }

            AdjacencyCluster result = new AdjacencyCluster();

            List<ZoneData> zoneDatas_Converted = new List<ZoneData>();
            foreach (ZoneData zoneData in zoneDatas)
            {
                if (zoneData == null)
                {
                    continue;
                }

                if(spaceNames != null && !spaceNames.Contains(zoneData.name))
                {
                    continue;
                }

                zoneDatas_Converted.Add(zoneData);
            }

            //Every requested hourly series of every converted zone, read in ONE day-by-day pass over calendar days
            //1..365 (Query.ZoneResultSeries) instead of one GetAnnualZoneResult per zone and series: once a TSD's
            //decoded year no longer fits TSD.exe's ~550 MB day cache, each annual read decodes the whole year again
            //(~8 s per series on the x30 Part O TSD; this is the TM59 read, ~74 min there). The daily answers over
            //1..365 are, bit for bit, what GetAnnualZoneResult returns - including the -1 TSD pads the days of a
            //part-year simulation with - so this is the same data, part-year files included; see
            //the SAM_Tas PR record.
            List<tsdZoneArray> tsdZoneArrays = new List<tsdZoneArray>();
            if (spaceDataTypes != null)
            {
                foreach (SpaceDataType spaceDataType in spaceDataTypes)
                {
                    tsdZoneArray? tsdZoneArray = spaceDataType.TsdZoneArray();
                    if (tsdZoneArray != null && !tsdZoneArrays.Contains(tsdZoneArray.Value))
                    {
                        tsdZoneArrays.Add(tsdZoneArray.Value);
                    }
                }
            }

            List<Dictionary<tsdZoneArray, float[]>> zoneResultSeries = tsdZoneArrays.Count == 0 || zoneDatas_Converted.Count == 0
                ? null
                : zoneDatas_Converted.ZoneResultSeries(1, 365, tsdZoneArrays);

            for (int i = 0; i < zoneDatas_Converted.Count; i++)
            {
                ZoneData zoneData = zoneDatas_Converted[i];

                Space space = zoneData.ToSAM(spaceDataTypes, zoneResultSeries?[i]);

                //Released as soon as the space holds its values.
                if (zoneResultSeries != null)
                {
                    zoneResultSeries[i] = null;
                }
                if (space != null)
                {
                    result.AddObject(space);
                }

                List<SurfaceData> surfaceDatas = zoneData.SurfaceDatas();
                if (surfaceDatas == null)
                {
                    continue;
                }

                foreach(SurfaceData surfaceData in surfaceDatas)
                {
                    if (surfaceData == null)
                    {
                        continue;
                    }

                    Panel panel = surfaceData.ToSAM(panelDataTypes);
                    if (panel == null)
                    {
                        continue;
                    }

                    result.AddObject(panel);

                    if (space != null)
                    {
                        result.AddRelation(space, panel);
                    }
                }
            }

            return result;

        }

        public static AdjacencyCluster ToSAM_AdjacencyCluster(this SAMTSDDocument sAMTSDDocument, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            return ToSAM_AdjacencyCluster(sAMTSDDocument?.TSDDocument, spaceDataTypes, panelDataTypes);
        }

        public static AdjacencyCluster ToSAM_AdjacencyCluster(this TSDDocument tSDDocument, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            return ToSAM_AdjacencyCluster(tSDDocument?.SimulationData?.GetBuildingData(), spaceDataTypes, panelDataTypes);
        }

        public static AdjacencyCluster ToSAM_AdjacencyCluster(this string path_TSD, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
                return null;

            AdjacencyCluster result = null;

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = sAMTSDDocument.ToSAM_AdjacencyCluster(spaceDataTypes, panelDataTypes);
            }

            return result;
        }

        public static AdjacencyCluster ToSAM(this TBD.Building building)
        {
            return ToSAM(building, null);
        }

        /// <summary>
        /// Backwards-compatible overload - forwards to the <c>ProfileReuseIndex</c> overload with no index, so
        /// callers compiled against the previous
        /// <c>ToSAM(TBD.Building, Dictionary&lt;string, Polygon3D&gt;)</c> signature keep working. An added
        /// optional parameter changes that signature's arity and breaks them at runtime.
        /// </summary>
        public static AdjacencyCluster ToSAM(this TBD.Building building, Dictionary<string, Polygon3D> polygonCache)
        {
            return ToSAM(building, polygonCache, null);
        }

        /// <summary>
        /// AdjacencyCluster build with optional shared polygon3D cache. Pass a non-null
        /// <paramref name="polygonCache"/> to share converted Polygon3D objects with downstream
        /// callers (e.g. <c>Create.SolarModel</c>) — same roomSurface won't be re-marshaled
        /// over COM. Key format: <c>zoneSurface.GUID + "/" + roomSurfaceIndex</c>.
        /// </summary>
        /// <param name="building">The TBD building to import.</param>
        /// <param name="polygonCache">The shared Polygon3D cache described above, or null.</param>
        /// <param name="profileReuseIndex">
        /// The conversion-wide profile reuse index, or null for today's per-zone profile naming. Threaded down
        /// to every internal-condition conversion so the whole cluster references the same shared
        /// <c>ProfileLibrary</c> definitions.
        /// </param>
        public static AdjacencyCluster ToSAM(this TBD.Building building, Dictionary<string, Polygon3D> polygonCache, ProfileReuseIndex profileReuseIndex)
        {
            if (building == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = new ();

            Dictionary<string, Construction> dictionary_Construction = [];
            List<ApertureConstruction> apertureConstructions = [];

            //The reconstructed families, keyed by Query.ApertureConstructionPairKey - the pair of TBD
            //construction identities a physical aperture's two halves carry. Building-wide, so one family
            //stays one object however many zones its windows are spread across.
            Dictionary<string, ApertureConstruction> apertureConstructionsByPairKey = [];

            //double groundElevation = 0;

            Dictionary<string, Space> dictionary_Space = [];
            Dictionary<string, List<Panel>> dictionary_Panel = [];

            //Dictionary<string, List<Tuple<string, string>>> dictionary_Relations = new Dictionary<string, List<Tuple<string, string>>>();

            foreach (TBD.zone zone in building.Zones())
            {
                Space space = zone.ToSAM(out List<InternalCondition> internalConditions, profileReuseIndex);
                if (space == null)
                {
                    continue;
                }

                if(internalConditions != null && internalConditions.Count != 0)
                {
                    //The same rule Space.ToSAM applied when it restored the zone's SAM airflow metadata, stated
                    //once so the two cannot drift onto different conditions.
                    InternalCondition internalCondition = internalConditions[Query.PrimaryInternalConditionIndex(internalConditions)];

                    space.InternalCondition = internalCondition;
                    internalConditions.Remove(internalCondition);
                    internalConditions.ForEach(x => adjacencyCluster.AddObject(x));
                }

                space.SetValue(SpaceParameter.ZoneGuid, zone.GUID);

                //List<Tuple<string, string>> tuples_Relations = new List<Tuple<string, string>>();
                //dictionary_Relations[zone.GUID] = tuples_Relations;

                adjacencyCluster.AddObject(space);

                dictionary_Space[zone.GUID] = space;

                List<TBD.IZoneSurface> zoneSurfaces = zone.ZoneSurfaces();
                if(zoneSurfaces == null)
                {
                    continue;
                }

                foreach(TBD.IZoneSurface zoneSurface in zoneSurfaces)
                {
                    TBD.buildingElement buildingElement = zoneSurface.buildingElement;
                    if(buildingElement == null)
                    {
                        continue;
                    }

                    //tuples_Relations.Add(new Tuple<string, string>(zoneSurface?.GUID, zoneSurface?.linkSurface?.GUID));

                    //Add link surface for internal Panels
                    //zoneSurface.linkSurface

                    PanelType panelType = Query.PanelType(buildingElement.BEType);
                    if (panelType == PanelType.Undefined)
                    {
                        if(buildingElement.BEType != 0)
                        {
                            continue;
                        }
                        panelType = PanelType.Air;
                    }

                    //bool ground = Analytical.Query.Ground(panelType);

                    Construction construction = null;

                    TBD.Construction construction_TBD = buildingElement.GetConstruction();

                    if(construction_TBD != null)
                    {
                        if (!dictionary_Construction.TryGetValue(construction_TBD.GUID, out construction) || construction == null)
                        {
                            construction = construction_TBD.ToSAM();
                            construction.SetValue(Analytical.ConstructionParameter.DefaultPanelType, panelType);
                            dictionary_Construction[construction_TBD.GUID] = construction;
                        }
                    }

                    List<Panel> panels_Link = null;

                    TBD.IZoneSurface zoneSurface_Link = zoneSurface.linkSurface;
                    if (zoneSurface_Link != null)
                    {
                        dictionary_Panel.TryGetValue(zoneSurface_Link.GUID, out panels_Link);
                    }

                    bool adiabatic = zoneSurface.type == TBD.SurfaceType.tbdNullLink;

                    ZoneSurfaceReference zoneSurfaceReference = new (zoneSurface.number, zone.GUID);

                    int roomSurfaceIndex_panel = 0;
                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Polygon3D polygon3D = GetOrConvertPolygon(polygonCache, zoneSurface.GUID, roomSurfaceIndex_panel, roomSurface);
                        roomSurfaceIndex_panel++;
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        Face3D face3D = new Face3D(polygon3D);

                        //if(ground)
                        //{
                        //    groundElevation = Math.Max(groundElevation, face3D.GetBoundingBox().Max.Z);
                        //}

                        Panel panel = null;
                        if (panels_Link != null && panels_Link.Count != 0)
                        {
                            panel = panels_Link.Find(x => face3D.InRange(x.GetInternalPoint3D()));
                            if(panel is null)
                            {
                                panel = panels_Link.Find(x => face3D.InRange(x.GetInternalPoint3D(), Tolerance.MacroDistance));
                                if (panel is null && panels_Link.Count == 1)
                                {
                                    panel = panels_Link[0];
                                }
                            }
                        }

                        if (panel == null)
                        {
                            panel = Analytical.Create.Panel(construction, panelType, face3D);
                        }

                        if (panel == null)
                        {
                            continue;
                        }

                        if(adiabatic)
                        {
                            panel.SetValue(Analytical.PanelParameter.Adiabatic, true);
                        }

                        PanelParameter panelParameter = panel.HasValue(PanelParameter.ZoneSurfaceReference_1) ? PanelParameter.ZoneSurfaceReference_2 : PanelParameter.ZoneSurfaceReference_1;
                        panel.SetValue(panelParameter, zoneSurfaceReference);
                        panel.SetValue(PanelParameter.BuildingElementGuid, buildingElement.GUID);

                        adjacencyCluster.AddObject(panel);
                        adjacencyCluster.AddRelation(panel, space);

                        if(!dictionary_Panel.TryGetValue(zoneSurface.GUID, out List<Panel>  panels))
                        {
                            panels = [];
                            dictionary_Panel[zoneSurface.GUID] = panels;
                        }

                        panels.Add(panel);

                        if (zoneSurface_Link != null)
                        {
                            if (dictionary_Space.TryGetValue(zoneSurface_Link.zone.GUID, out Space space_Link))
                            {
                                adjacencyCluster.AddRelation(panel, space_Link);
                            }
                        }

                    }
                }

                // EVERY aperture surface in this zone, in ONE ungrouped list. Which physical aperture a
                // surface belongs to is decided by GEOMETRY - a frame ring and the pane inset in it - and
                // never by what their constructions happen to be called. The bucketing this replaces put a
                // window's two halves in different groups whenever their construction base names differed,
                // which Stage 2's by-value sharing makes an ordinary outcome: see
                // Query.ApertureConstructionPairKey.
                List<Tuple<Polygon3D, TBD.IZoneSurface>> tuples_Aperture = [];

                foreach(TBD.IZoneSurface zoneSurface in zoneSurfaces)
                {
                    TBD.buildingElement buildingElement = zoneSurface.buildingElement;
                    if (buildingElement == null)
                    {
                        continue;
                    }

                    if (Query.ApertureType(buildingElement.BEType) == ApertureType.Undefined)
                    {
                        continue;
                    }

                    int roomSurfaceIndex_aperture = 0;
                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Polygon3D polygon3D = GetOrConvertPolygon(polygonCache, zoneSurface.GUID, roomSurfaceIndex_aperture, roomSurface);
                        roomSurfaceIndex_aperture++;
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        Aperture aperture = Analytical.Query.Apertures(adjacencyCluster, polygon3D.InternalPoint3D(), 1, Tolerance.MacroDistance)?.FirstOrDefault();
                        if(aperture != null)
                        {
                            //THE SECOND SIDE of an aperture between two zones. The import walks each zone in
                            //turn, so an internal aperture is met twice; the first meeting created it, and
                            //this is the same opening seen from the other room. It used to be skipped
                            //outright, which meant side 2 got no ZoneSurfaceReference at all: the aperture
                            //came back stating one physical surface where the TBD holds two, and an update
                            //resolving that aperture could only ever find one of them.
                            //
                            //Both sides are handed to Modify.AddApertureZoneSurfaceReference, which re-orders
                            //the pair canonically rather than appending to the first free slot, so which zone
                            //the import happened to walk first does not decide which side is _1.
                            AperturePart aperturePart_Side = Query.AperturePart_BuildingElementType(zoneSurface.buildingElement);
                            if (aperturePart_Side != AperturePart.Undefined)
                            {
                                Aperture aperture_Side = adjacencyCluster.GetAperture(aperture.Guid, out Panel panel_Side);
                                if (aperture_Side != null && panel_Side != null)
                                {
                                    aperture_Side.AddApertureZoneSurfaceReference(aperturePart_Side, new ZoneSurfaceReference(zoneSurface.number, zone.GUID), out string _);

                                    panel_Side.RemoveAperture(aperture_Side.Guid);
                                    panel_Side.AddAperture(aperture_Side);
                                    adjacencyCluster.AddObject(panel_Side);
                                }
                            }

                            continue;
                        }

                        tuples_Aperture.Add(new Tuple<Polygon3D, TBD.IZoneSurface>(polygon3D, zoneSurface));
                    }
                }

                // Grouped by the pure, COM-free Query.GroupAperturePolygons rather than in-line: a lone
                // pane with no coincident frame is a genuine one-member group (not an empty one that
                // silently drops every stamp below), and the group's seed is captured before anything
                // is removed, not read back off whatever the shrinking list happens to have at index 0.
                foreach (List<Tuple<Polygon3D, TBD.IZoneSurface>> tuples_Temp in Query.GroupAperturePolygons(tuples_Aperture, Tolerance.MacroDistance))
                {
                    List<TBD.IZoneSurface> zoneSurfaces_Aperture = tuples_Temp.ConvertAll(x => x.Item2);

                    //BEType FIRST, construction name second. TAS keeps which half a surface is on the
                    //ELEMENT; a construction name is, after Stage 2, a shared definition label that
                    //happens to end -pane/-frame by our own convention, and a foreign TBD need not
                    //follow it at all. Asking the element rather than the name also means a window
                    //whose two constructions were named unconventionally still comes back as one pane
                    //and one frame instead of both halves guessed from list position below.
                    TBD.IZoneSurface zoneSurface_Pane = zoneSurfaces_Aperture.Find(x => SurfacePart(x) == AperturePart.Pane);
                    TBD.IZoneSurface zoneSurface_Frame = zoneSurfaces_Aperture.Find(x => SurfacePart(x) == AperturePart.Frame);

                    // ---------------------------------------------------------------------------------
                    // THE FAMILY. Identity is the PAIR of construction identities the two halves carry,
                    // never their names - so two SAM ApertureConstructions sharing one half by value stay
                    // two families, and one family stays one however many zones it appears in.
                    // ---------------------------------------------------------------------------------
                    TBD.Construction construction_Pane = zoneSurface_Pane?.buildingElement?.GetConstruction();
                    TBD.Construction construction_Frame = zoneSurface_Frame?.buildingElement?.GetConstruction();

                    if (construction_Pane == null && construction_Frame == null)
                    {
                        //Neither half states which it is and neither carries a construction. The group's
                        //own seed becomes the pane, exactly as ToSAM_ApertureConstruction has always
                        //treated a construction whose name carries no part suffix.
                        construction_Pane = zoneSurfaces_Aperture.Count == 0 ? null : zoneSurfaces_Aperture[0]?.buildingElement?.GetConstruction();
                        if (construction_Pane == null)
                        {
                            continue;
                        }
                    }

                    //The apertureType comes from the PANE's element where there is one: a door leaf is
                    //BEType 14 (Door) while its frame is 15 (Window), so reading whichever half came first
                    //could type a door as a window.
                    TBD.buildingElement buildingElement_Type = zoneSurface_Pane?.buildingElement ?? zoneSurface_Frame?.buildingElement ?? zoneSurfaces_Aperture.FirstOrDefault()?.buildingElement;
                    ApertureType apertureType = Query.ApertureType(buildingElement_Type == null ? 0 : buildingElement_Type.BEType);
                    if (apertureType == ApertureType.Undefined)
                    {
                        apertureType = ApertureType.Window;
                    }

                    string pairKey = Query.ApertureConstructionPairKey(ConstructionKey(construction_Pane), ConstructionKey(construction_Frame));
                    if (!apertureConstructionsByPairKey.TryGetValue(pairKey, out ApertureConstruction apertureConstruction) || apertureConstruction == null)
                    {
                        string name = Query.ApertureConstructionName(
                            Query.ApertureConstructionNameBase(construction_Pane?.name),
                            Query.ApertureConstructionNameBase(construction_Frame?.name),
                            apertureConstructions.ConvertAll(x => x.Name));

                        apertureConstruction = new ApertureConstruction(
                            Guid.NewGuid(),
                            name,
                            apertureType,
                            construction_Pane == null ? null : construction_Pane.ToSAM_ConstructionLayers(),
                            construction_Frame == null ? null : construction_Frame.ToSAM_ConstructionLayers());

                        apertureConstructions.Add(apertureConstruction);
                        apertureConstructionsByPairKey[pairKey] = apertureConstruction;
                    }

                    Face3D face3D = null;
                    if (tuples_Temp.Count == 1)
                    {
                        face3D = new Face3D(tuples_Temp[0].Item1);
                    }
                    else
                    {
                        List<Face3D> face3Ds = Geometry.Spatial.Create.Face3Ds(tuples_Temp.ConvertAll(x => x.Item1));
                        if (face3Ds != null && face3Ds.Count != 0)
                        {
                            if (face3Ds.Count > 1)
                            {
                                face3Ds.Sort((x, y) => y.ExternalEdge2D.GetArea().CompareTo(x.ExternalEdge2D.GetArea()));
                            }

                            face3D = face3Ds.FirstOrDefault();
                        }
                    }

                    Aperture aperture_New = new Aperture(apertureConstruction, face3D);

                    //TODO: New code added to include Aperture Guid TO BE CHECKED 2023.01.30
                    if (zoneSurfaces_Aperture.Count != 0)
                    {
                        if (zoneSurface_Pane == null || zoneSurface_Frame == null)
                        {
                            //The -pane/-frame naming convention, for a surface whose element type states
                            //nothing usable. Only ever fills a slot the element types left empty.
                            foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces_Aperture)
                            {
                                string name = zoneSurface?.buildingElement?.GetConstruction()?.name;
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    continue;
                                }

                                if (zoneSurface_Pane == null && name.EndsWith("-pane"))
                                {
                                    zoneSurface_Pane = zoneSurface;
                                }

                                if (zoneSurface_Frame == null && name.EndsWith("-frame"))
                                {
                                    zoneSurface_Frame = zoneSurface;
                                }

                                if (zoneSurface_Frame != null && zoneSurface_Pane != null)
                                {
                                    break;
                                }
                            }
                        }

                        if (zoneSurfaces_Aperture.Count != 1)
                        {
                            //Last resort, and only for a group that really does hold more than one
                            //surface: something in it is the pane and something is the frame, and neither
                            //the element types nor the names would say which. A SINGLETON is deliberately
                            //excluded - a lone pane with no separately written frame ring is not also its
                            //own frame, and stamping it as both made frame-first reference matching
                            //classify the pane as a frame.
                            if (zoneSurface_Frame == null)
                            {
                                zoneSurface_Frame = zoneSurfaces_Aperture[0];
                            }

                            if (zoneSurface_Pane == null)
                            {
                                zoneSurface_Pane = zoneSurfaces_Aperture[0];
                            }
                        }

                        if (zoneSurface_Frame != null)
                        {
                            //Through the one mutator, so this path orders slots by the same rule the
                            //export and UpdateIds use. The aperture is brand new here, so this is its _1;
                            //the adjacent zone's pass adds the other side above.
                            List<TBD.IZoneSurface> zoneSurfaces_Frame = zoneSurfaces_Aperture.FindAll(x => SurfacePart(x) == AperturePart.Frame);
                            if (zoneSurfaces_Frame.Count == 0)
                            {
                                zoneSurfaces_Frame.Add(zoneSurface_Frame);
                            }

                            aperture_New.SetApertureZoneSurfaceReferences(AperturePart.Frame, zoneSurfaces_Frame.ConvertAll(x => new ZoneSurfaceReference(x.number, zone.GUID)), out string _);

                            TBD.buildingElement buildingElement = zoneSurface_Frame.buildingElement;
                            if (buildingElement != null)
                            {
                                aperture_New.SetValue(ApertureParameter.FrameBuildingElementGuid, buildingElement.GUID);
                            }
                        }

                        if (zoneSurface_Pane != null)
                        {
                            List<TBD.IZoneSurface> zoneSurfaces_Pane = zoneSurfaces_Aperture.FindAll(x => SurfacePart(x) == AperturePart.Pane);
                            if (zoneSurfaces_Pane.Count == 0)
                            {
                                zoneSurfaces_Pane.Add(zoneSurface_Pane);
                            }

                            aperture_New.SetApertureZoneSurfaceReferences(AperturePart.Pane, zoneSurfaces_Pane.ConvertAll(x => new ZoneSurfaceReference(x.number, zone.GUID)), out string _);

                            TBD.buildingElement buildingElement = zoneSurface_Pane.buildingElement;
                            if (buildingElement != null)
                            {
                                aperture_New.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElement.GUID);

                                // Import the operable aperture types (TBD ApertureType) assigned to
                                // the pane building element into SAM OpeningProperties, so they
                                // round-trip back out via Modify.SetApertureTypes on export.
                                IOpeningProperties openingProperties = Convert.ToSAM_OpeningProperties(buildingElement);
                                if (openingProperties != null)
                                {
                                    aperture_New.SetValue(Analytical.ApertureParameter.OpeningProperties, openingProperties);
                                }
                            }
                        }
                    }

                    adjacencyCluster.AddAperture(aperture_New, tolerance_Distance: Tolerance.MacroDistance);
                }

                if (internalConditions != null)
                {
                    foreach (InternalCondition internalCondition in internalConditions)
                    {
                        adjacencyCluster.AddObject(internalCondition);
                        adjacencyCluster.AddRelation(space, internalCondition);
                    }
                }
            }

            List<TBD.ZoneGroup> zoneGroups = building.ZoneGroups();
            if(zoneGroups != null && zoneGroups.Count != 0)
            {
                foreach(TBD.ZoneGroup zoneGroup in zoneGroups)
                {
                    Zone zone = new Zone(zoneGroup.name);
                    zone.SetValue(Analytical.ZoneParameter.ZoneCategory, zoneGroup.description);
                    zone.SetValue(ZoneParameter.TBDZoneGroup, Query.TBDZoneGroup(zoneGroup.type));

                    adjacencyCluster.AddObject(zone);

                    List<TBD.zone> zones = zoneGroup.Zones();
                    if(zones != null)
                    {
                        foreach(TBD.zone zone_Temp in zones)
                        {
                            if(!dictionary_Space.TryGetValue(zone_Temp.GUID, out Space space))
                            {
                                continue;
                            }

                            adjacencyCluster.AddRelation(zone, space);
                        }
                    }

                }
            }

            //adjacencyCluster.UpdatePanelTypes(groundElevation);

            return adjacencyCluster;
        }

        /// <summary>
        /// Convert a TAS roomSurface perimeter to a SAM Polygon3D, consulting the optional
        /// shared cache first. Key format: <c>zoneSurfaceGuid + "/" + roomSurfaceIndex</c>.
        /// If <paramref name="polygonCache"/> is null, performs the conversion uncached.
        /// </summary>
        private static Polygon3D GetOrConvertPolygon(Dictionary<string, Polygon3D> polygonCache, string zoneSurfaceGuid, int roomSurfaceIndex, TBD.IRoomSurface roomSurface)
        {
            string cacheKey = zoneSurfaceGuid + "/" + roomSurfaceIndex;
            if (polygonCache != null && polygonCache.TryGetValue(cacheKey, out Polygon3D cached))
            {
                return cached;
            }

            Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
            if (polygonCache != null && polygon3D != null)
            {
                polygonCache[cacheKey] = polygon3D;
            }
            return polygon3D;
        }

        /// <summary>
        /// Which half of an aperture a physical surface is: the ELEMENT's own <c>BEType</c> first, the
        /// <c>-pane</c>/<c>-frame</c> construction-naming convention only where that says nothing.
        /// </summary>
        private static AperturePart SurfacePart(TBD.IZoneSurface zoneSurface)
        {
            AperturePart result = Query.AperturePart_BuildingElementType(zoneSurface?.buildingElement);
            if (result != AperturePart.Undefined)
            {
                return result;
            }

            string constructionName = zoneSurface?.buildingElement?.GetConstruction()?.name;
            if (!string.IsNullOrWhiteSpace(constructionName))
            {
                if (constructionName.EndsWith("-pane"))
                {
                    return AperturePart.Pane;
                }

                if (constructionName.EndsWith("-frame"))
                {
                    return AperturePart.Frame;
                }
            }

            return AperturePart.Undefined;
        }

        /// <summary>
        /// A TBD construction's identity for <see cref="Query.ApertureConstructionPairKey(string, string)"/>:
        /// its GUID, or its name when it carries none.
        /// </summary>
        private static string ConstructionKey(TBD.Construction construction)
        {
            if (construction == null)
            {
                return null;
            }

            string guid = construction.GUID;
            return string.IsNullOrWhiteSpace(guid) ? construction.name : guid;
        }

    }
}
