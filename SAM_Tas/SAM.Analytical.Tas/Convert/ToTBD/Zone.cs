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

            Shell shell = adjacencyCluster.Shell(space);
            if(shell == null)
            {
                return null;
            }

            TBD.zone result = building.AddZone();
            result.name = space.Name;
            result.volume = System.Convert.ToSingle(shell.Volume());
            result.floorArea = System.Convert.ToSingle(shell.Area(0.1));

            TBD.room room = result.AddRoom();

            List<TBD.buildingElement> buildingElements = building.BuildingElements();
            List<TBD.Construction> constructions = building.Constructions();

            List<Panel> panels = adjacencyCluster?.GetPanels(space);
            if(panels != null || panels.Count != 0)
            {
                foreach(Panel panel in panels)
                {
                    string name = panel.Name;
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    Face3D face3D = panel.Face3D;
                    if (face3D == null)
                    {
                        continue;
                    }

                    TBD.zoneSurface zoneSurface = result.AddSurface();
                    zoneSurface.orientation = System.Convert.ToSingle(Geometry.Spatial.Query.Azimuth(panel, Vector3D.WorldY));
                    zoneSurface.inclination = System.Convert.ToSingle(Geometry.Spatial.Query.Tilt(panel));
                    zoneSurface.area = System.Convert.ToSingle(face3D.GetArea());

                    TBD.RoomSurface roomSurface = room.AddSurface();
                    roomSurface.area = System.Convert.ToSingle(face3D.GetArea());
                    roomSurface.zoneSurface = zoneSurface;

                    TBD.Perimeter perimeter = Geometry.Tas.Convert.ToTBD(face3D, roomSurface);
                    if(perimeter == null)
                    {
                        continue;
                    }

                    PanelType panelType = panel.PanelType;

                    TBD.buildingElement buildingElement = buildingElements.Find(x => x.name == name);
                    if (buildingElement == null)
                    {
                        TBD.Construction construction_TBD = null;

                        Construction construction = panel.Construction;
                        if (construction != null)
                        {
                            construction_TBD = constructions.Find(x => x.name == construction.Name);
                            if (construction_TBD == null)
                            {
                                construction_TBD = building.AddConstruction(null);
                                construction_TBD.name = construction.Name;

                                List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                                if (constructionLayers != null && constructionLayers.Count != 0)
                                {
                                    int index = 1;
                                    foreach (ConstructionLayer constructionLayer in constructionLayers)
                                    {
                                        Core.Material material = analyticalModel?.MaterialLibrary?.GetMaterial(constructionLayer.Name) as Core.Material;
                                        if (material == null)
                                        {
                                            continue;
                                        }

                                        TBD.material material_TBD = construction_TBD.AddMaterial(material);
                                        if (material_TBD != null)
                                        {
                                            material_TBD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                                            construction_TBD.materialWidth[index] = System.Convert.ToSingle(constructionLayer.Thickness);
                                            index++;
                                        }
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

                        buildingElement = building.AddBuildingElement();
                        buildingElement.name = name;
                        buildingElement.colour = Core.Convert.ToUint(Analytical.Query.Color(panelType));
                        buildingElement.BEType = Query.BEType(panelType.Text());
                        buildingElement.AssignConstruction(construction_TBD);
                        buildingElements.Add(buildingElement);
                    }

                    if (buildingElement != null)
                    {
                        zoneSurface.buildingElement = buildingElement;
                    }

                    zoneSurface.type = Query.SurfaceType(panelType);
                }
            }

            return result;
        }
    }
}
