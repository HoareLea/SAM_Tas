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
                foreach(Space space in spaces)
                {
                    space.RemoveValue(SpaceParameter.ZoneGuid);
                    adjacencyCluster.AddObject(space);
                }

                List<Panel> panels = adjacencyCluster.GetPanels();
                if(panels != null && panels.Count != 0)
                {
                    foreach (Panel panel in panels)
                    {
                        panel.RemoveValue(PanelParameter.ZoneSurfaceReference_1);
                        panel.RemoveValue(PanelParameter.ZoneSurfaceReference_2);
                        adjacencyCluster.AddObject(panel);
                    }
                }
                
                List<TBD.zone> zones = building.Zones();
                if (zones != null && zones.Count != 0)
                {
                    foreach(Space space in spaces)
                    {
                        TBD.zone zone = space.Match(zones);
                        if(zone == null)
                        {
                            continue;
                        }

                        space.SetValue(SpaceParameter.ZoneGuid, zone.GUID);
                        adjacencyCluster.AddObject(space);

                        List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                        if(panels_Space == null || panels_Space.Count == 0)
                        {
                            continue;
                        }

                        List<TBD.IZoneSurface> zoneSurfaces = zone?.ZoneSurfaces();
                        if(zoneSurfaces == null || zoneSurfaces.Count == 0)
                        {
                            continue;
                        }

                        foreach (TBD.IZoneSurface zoneSurface in zoneSurfaces)
                        {
                            Panel panel = zoneSurface.Match(panels_Space, tolerance);
                            if(panel == null)
                            {
                                continue;
                            }

                            Core.Tas.ZoneSurfaceReference zoneSurfaceReference = new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID);
                            panel.SetValue(PanelParameter.BuildingElementGuid, zoneSurface.buildingElement?.GUID);

                            Core.Tas.ZoneSurfaceReference zoneSurfaceReference_1;

                            List<Aperture> apertures = panel.Apertures;
                            if (apertures != null && apertures.Count != 0)
                            {
                                Aperture aperture = zoneSurface.Match(apertures, out AperturePart aperturePart, tolerance);
                                if (aperture != null)
                                {
                                    ApertureParameter apertureParameter_1 = aperturePart == AperturePart.Frame ? ApertureParameter.FrameZoneSurfaceReference_1 : ApertureParameter.PaneZoneSurfaceReference_1;
                                    ApertureParameter apertureParameter_2 = aperturePart == AperturePart.Frame ? ApertureParameter.FrameZoneSurfaceReference_2 : ApertureParameter.PaneZoneSurfaceReference_2;
                                    if (!aperture.TryGetValue(apertureParameter_1, out zoneSurfaceReference_1) || zoneSurfaceReference_1 == null)
                                    {
                                        aperture.SetValue(apertureParameter_1, zoneSurfaceReference);
                                    }
                                    else
                                    {
                                        aperture.SetValue(apertureParameter_2, zoneSurfaceReference);
                                    }

                                    panel.RemoveAperture(aperture.Guid);
                                    panel.AddAperture(aperture);
                                    adjacencyCluster.AddObject(panel);
                                    continue;
                                }
                            }

                            if (!panel.TryGetValue(PanelParameter.ZoneSurfaceReference_1, out zoneSurfaceReference_1) || zoneSurfaceReference_1 == null)
                            {
                                panel.SetValue(PanelParameter.ZoneSurfaceReference_1, zoneSurfaceReference);
                            }
                            else
                            {
                                panel.SetValue(PanelParameter.ZoneSurfaceReference_2, zoneSurfaceReference);
                            }

                            adjacencyCluster.AddObject(panel);
                        }
                    }

                }
            }

            return true;
        }
    }
}