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

            foreach(ZoneData zoneData in zoneDatas)
            {
                if (zoneData == null)
                {
                    continue;
                }

                if(spaceNames != null && !spaceNames.Contains(zoneData.name))
                {
                    continue;
                }

                Space space = zoneData.ToSAM(spaceDataTypes);
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

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD))
            {
                result = sAMTSDDocument.ToSAM_AdjacencyCluster(spaceDataTypes, panelDataTypes);
            }

            return result;
        }

        public static AdjacencyCluster ToSAM(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();

            Dictionary<string, Construction> dictionary_Construction = new Dictionary<string, Construction>();
            List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();

            //double groundElevation = 0;

            Dictionary<string, Space> dictionary_Space = new Dictionary<string, Space>();
            Dictionary<string, List<Panel>> dictionary_Panel = new Dictionary<string, List<Panel>>();

            //Dictionary<string, List<Tuple<string, string>>> dictionary_Relations = new Dictionary<string, List<Tuple<string, string>>>(); 

            foreach (TBD.zone zone in building.Zones())
            {
                Space space = zone.ToSAM(out List<InternalCondition> internalConditions);
                if (space == null)
                {
                    continue;
                }

                if(internalConditions != null && internalConditions.Count != 0)
                {
                    InternalCondition internalCondition = internalConditions.Find(x => !x.Name.EndsWith("HDD") && !x.Name.EndsWith("CDD"));
                    if(internalCondition == null)
                    {
                        internalCondition = internalConditions[0];
                    }

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
                        continue;
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

                    ZoneSurfaceReference zoneSurfaceReference = new ZoneSurfaceReference(zoneSurface.number, zone.GUID);

                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
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
                            panels = new List<Panel>();
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

                Dictionary<Guid, List<Tuple<Polygon3D, TBD.IZoneSurface>>> dictionary = new Dictionary<Guid, List<Tuple<Polygon3D, TBD.IZoneSurface>>>();

                foreach(TBD.IZoneSurface zoneSurface in zoneSurfaces)
                {
                    TBD.buildingElement buildingElement = zoneSurface.buildingElement;
                    if (buildingElement == null)
                    {
                        continue;
                    }

                    ApertureType apertureType = Query.ApertureType(buildingElement.BEType);
                    if (apertureType == ApertureType.Undefined)
                    {
                        continue;
                    }

                    TBD.Construction construction_TBD = buildingElement.GetConstruction();
                    ApertureConstruction apertureConstruction = construction_TBD.ToSAM_ApertureConstruction(apertureType);
                    int index = apertureConstructions.FindIndex(x => x.Name == apertureConstruction.Name);

                    if (index == -1)
                    {
                        index = apertureConstructions.Count;
                        apertureConstructions.Add(apertureConstruction);
                    }
                    else
                    {
                        apertureConstruction = new ApertureConstruction(apertureConstructions[index].Guid, apertureConstruction, apertureConstruction.Name);
                        apertureConstructions[index] = apertureConstruction;

                        //apertureConstruction = apertureConstructions[index];

                        List<ConstructionLayer> constructionLayers = null;

                        constructionLayers = apertureConstruction.FrameConstructionLayers;
                        if (constructionLayers == null || constructionLayers.Count == 0)
                        {
                            if (construction_TBD.name.EndsWith(AperturePart.Frame.Sufix()))
                            {
                                constructionLayers = ToSAM_ConstructionLayers(construction_TBD);
                                apertureConstruction = new ApertureConstruction(apertureConstruction, apertureConstruction.PaneConstructionLayers, constructionLayers);
                                apertureConstructions[index] = apertureConstruction;
                            }
                        }

                        constructionLayers = apertureConstruction.PaneConstructionLayers;
                        if (constructionLayers == null || constructionLayers.Count == 0)
                        {
                            if (construction_TBD.name.EndsWith(AperturePart.Pane.Sufix()))
                            {
                                constructionLayers = ToSAM_ConstructionLayers(construction_TBD);
                                apertureConstruction = new ApertureConstruction(apertureConstruction, constructionLayers, apertureConstruction.FrameConstructionLayers);
                                apertureConstructions[index] = apertureConstruction;
                            }
                        }
                    }

                    if(apertureConstruction == null)
                    {
                        return null;
                    }

                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        Aperture aperture = Analytical.Query.Apertures(adjacencyCluster, polygon3D.InternalPoint3D(), 1, Tolerance.MacroDistance)?.FirstOrDefault();
                        if(aperture != null)
                        {
                            continue;
                        }

                        if(!dictionary.TryGetValue(apertureConstruction.Guid, out List<Tuple<Polygon3D, TBD.IZoneSurface>> tuples) || tuples == null)
                        {
                            tuples = new List<Tuple<Polygon3D, TBD.IZoneSurface>>();
                            dictionary[apertureConstruction.Guid] = tuples;
                        }

                        tuples.Add(new Tuple<Polygon3D, TBD.IZoneSurface>(polygon3D, zoneSurface));
                    }
                }

                foreach (KeyValuePair<Guid, List<Tuple<Polygon3D, TBD.IZoneSurface>>> keyValuePair in dictionary)
                {
                    if(keyValuePair.Value == null || keyValuePair.Value.Count == 0)
                    {
                        continue;
                    }

                    ApertureConstruction apertureConstruction = apertureConstructions.Find(x => x.Guid == keyValuePair.Key);
                    if(apertureConstruction == null)
                    {
                        continue;
                    }

                    List<Tuple<Polygon3D, TBD.IZoneSurface>> tuples = keyValuePair.Value;
                    tuples.Sort((x, y) => y.Item1.GetArea().CompareTo(x.Item1.GetArea()));

                    while (tuples.Count > 0)
                    {
                        Polygon3D polygon3D = tuples[0].Item1;
                        tuples.RemoveAt(0);

                        List<Tuple<Polygon3D, TBD.IZoneSurface>> tuples_Temp = tuples.FindAll(x => new Face3D(polygon3D).InRange(x.Item1.InternalPoint3D(), Tolerance.MacroDistance));

                        Face3D face3D = null;
                        if (tuples_Temp == null || tuples_Temp.Count == 0)
                        {
                            face3D = new Face3D(polygon3D);
                        }
                        else
                        {
                            tuples_Temp.Add(new Tuple<Polygon3D, TBD.IZoneSurface>(polygon3D, tuples[0].Item2));
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

                        tuples_Temp.ForEach(x => tuples.Remove(x));

                        Aperture aperture = new Aperture(apertureConstruction, face3D);

                        //TODO: New code added to include Aperture Guid TO BE CHECKED 2023.01.30
                        List<TBD.IZoneSurface> zoneSurfaces_Aperture = tuples_Temp.ConvertAll(x => x.Item2);
                        if (zoneSurfaces_Aperture != null && zoneSurfaces_Aperture.Count != 0)
                        {
                            TBD.IZoneSurface zoneSurface_Pane = null;
                            TBD.IZoneSurface zoneSurface_Frame = null;
                            foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces_Aperture)
                            {
                                TBD.Construction construction = zoneSurface?.buildingElement?.GetConstruction();
                                if (construction == null)
                                {
                                    continue;
                                }

                                string name = construction.name;
                                if (string.IsNullOrWhiteSpace(name))
                                {
                                    continue;
                                }

                                if (name.EndsWith("-pane"))
                                {
                                    zoneSurface_Pane = zoneSurface;
                                }

                                if (name.EndsWith("-frame"))
                                {
                                    zoneSurface_Frame = zoneSurface;
                                }

                                if (zoneSurface_Frame != null && zoneSurface_Pane != null)
                                {
                                    break;
                                }

                            }

                            if (zoneSurface_Frame == null)
                            {
                                zoneSurface_Frame = zoneSurfaces_Aperture[0];
                            }

                            if (zoneSurface_Pane == null)
                            {
                                zoneSurface_Pane = zoneSurfaces_Aperture[0];
                            }

                            if (zoneSurface_Frame != null)
                            {
                                ApertureParameter apertureParameter = aperture.HasValue(ApertureParameter.FrameZoneSurfaceReference_1) ? ApertureParameter.FrameZoneSurfaceReference_2 : ApertureParameter.FrameZoneSurfaceReference_1;
                                aperture.SetValue(apertureParameter, new ZoneSurfaceReference(zoneSurface_Frame.number, zone.GUID));

                                TBD.buildingElement buildingElement = zoneSurface_Frame.buildingElement;
                                if (buildingElement != null)
                                {
                                    aperture.SetValue(ApertureParameter.FrameBuildingElementGuid, buildingElement.GUID);
                                }
                            }

                            if (zoneSurface_Pane != null)
                            {
                                ApertureParameter apertureParameter = aperture.HasValue(ApertureParameter.PaneZoneSurfaceReference_1) ? ApertureParameter.PaneZoneSurfaceReference_2 : ApertureParameter.PaneZoneSurfaceReference_1;
                                aperture.SetValue(apertureParameter, new ZoneSurfaceReference(zoneSurface_Pane.number, zone.GUID));

                                TBD.buildingElement buildingElement = zoneSurface_Pane.buildingElement;
                                if (buildingElement != null)
                                {
                                    aperture.SetValue(ApertureParameter.PaneBuildingElementGuid, buildingElement.GUID);
                                }
                            }
                        }

                        adjacencyCluster.AddAperture(aperture, tolerance_Distance: Tolerance.MacroDistance);

                    }
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
    }
}
