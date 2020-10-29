using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static AnalyticalModel UpdateT3D(this AnalyticalModel analyticalModel, string path_T3D)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_T3D))
                return null;

            AnalyticalModel result = null;

            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = UpdateT3D(analyticalModel, sAMT3DDocument.T3DDocument);
                if (result != null)
                    sAMT3DDocument.Save();
            }

            return result;

        }

        public static AnalyticalModel UpdateT3D(this AnalyticalModel analyticalModel, T3DDocument t3DDocument)
        {
            if (analyticalModel == null)
                return null;


            Building building = t3DDocument?.Building;
            if (building == null)
                return null;

            Modify.RemoveUnsusedZones(building);
            
            double northAngle = double.NaN;
            if (analyticalModel.TryGetValue(AnalyticalModelParameter.NorthAngle, out northAngle))
                building.northAngle = northAngle;

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster != null)
            {
                //Zones -> Spaces
                List<Space> spaces = adjacencyCluster.GetSpaces();
                if (spaces != null)
                {
                    List<Zone> zones = building.Zones();
                    if (zones != null)
                    {
                        foreach (Zone zone in zones)
                        {
                            Space space = zone.Match(spaces);
                            if (space == null)
                                continue;

                            //TODO: Update Zone
                            Space space_New = space.Clone();
                            space_New.Add(Create.ParameterSet(ActiveSetting.Setting, zone));
                            adjacencyCluster.AddObject(space_New);
                        }
                    }
                }

                //Elements -> Constructions
                List<Construction> constructions = adjacencyCluster.GetConstructions();
                if(constructions != null)
                {
                    List<Element> elements = building.Elements();
                    if(elements != null)
                    {
                        foreach (Element element in elements)
                        {
                            Construction construction = element.Match(constructions);
                            if (construction == null)
                                continue;

                            //Update Element

                            //Thickness
                            double thickness = construction.GetThickness();
                            if(!double.IsNaN(thickness))
                                element.width = thickness;

                            //if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Thickness(), out thickness, true))
                            //    element.width= thickness;

                            //Colour
                            System.Drawing.Color color = System.Drawing.Color.Empty;
                            if (construction.TryGetValue(ConstructionParameter.Color, out color))
                                element.colour = Core.Convert.ToUint(color);
                                

                            //Transparent
                            bool transparent = false;
                            MaterialType materialType = Analytical.Query.MaterialType(construction.ConstructionLayers, analyticalModel.MaterialLibrary);
                            if (materialType == MaterialType.Undefined)
                            {
                                materialType = MaterialType.Opaque;
                                if(construction.TryGetValue(ConstructionParameter.Transparent, out transparent))
                                    element.transparent = transparent;
                            }
                            else
                            {
                                element.transparent = materialType == MaterialType.Transparent;
                            }

                            //InternalShadows
                            bool internalShadows = false;
                            if(construction.TryGetValue(ConstructionParameter.IsInternalShadow, out internalShadows))
                                element.internalShadows = internalShadows;
                            else
                                element.internalShadows = element.transparent;


                            //BEType
                            string string_BEType = null;

                            PanelType panelType = construction.PanelType();
                            if(panelType != Analytical.PanelType.Undefined)
                            {
                                string_BEType = panelType.Text();
                            }
                            else
                            {
                                
                                if(!construction.TryGetValue(ConstructionParameter.DefaultPanelType, out string_BEType))
                                    string_BEType = null;         
                            }

                            if(!string.IsNullOrEmpty(string_BEType))
                            {
                                int bEType = Query.BEType(string_BEType);
                                if (bEType != -1)
                                {
                                    element.BEType = bEType;
                                    panelType = PanelType(bEType);
                                }
                            }
                            else
                            {
                                panelType = Analytical.PanelType.Undefined;

                                List<Panel> panels_Construction =  adjacencyCluster.GetPanels(construction);
                                if(panels_Construction != null && panels_Construction.Count > 0)
                                {
                                    Panel panel = panels_Construction.Find(x => x.PanelType != Analytical.PanelType.Undefined);
                                    if (panel != null)
                                        panelType = panel.PanelType;
                                }    
                            }

                            if (panelType == Analytical.PanelType.Undefined)
                            {
                                List<Panel> panels_Construction = adjacencyCluster.GetPanels(construction);
                                if (panels_Construction != null && panels_Construction.Count != 0)
                                    element.zoneFloorArea = panels_Construction.Find(x => x.PanelType.PanelGroup() == PanelGroup.Floor) != null;
                            }

                            if (panelType.PanelGroup() == PanelGroup.Floor)
                                element.zoneFloorArea = true;

                            //Ground
                            bool ground = false;
                            if (construction.TryGetValue(ConstructionParameter.IsGround, out ground))
                                element.ground = ground;

                            //Air
                            bool air = false;
                            if(construction.TryGetValue(ConstructionParameter.IsAir, out air))
                                element.ghost = air;

                            List<Panel> panels = adjacencyCluster.GetPanels(construction);
                            if(panels != null && panels.Count > 0)
                            {
                                ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, element);
                                construction.Add(parameterSet);

                                foreach(Panel panel in panels)
                                {
                                    Panel panel_New = new Panel(panel, construction);
                                    adjacencyCluster.AddObject(panel_New);
                                }
                            }                            
                        }
                    }
                }

                //Windows -> ApertureConstruction
                List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
                if(apertureConstructions != null)
                {
                    List<window> windows = building.Windows();
                    if (windows != null)
                    {
                        foreach(window window in windows)
                        {
                            if (window == null)
                                continue;

                            ApertureConstruction apertureConstruction = window.Match(apertureConstructions);
                            if (apertureConstruction == null)
                                continue;

                            //Colour
                            System.Drawing.Color color = System.Drawing.Color.Empty;
                            if (apertureConstruction.TryGetValue(ApertureConstructionParameter.Color, out color))
                                window.colour = Core.Convert.ToUint(color);

                            //Transparent
                            List<ConstructionLayer> constructionLayers = null;
                            if (true)
                                constructionLayers = apertureConstruction.PaneConstructionLayers;
                            else
                                constructionLayers = apertureConstruction.FrameConstructionLayers;

                            bool transparent = false;
                            MaterialType materialType = Analytical.Query.MaterialType(constructionLayers, analyticalModel.MaterialLibrary);
                            if (materialType == MaterialType.Undefined)
                            {
                                materialType = MaterialType.Opaque;
                                if (apertureConstruction.TryGetValue(ApertureConstructionParameter.Transparent, out transparent))
                                    window.transparent = transparent;
                            }
                            else
                            {
                                window.transparent = materialType == MaterialType.Transparent;
                            }

                            //InternalShadows
                            bool internalShadows = false;
                            if(apertureConstruction.TryGetValue(ApertureConstructionParameter.IsInternalShadow, out internalShadows))
                                window.internalShadows = internalShadows;

                            //FrameWidth
                            double frameWidth = double.NaN;
                            if(apertureConstruction.TryGetValue(ApertureConstructionParameter.DefaultFrameWidth, out frameWidth))
                                window.frameWidth = frameWidth;

                        }
                    }
                }


            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel, adjacencyCluster);

            return result;
        }
    }
}
