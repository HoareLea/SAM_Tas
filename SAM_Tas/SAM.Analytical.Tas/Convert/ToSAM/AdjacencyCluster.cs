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
        public static AdjacencyCluster ToSAM(this BuildingData buildingData, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable <PanelDataType> panelDataTypes = null)
        {
            if (buildingData == null)
                return null;

            List<ZoneData> zoneDatas = buildingData.ZoneDatas();
            if (zoneDatas == null)
                return null;

            AdjacencyCluster result = new AdjacencyCluster();

            foreach(ZoneData zoneData in zoneDatas)
            {
                if (zoneData == null)
                    continue;

                Space space = zoneData.ToSAM(spaceDataTypes);
                if (space != null)
                    result.AddObject(space);

                List<SurfaceData> surfaceDatas = zoneData.SurfaceDatas();
                if (surfaceDatas == null)
                    continue;

                foreach(SurfaceData surfaceData in surfaceDatas)
                {
                    if (surfaceData == null)
                        continue;

                    Panel panel = surfaceData.ToSAM(panelDataTypes);
                    if (panel == null)
                        continue;

                    result.AddObject(panel);

                    if (space != null)
                        result.AddRelation(space, panel);
                }
            }

            return result;

        }

        public static AdjacencyCluster ToSAM(this SAMTSDDocument sAMTSDDocument, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            return ToSAM(sAMTSDDocument?.TSDDocument, spaceDataTypes, panelDataTypes);
        }

        public static AdjacencyCluster ToSAM(this TSDDocument tSDDocument, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            return ToSAM(tSDDocument?.SimulationData?.GetBuildingData(), spaceDataTypes, panelDataTypes);
        }

        public static AdjacencyCluster ToSAM_AdjacencyCluster(this string path_TSD, IEnumerable<SpaceDataType> spaceDataTypes = null, IEnumerable<PanelDataType> panelDataTypes = null)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
                return null;

            AdjacencyCluster result = null;

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD))
            {
                result = sAMTSDDocument.ToSAM(spaceDataTypes, panelDataTypes);
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

                    TBD.Construction construction_TBD = buildingElement.GetConstruction();

                    if (!dictionary_Construction.TryGetValue(construction_TBD.GUID, out Construction construction) || construction == null)
                    {
                        construction = construction_TBD.ToSAM();
                        construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
                        dictionary_Construction[construction_TBD.GUID] = construction;
                    }

                    List<Panel> panels_Link = null;

                    TBD.IZoneSurface zoneSurface_Link = zoneSurface.linkSurface;
                    if (zoneSurface_Link != null)
                    {
                        dictionary_Panel.TryGetValue(zoneSurface_Link.GUID, out panels_Link);
                    }

                    bool adiabatic = zoneSurface.type == TBD.SurfaceType.tbdNullLink;

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

                        panel.SetValue(PanelParameter.ZoneSurfaceGuid, zoneSurface.GUID);

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

                Dictionary<Guid, List<Polygon3D>> dictionary = new Dictionary<Guid, List<Polygon3D>>();

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
                        apertureConstruction = apertureConstructions[index];

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

                        if(!dictionary.TryGetValue(apertureConstruction.Guid, out List<Polygon3D> polygon3Ds) || polygon3Ds == null)
                        {
                            polygon3Ds = new List<Polygon3D>();
                            dictionary[apertureConstruction.Guid] = polygon3Ds;
                        }

                        polygon3Ds.Add(polygon3D);
                    }
                }

                foreach (KeyValuePair<Guid, List<Polygon3D>> keyValuePair in dictionary)
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

                    List<Polygon3D> polygon3Ds = keyValuePair.Value;
                    polygon3Ds.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));

                    List<Aperture> apertures = new List<Aperture>();
                    while (polygon3Ds.Count > 0)
                    {
                        Polygon3D polygon3D = polygon3Ds[0];
                        polygon3Ds.RemoveAt(0);

                        List<Polygon3D> polygon3Ds_Temp = polygon3Ds.FindAll(x => new Face3D(polygon3D).InRange(x.InternalPoint3D()));

                        Face3D face3D = null;
                        if(polygon3Ds_Temp == null || polygon3Ds_Temp.Count == 0)
                        {
                            face3D = new Face3D(polygon3D);
                        }
                        else
                        {
                            polygon3Ds_Temp.Add(polygon3D);
                            List<Face3D> face3Ds = Geometry.Spatial.Create.Face3Ds(polygon3Ds_Temp);
                            if(face3Ds != null && face3Ds.Count != 0)
                            {
                                if(face3Ds.Count  > 1)
                                {
                                    face3Ds.Sort((x, y) => y.ExternalEdge2D.GetArea().CompareTo(x.ExternalEdge2D.GetArea()));
                                }

                                face3D = face3Ds.FirstOrDefault();
                            }
                        }

                        polygon3Ds_Temp.ForEach(x => polygon3Ds.Remove(x));

                        Aperture aperture = new Aperture(apertureConstruction, face3D);
                        apertures.Add(aperture);

                    }

                    adjacencyCluster.AddApertures(apertures);
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

            //adjacencyCluster.UpdatePanelTypes(groundElevation);

            return adjacencyCluster;
        }
    }
}
