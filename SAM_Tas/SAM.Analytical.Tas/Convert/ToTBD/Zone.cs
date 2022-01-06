using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static TBD.zone ToTBD(this Space space, AnalyticalModel analyticalModel, TBD.Building building)
        {
            if(space == null || building == null)
            {
                return null;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;

            TBD.zone result = building.AddZone();
            result.name = space.Name;

            TBD.room room = result.AddRoom();

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            List<TBD.Construction> constructions = building.Constructions();

            List<Panel> panels = adjacencyCluster?.GetPanels(space);
            if(panels != null || panels.Count != 0)
            {
                foreach(Panel panel in panels)
                {
                    TBD.zoneSurface zoneSurface = result.AddSurface();
                    zoneSurface.orientation = System.Convert.ToSingle(Geometry.Spatial.Query.Azimuth(panel, Vector3D.WorldY));
                    zoneSurface.inclination = System.Convert.ToSingle(Geometry.Spatial.Query.Tilt(panel));
                    
                    TBD.RoomSurface roomSurface = room.AddSurface();

                    Face3D face3D = panel.Face3D;
                    if(face3D == null)
                    {
                        continue;
                    }

                    TBD.Perimeter perimeter = Geometry.Tas.Convert.ToTBD(face3D, roomSurface);
                    if(perimeter == null)
                    {
                        continue;
                    }

                    string guid_Panel = panel.Guid.ToString("B");

                    TBD.Construction construction_TBD = null;

                    PanelType panelType = panel.PanelType;

                    Construction construction = panel.Construction;
                    if(construction != null)
                    {
                        string guid_Construction = construction.Guid.ToString("B");
                        construction_TBD = constructions.Find(x => x.name == construction.Name);
                        if(construction_TBD == null)
                        {
                            construction_TBD = building.AddConstruction(null);
                            //construction_TBD.GUID = guid_Construction;
                            construction_TBD.name = construction.Name;

                            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                            if(constructionLayers != null && constructionLayers.Count != 0)
                            {
                                foreach(ConstructionLayer constructionLayer in constructionLayers)
                                {
                                    Core.Material material = analyticalModel?.MaterialLibrary?.GetMaterial(constructionLayer.Name) as Core.Material;
                                    if(material == null)
                                    {
                                        continue;
                                    }

                                    TBD.material material_TBD = construction_TBD.AddMaterial(material);
                                    material_TBD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                                }
                            }

                            constructions.Add(construction_TBD);
                        }

                        if (panelType == PanelType.Undefined && construction != null)
                        {
                            panelType = construction.PanelType();
                            if (panelType == PanelType.Undefined && construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string panelTypeString))
                            {
                                panelType = Core.Query.Enum<PanelType>(panelTypeString);
                            }
                        }
                    }

                    zoneSurface.type = Query.SurfaceType(panelType);

                    TBD.buildingElement buildingElement = buildingElements.Find(x => x.name == guid_Panel);
                    if(buildingElement == null)
                    {
                        buildingElement = building.AddBuildingElement();
                        //buildingElement.GUID = guid_Panel;
                        buildingElement.name = guid_Panel;
                        buildingElement.BEType = Query.BEType(panelType.Text());
                        buildingElement.AssignConstruction(construction_TBD);
                        buildingElements.Add(buildingElement);
                    }

                    if(buildingElement != null)
                    {
                        zoneSurface.buildingElement = buildingElement;
                    }
                }
            }

            return result;
        }
    }
}
