using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public static partial class Convert
    {
        public static SAPData ToSAP(this AnalyticalModel analyticalModel, string zoneCategory = null, TextMap textMap = null)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            if(textMap == null)
            {
                textMap = Analytical.Query.DefaultInternalConditionTextMap_TM59();
            }

            TM59Manager tM59Manager = new TM59Manager(textMap);

            SAPData result = new SAPData();

            List<Space> spaces_ColdArea = adjacencyCluster.GetSpaces();
            List<Space> spaces = new List<Space>();

            List<Zone> zones = adjacencyCluster.GetZones();
            if(zones != null && zones.Count != 0)
            {
                if(zoneCategory != null)
                {
                    for(int i = zones.Count - 1; i >= 0; i--)
                    {
                        if(zones[i] == null || !zones[i].TryGetValue(Analytical.ZoneParameter.ZoneCategory, out string zoneCategory_Temp) || zoneCategory != zoneCategory_Temp)
                        {
                            zones.RemoveAt(i);
                        }
                    }
                }

                foreach(Zone zone in zones)
                {
                    List<Space> spaces_Zone = adjacencyCluster.GetSpaces(zone);
                    if(spaces_Zone == null)
                    {
                        continue;
                    }

                    foreach(Space space in spaces_Zone)
                    {
                        if(!space.TryGetValue(SpaceParameter.ZoneGuid, out Guid guid))
                        {
                            guid = space.Guid;
                        }

                        result.AddDewlling(zone.Name, guid);
                        spaces_ColdArea.RemoveAll(x => x.Guid == space.Guid);
                        spaces.Add(space);
                    }
                }
            }

            if(spaces_ColdArea != null && spaces_ColdArea.Count != 0)
            {
                foreach(Space space_ColdArea in spaces_ColdArea)
                {
                    InternalCondition internalCondition = space_ColdArea.InternalCondition;
                    if(internalCondition != null && internalCondition.Name != null && internalCondition.Name.Trim().ToLower().Contains("unconditioned"))
                    {
                        if (!space_ColdArea.TryGetValue(SpaceParameter.ZoneGuid, out Guid guid))
                        {
                            guid = space_ColdArea.Guid;
                        }

                        result.AddColdArea("SAM Cold Area", guid);
                        spaces.Add(space_ColdArea);
                    }
                }
            }

            List<Panel> panels = new List<Panel>();
            List<Aperture> apertures = new List<Aperture>();

            foreach (Space space in spaces)
            {
                if(space == null)
                {
                    continue;
                }

                List<Panel> panels_Space = adjacencyCluster.GetPanels(space);
                if (panels_Space != null)
                {
                    foreach (Panel panel_Space in panels_Space)
                    {
                        panels.Add(panel_Space);
                        List<Aperture> apertures_Panel = panel_Space.Apertures;
                        if (apertures_Panel != null)
                        {
                            foreach (Aperture aperture_Panel in apertures_Panel)
                            {
                                apertures.Add(aperture_Panel);
                            }
                        }
                    }
                }

                if (!space.TryGetValue(SpaceParameter.ZoneGuid, out Guid guid))
                {
                    guid = space.Guid;
                }

                if (space.TryGetValue(Analytical.SpaceParameter.LevelName, out string name) && !string.IsNullOrWhiteSpace(name))
                {
                    result.AddStorey(name, guid);
                }

                if(tM59Manager.IsLiving(space) || tM59Manager.IsLiving(space.InternalCondition))
                {
                    result.AddLivingArea(guid);
                }
            }

            foreach(Panel panel in panels)
            {
                if(panel == null)
                {
                    continue;
                }

                if(!panel.TryGetValue(PanelParameter.BuildingElementGuid, out Guid guid) || guid == Guid.Empty)
                {
                    guid = panel.Guid;
                }

                result.AddBuildingElement(guid, panel.PanelType.BuildingElemetType(), false);
            }

            foreach(Aperture aperture in apertures)
            {
                if (aperture == null)
                {
                    continue;
                }

                Guid guid;

                if (!aperture.TryGetValue(ApertureParameter.FrameBuildingElementGuid, out guid) || guid == Guid.Empty)
                {
                    guid = aperture.Guid;
                }

                result.AddBuildingElement(guid, BuildingElementType.Window, false);

                if (!aperture.TryGetValue(ApertureParameter.PaneBuildingElementGuid, out guid) || guid == Guid.Empty)
                {
                    guid = aperture.Guid;
                }

                result.AddBuildingElement(guid, BuildingElementType.Window, false);
            }

            return result;
        }
    }
}
