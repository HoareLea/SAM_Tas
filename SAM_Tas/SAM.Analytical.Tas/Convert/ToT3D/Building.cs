using SAM.Core;
using System;
using System.Collections.Generic;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Building ToT3D(this RelationCluster relationCluster, T3DDocument t3DDocument)
        {
            if (t3DDocument == null || relationCluster == null)
                return null;
            
            Building building = t3DDocument.Building;

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
