using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;

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

                    Construction construction = dictionary_Construction[construction_TBD.GUID];
                    if (construction == null)
                    {
                        construction = construction_TBD.ToSAM();
                        construction.SetValue(ConstructionParameter.DefaultPanelType, panelType);
                    }

                    foreach (TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Geometry.Spatial.Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        Panel panel = Analytical.Create.Panel(construction, panelType, new Geometry.Spatial.Face3D(polygon3D));
                        if (panel == null)
                        {
                            continue;
                        }

                        adjacencyCluster.AddObject(panel);
                        adjacencyCluster.AddRelation(panel, space);
                    }
                }

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

                    ApertureConstruction apertureConstruction = dictionary_ApertureConstruction[construction_TBD.GUID];
                    if (apertureConstruction == null)
                    {
                        apertureConstruction = construction_TBD.ToSAM_ApertureConstruction(apertureType);
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
                        Geometry.Spatial.Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                        if (polygon3D == null)
                        {
                            continue;
                        }

                        //List<Aperture> apertures = adjacencyCluster.AddApertures(apertureConstruction, new List<Geometry.Spatial.IClosedPlanar3D> { polygon3D })
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

            return adjacencyCluster;
        }
    }
}
