using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateIds(this AdjacencyCluster adjacencyCluster, TBD.Building building, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (building == null || adjacencyCluster == null)
            {
                return false;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces != null && spaces.Count != 0)
            {
                List<TBD.zone> zones = building.Zones();
                if (zones != null && zones.Count != 0)
                {
                    foreach(Space space in spaces)
                    {
                        TBD.zone zone = space.Match(zones);
                        if(zone != null)
                        {
                            space.SetValue(SpaceParameter.ZoneGuid, zone.GUID);
                            adjacencyCluster.AddObject(space);
                        }

                        List<Panel> panels = adjacencyCluster.GetPanels(space);
                        if(panels != null && panels.Count != 0)
                        {
                            List<TBD.IZoneSurface> zoneSurfaces = zone.ZoneSurfaces();
                            if(zoneSurfaces != null && zoneSurfaces.Count != 0)
                            {
                                foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces)
                                {
                                    Panel panel = zoneSurface.Match(panels, tolerance);
                                    if(panel != null)
                                    {
                                        Core.ParameterizedSAMObject parameterizedSAMObject = panel;

                                        List<Aperture> apertures = panel.Apertures;
                                        if(apertures != null && apertures.Count != 0)
                                        {
                                            Aperture aperture = zoneSurface.Match(apertures, tolerance);
                                            if(aperture != null)
                                            {
                                                parameterizedSAMObject = aperture;
                                            }
                                        }

                                        parameterizedSAMObject.SetValue(PanelParameter.ZoneSurfaceGuid, zoneSurface.GUID);
                                        parameterizedSAMObject.SetValue(PanelParameter.ZoneSurfaceNumber, zoneSurface.number);
                                        parameterizedSAMObject.SetValue(PanelParameter.BuildingElementGuid, zoneSurface.buildingElement?.GUID);
                                        adjacencyCluster.AddObject(parameterizedSAMObject);
                                    }
                                }
                            }
                        }
                    }

                }
            }

            return true;
        }
    }
}