using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Sets Apertures Types by matching geometry
        /// </summary>
        /// <param name="building"></param>
        /// <param name="adjacencyCluster"></param>
        /// <param name="tolerance"></param>
        public static void SetApertureTypes(this TBD.Building building, AdjacencyCluster adjacencyCluster, double tolerance = Core.Tolerance.Distance)
        {
            if(building == null || adjacencyCluster == null)
            {
                return;
            }

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
            {
                return;
            }

            AdjacencyCluster adjacencyCluster_Temp = building?.ToSAM();
            if(adjacencyCluster_Temp == null)
            {
                return;
            }

            BoundingBox3D boundingBox3D_Temp = adjacencyCluster_Temp.GetPanels().BoundingBox3D();
            BoundingBox3D boundingBox3D = adjacencyCluster.GetPanels().BoundingBox3D();
            adjacencyCluster_Temp.Transform(Transform3D.GetTranslation(new Vector3D(boundingBox3D_Temp.GetCentroid(), boundingBox3D.GetCentroid())));

            List<Panel> panels_Temp = adjacencyCluster_Temp.GetPanels();
            if(panels_Temp == null || panels_Temp.Count == 0)
            {
                return;
            }

            foreach(Panel panel_Temp in panels_Temp)
            {
                List<Aperture> apertures_Temp = panel_Temp?.Apertures;
                if(apertures_Temp == null || apertures_Temp.Count == 0)
                {
                    continue;
                }

                foreach(Aperture aperture_Temp in apertures_Temp)
                {

                    Point3D point3D = aperture_Temp.GetFace3D().GetInternalPoint3D(tolerance);

                    if(!aperture_Temp.TryGetValue(ApertureParameter.PaneBuildingElementGuid, out string GUID) || string.IsNullOrWhiteSpace(GUID))
                    {
                        continue;
                    }

                    Aperture aperture = adjacencyCluster.Apertures(point3D)?.FirstOrDefault();
                    if(aperture == null)
                    {
                        continue;
                    }

                    if(!aperture.TryGetValue(SAM.Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                    {
                        continue;
                    }

                    TBD.buildingElement buildingElement = buildingElements.Find(x => x.GUID == GUID);
                    if(buildingElement == null)
                    {
                        continue;
                    }

                    TBD.ApertureType apertureType = SetApertureType(building, buildingElement, openingProperties);
                }
            }
        }
  }
}