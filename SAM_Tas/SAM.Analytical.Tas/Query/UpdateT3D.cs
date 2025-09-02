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

            Modify.RemoveUnused(building);
            
            double northAngle = double.NaN;
            if (analyticalModel.TryGetValue(SAM.Analytical.AnalyticalModelParameter.NorthAngle, out northAngle))
            {
                building.northAngle = global::System.Math.Round(Units.Convert.ToDegrees(northAngle), 1);
                if(building.northAngle < 0.5)
                {
                    building.northAngle = 0.5;
                }
            }


            Location location = analyticalModel.Location;
            if(location != null)
            {
                building.longitude = location.Longitude;
                building.latitude = location.Latitude;

                if(location.TryGetValue(LocationParameter.TimeZone, out string timeZone))
                {
                    double @double = Core.Query.Double(Core.Query.UTC(timeZone));
                    if(!double.IsNaN(@double))
                    {
                        building.timeZone = global::System.Convert.ToSingle(@double);
                    }
                }
            }

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster != null)
            {
                //Zones -> Spaces
                Dictionary<string, ISpace> spaces = adjacencyCluster.SpaceDictionary<ISpace>();
                if (spaces != null)
                {
                    Dictionary<string, TAS3D.Zone> zones = building.ZoneDictionary();
                    if (zones != null)
                    {
                        foreach (KeyValuePair<string, TAS3D.Zone> keyValuePair in zones)
                        {
                            if (!spaces.TryGetValue(keyValuePair.Key, out ISpace space) || space == null)
                            {
                                continue;
                            }

                            //TODO: Update Zone
                            ISpace space_New = space.Clone();
                            if(space_New is SAMObject)
                            {
                                ((SAMObject)space_New).Add(Create.ParameterSet(ActiveSetting.Setting, keyValuePair.Value));
                            }

                            if(space_New is ExternalSpace)
                            {
                                keyValuePair.Value.external = true;
                            }

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
                            {
                                element.ghost = true;
                                continue;
                            }

                            //Update Element

                            //Thickness
                            double thickness = construction.GetValue<double>(Analytical.ConstructionParameter.DefaultThickness);
                            if (double.IsNaN(thickness) || thickness == 0)
                            {
                                thickness = construction.GetThickness(false);
                            }

                            if(!double.IsNaN(thickness))
                            {
                                element.width = thickness;
                            }

                            //if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Thickness(), out thickness, true))
                            //    element.width= thickness;

                            //Colour
                            System.Drawing.Color color = global::System.Drawing.Color.Empty;
                            if (construction.TryGetValue(Analytical.ConstructionParameter.Color, out color))
                                element.colour = Core.Convert.ToUint(color);

                            //Transparent
                            bool transparent = false;
                            MaterialType materialType = Analytical.Query.MaterialType(construction.ConstructionLayers, analyticalModel.MaterialLibrary);
                            if (materialType == MaterialType.Undefined)
                            {
                                materialType = MaterialType.Opaque;
                                if(construction.TryGetValue(Analytical.ConstructionParameter.Transparent, out transparent))
                                    element.transparent = transparent;
                            }
                            else
                            {
                                element.transparent = materialType == MaterialType.Transparent;
                            }

                            //InternalShadows
                            bool internalShadows = false;
                            if(construction.TryGetValue(Analytical.ConstructionParameter.IsInternalShadow, out internalShadows))
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
                                
                                if(!construction.TryGetValue(Analytical.ConstructionParameter.DefaultPanelType, out string_BEType))
                                    string_BEType = null;
                            }

                            if(!string.IsNullOrEmpty(string_BEType))
                            {
                                int bEType = BEType(string_BEType);
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
                            if (construction.TryGetValue(Analytical.ConstructionParameter.IsGround, out ground))
                                element.ground = ground;

                            //Air
                            bool air = false;
                            if(construction.TryGetValue(Analytical.ConstructionParameter.IsAir, out air))
                                element.ghost = air;

                            List<Panel> panels = adjacencyCluster.GetPanels(construction);
                            if(panels != null && panels.Count > 0)
                            {
                                ParameterSet parameterSet = Create.ParameterSet(ActiveSetting.Setting, element);
                                construction.Add(parameterSet);

                                foreach(Panel panel in panels)
                                {
                                    Panel panel_New = Analytical.Create.Panel(panel, construction);
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

                            Aperture aperture = null;
                            if (UniqueNameDecomposition(window.name, out string prefix, out string name, out System.Guid? guid, out int id) && guid != null && guid.HasValue)
                            {
                                aperture = adjacencyCluster.GetAperture(guid.Value);
                            }

                            //Colour
                            if (aperture != null)
                            {
                                window.SetColor(aperture, Analytical.AperturePart.Pane);
                            }
                            else
                            {
                                System.Drawing.Color color = global::System.Drawing.Color.Empty;
                                if (!apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.Color, out color))
                                    color = Analytical.Query.Color(apertureConstruction.ApertureType);

                                if (color != global::System.Drawing.Color.Empty)
                                    window.colour = Core.Convert.ToUint(color);
                            }

                            //Transparent
                            List<ConstructionLayer> constructionLayers = apertureConstruction.PaneConstructionLayers;

                            bool transparent = false;
                            window.transparent = transparent; //Requested by Michal 2021.03.01
                            window.internalShadows = false;

                            MaterialType materialType = Analytical.Query.MaterialType(constructionLayers, analyticalModel.MaterialLibrary);
                            if (materialType == MaterialType.Undefined)
                            {
                                materialType = MaterialType.Opaque;
                                if (apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.Transparent, out transparent))
                                    window.transparent = transparent;
                            }
                            else
                            {
                                window.transparent = materialType == MaterialType.Transparent;
                            }


                            if(window.transparent)
                            {
                                //InternalShadows
                                window.internalShadows = false; //Requested by Michal 2021.03.01
                                bool internalShadows = false;
                                if (apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.IsInternalShadow, out internalShadows))
                                {
                                    window.internalShadows = internalShadows;
                                }
                                else
                                {
                                    List<Panel> panels = adjacencyCluster.GetPanels(apertureConstruction);
                                    if(panels != null && panels.Count != 0)
                                    {
                                        window.internalShadows = panels.TrueForAll(x => adjacencyCluster.External(x));
                                    }
                                }
                                    
                            }

                            //FrameWidth
                            double frameWidth = double.NaN;
                            
                            if(apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.DefaultFrameWidth, out frameWidth))
                            {
                                window.frameWidth = frameWidth;
                            }

                            frameWidth = apertureConstruction.GetFrameThickness();
                            if(!double.IsNaN(frameWidth))
                            {
                                window.frameWidth = frameWidth;
                            }

                            if (aperture != null)
                            {
                                double frameFactor = aperture.GetFrameFactor();
                                window.isPercFrame = true;
                                window.framePerc = frameFactor * 100;
                            }

                        }
                    }
                }
            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel, adjacencyCluster);

            return result;
        }
    }
}
