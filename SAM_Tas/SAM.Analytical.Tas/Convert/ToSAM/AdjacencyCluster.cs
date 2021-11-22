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
            foreach (TBD.Construction construction_TBD in building.Constructions())
            {
                Construction construction = construction_TBD.ToSAM();
                if (construction != null)
                {
                    continue;
                }

                dictionary_Construction[construction_TBD.GUID] = construction;
                adjacencyCluster.AddObject(construction);
            }

            foreach (TBD.zone zone in building.Zones())
            {
                Space space = zone.ToSAM(out List<InternalCondition> internalConditions);
                if (space == null)
                {
                    continue;
                }

                adjacencyCluster.AddObject(space);

                foreach (TBD.IZoneSurface zoneSurface in zone.ZoneSurfaces())
                {
                    TBD.buildingElement buildingElement = zoneSurface.buildingElement;
                    TBD.Construction construction_TBD = buildingElement.GetConstruction();

                    PanelType panelType = Query.PanelType(buildingElement.BEType);

                    Construction construction = null;
                    if(construction_TBD != null)
                    {
                        construction = dictionary_Construction[construction_TBD.GUID];
                    }

                    foreach(TBD.IRoomSurface roomSurface in zoneSurface.RoomSurfaces())
                    {
                        Geometry.Spatial.Polygon3D polygon3D = Geometry.Tas.Convert.ToSAM(roomSurface?.GetPerimeter()?.GetFace());
                        if(polygon3D == null)
                        {
                            continue;
                        }

                        Panel panel = Analytical.Create.Panel(construction, panelType, new Geometry.Spatial.Face3D(polygon3D));
                        if(panel == null)
                        {
                            continue;
                        }

                        adjacencyCluster.AddObject(panel);
                        adjacencyCluster.AddRelation(panel, space);
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
