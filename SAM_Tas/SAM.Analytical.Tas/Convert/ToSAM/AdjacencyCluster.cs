using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;
using SAM.Geometry.Spatial;
using System.Linq;

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
            Dictionary<string, ApertureConstruction> dictionary_ApertureConstruction = new Dictionary<string, ApertureConstruction>();

            foreach (TBD.zone zone in building.Zones())
            {
                Space space = zone.ToSAM(out List<InternalCondition> internalConditions);
                if (space == null)
                {
                    continue;
                }

                adjacencyCluster.AddObject(space);

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

                    PanelType panelType = Query.PanelType(buildingElement.BEType);
                    if (panelType == PanelType.Undefined)
                    {
                        continue;
                    }

                    TBD.Construction construction_TBD = buildingElement.GetConstruction();

                    if (!dictionary_Construction.TryGetValue(construction_TBD.GUID, out Construction construction) || construction == null)
                    {
                        construction = construction_TBD.ToSAM();
                        construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
                        dictionary_Construction[construction_TBD.GUID] = construction;
                    }

                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        Panel panel = Analytical.Create.Panel(construction, panelType, new Face3D(polygon3D));
                        if (panel == null)
                        {
                            continue;
                        }

                        adjacencyCluster.AddObject(panel);
                        adjacencyCluster.AddRelation(panel, space);
                    }
                }

                Dictionary<System.Guid, List<Polygon3D>> dictionary = new Dictionary<System.Guid, List<Polygon3D>>();

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

                    if (!dictionary_ApertureConstruction.TryGetValue(construction_TBD.GUID, out ApertureConstruction apertureConstruction) || apertureConstruction == null)
                    {
                        apertureConstruction = construction_TBD.ToSAM_ApertureConstruction(apertureType);
                        dictionary_ApertureConstruction[construction_TBD.GUID] = apertureConstruction;
                    }
                    else
                    {
                        List<ConstructionLayer> constructionLayers = null;

                        constructionLayers = apertureConstruction.FrameConstructionLayers;
                        if (constructionLayers == null || constructionLayers.Count == 0)
                        {
                            if (construction_TBD.name.EndsWith(AperturePart.Frame.Sufix()))
                            {
                                constructionLayers = ToSAM_ConstructionLayers(construction_TBD);
                                apertureConstruction = new ApertureConstruction(apertureConstruction, apertureConstruction.PaneConstructionLayers, constructionLayers);
                                dictionary_ApertureConstruction[construction_TBD.GUID] = apertureConstruction;
                            }
                        }

                        constructionLayers = apertureConstruction.PaneConstructionLayers;
                        if (constructionLayers == null || constructionLayers.Count == 0)
                        {
                            if (construction_TBD.name.EndsWith(AperturePart.Pane.Sufix()))
                            {
                                constructionLayers = ToSAM_ConstructionLayers(construction_TBD);
                                apertureConstruction = new ApertureConstruction(apertureConstruction, constructionLayers, apertureConstruction.FrameConstructionLayers);
                                dictionary_ApertureConstruction[construction_TBD.GUID] = apertureConstruction;
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

                List<ApertureConstruction> apertureConstructions = new List<ApertureConstruction>();
                foreach(ApertureConstruction apertureConstruction in dictionary_ApertureConstruction.Values)
                {
                    apertureConstructions.Add(apertureConstruction);
                }

                foreach (KeyValuePair<System.Guid, List<Polygon3D>> keyValuePair in dictionary)
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

                        List<Polygon3D> polygon3Ds_Temp = polygon3Ds.FindAll(x => polygon3D.Inside(x));

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
                                    face3Ds.Sort((x, y) => y.GetArea().CompareTo(x.GetArea()));
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

            return adjacencyCluster;
        }
    }
}
