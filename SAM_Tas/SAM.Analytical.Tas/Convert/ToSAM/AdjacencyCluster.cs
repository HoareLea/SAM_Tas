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
    }
}
