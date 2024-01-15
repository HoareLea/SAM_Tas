using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using TBD;
using SAM.Geometry.Object.Spatial;

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
        public static List<TBD.ApertureType> SetApertureTypes(this Building building, AdjacencyCluster adjacencyCluster, double tolerance = Core.Tolerance.Distance)
        {
            if(building == null || adjacencyCluster == null)
            {
                return null;
            }

            List<buildingElement> buildingElements = building.BuildingElements();
            if (buildingElements == null || buildingElements.Count == 0)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster_Temp = building?.ToSAM();
            if(adjacencyCluster_Temp == null)
            {
                return null;
            }

            //in gbXML workflow building is moved (by our interpretation) TBD to 0,0,z this is model wihtout shade so to be able to match by geometry we need to remove shade 
            //take boundin box and move building by vector, watch  if we do SAM to TBD without gbXML if we need to do it
            BoundingBox3D boundingBox3D_Temp = adjacencyCluster_Temp.GetPanels().FindAll(x => !adjacencyCluster_Temp.Shade(x)).BoundingBox3D();
            BoundingBox3D boundingBox3D = adjacencyCluster.GetPanels().FindAll(x => !adjacencyCluster.Shade(x)).BoundingBox3D();
            adjacencyCluster_Temp.Transform(Transform3D.GetTranslation(new Vector3D(boundingBox3D_Temp.GetCentroid(), boundingBox3D.GetCentroid())));

            List<Panel> panels_Temp = adjacencyCluster_Temp.GetPanels();
            if(panels_Temp == null || panels_Temp.Count == 0)
            {
                return null;
            }

            List<TBD.ApertureType> result = new List<TBD.ApertureType>();

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

                    Aperture aperture = adjacencyCluster.Apertures(point3D, 1, Core.Tolerance.MacroDistance)?.FirstOrDefault();
                    if(aperture == null)
                    {
                        continue;
                    }

                    if(!aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                    {
                        continue;
                    }

                    buildingElement buildingElement = buildingElements.Find(x => x.GUID == GUID);
                    if(buildingElement == null)
                    {
                        continue;
                    }

                    List<TBD.ApertureType> apertureTypes = SetApertureTypes(building, buildingElement, openingProperties);
                    if(apertureTypes != null)
                    {
                        result.AddRange(apertureTypes);
                    }
                }
            }

            return result;
        }

        public static List<TBD.ApertureType> SetApertureTypes(this Building building, buildingElement buildingElement, IOpeningProperties openingProperties, string name = null)
        {
            if (building == null || buildingElement == null || openingProperties == null)
            {
                return null;
            }

            if(openingProperties is ISingleOpeningProperties)
            {
                TBD.ApertureType apertureType = SetApertureType(building, buildingElement, (ISingleOpeningProperties)openingProperties, name);
                if(apertureType == null)
                {
                    return null;
                }

                return new List<TBD.ApertureType>() { apertureType };
            }

            if(openingProperties is MultipleOpeningProperties)
            {
                List<ISingleOpeningProperties> singleOpeningProperties = ((MultipleOpeningProperties)openingProperties).SingleOpeningProperties;
                if(singleOpeningProperties == null)
                {
                    return null;
                }

                List<TBD.ApertureType> result = new List<TBD.ApertureType>();
                for(int i = 0; i < singleOpeningProperties.Count; i++)
                {
                    int index = singleOpeningProperties.Count == 1 ? -1 : i + 1;

                    TBD.ApertureType apertureType = SetApertureType(building, buildingElement, singleOpeningProperties[i], index: index);
                    if (apertureType == null)
                    {
                        continue;
                    }

                    result.Add(apertureType);
                }

                return result;
            }


            return null;
        }
    }
}