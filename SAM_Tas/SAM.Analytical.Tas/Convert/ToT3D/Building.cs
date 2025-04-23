using System;
using System.Collections.Generic;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Building ToT3D(this AnalyticalModel analyticalModel, T3DDocument t3DDocument)
        {
            if (t3DDocument == null || analyticalModel == null)
                return null;

            return ToT3D(analyticalModel.AdjacencyCluster, t3DDocument.Building);

        }

        public static Building ToT3D(this AnalyticalModel analyticalModel, Building building)
        {
            if (building == null || analyticalModel == null)
                return null;

            double northAngle = double.NaN;
            if (analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.NorthAngle, out northAngle))
            {
                building.northAngle = Math.Round(Units.Convert.ToDegrees(northAngle), 1);
            }

            if (building.northAngle < 0.5)
            {
                building.northAngle = 0.5;
            }

            return ToT3D(analyticalModel.AdjacencyCluster, building);

        }

        public static Building ToT3D(this AdjacencyCluster relationCluster, Building building)
        {
            if (building == null || relationCluster == null)
                return null;

            Dictionary<Guid, Aperture> dictionary_Apertures = new Dictionary<Guid, Aperture>();

            List<Panel> panels = relationCluster.GetObjects<Panel>();
            if(panels != null)
            {
                foreach(Panel panel in panels)
                {
                    if (panel == null)
                        continue;

                    List<Aperture> apertures_Panel = panel.Apertures;
                    if(apertures_Panel != null)
                    {
                        foreach (Aperture aperture in apertures_Panel)
                            dictionary_Apertures[aperture.Guid] = aperture;
                    }

                    if(panel.PanelType == PanelType.Shade)
                    {
                        shade shade = panel.ToT3D_Shade(building);
                    }
                    else
                    {
                        Element element = panel.ToT3D(building);
                    }
                }
            }

            List<Aperture> apertures = relationCluster.GetObjects<Aperture>();
            if(apertures != null)
            {
                foreach (Aperture aperture in apertures)
                {
                    if (aperture == null)
                        continue;

                    dictionary_Apertures[aperture.Guid] = aperture;
                }
            }

            foreach(Aperture aperture in dictionary_Apertures.Values)
            {
                window widnow = aperture.ToT3D(building);
            }

            return building;
        }
    }
}
