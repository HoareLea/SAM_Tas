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
            if (Core.Query.TryGetValue(analyticalModel, Analytical.Query.ParameterName_NorthAngle(), out northAngle))
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
                            double thickness = double.NaN;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Thickness(), out thickness, true))
                                element.width= thickness;

                            //Colour
                            uint color = uint.MinValue;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Color(), out color, true))
                                element.colour = color;

                            //Transparent
                            bool transparent = false;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Transparent(), out transparent, true))
                                element.transparent = transparent;

                            //InternalShadows
                            bool internalShadows = false;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_InternalShadows(), out internalShadows, true))
                                element.internalShadows = internalShadows;

                            //BEType
                            string string_BEType = null;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_PanelType(), out string_BEType, true))
                            {
                                int bEType = Query.BEType(string_BEType);
                                if(bEType != -1)
                                    element.BEType = bEType;
                            }

                            //Ground
                            bool ground = false;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Ground(), out ground, true))
                                element.ground = ground;

                            //Air
                            bool air = false;
                            if (Core.Query.TryGetValue(construction, Analytical.Query.ParameterName_Air(), out air, true))
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
                            uint color = uint.MinValue;
                            if (Core.Query.TryGetValue(apertureConstruction, Analytical.Query.ParameterName_Color(), out color, true))
                                window.colour = color;

                            //Transparent
                            bool transparent = false;
                            if (Core.Query.TryGetValue(apertureConstruction, Analytical.Query.ParameterName_Transparent(), out transparent, true))
                                window.transparent = transparent;

                            //InternalShadows
                            bool internalShadows = false;
                            if (Core.Query.TryGetValue(apertureConstruction, Analytical.Query.ParameterName_InternalShadows(), out internalShadows, true))
                                window.internalShadows = internalShadows;

                            //FrameWidth
                            double frameWidth = double.NaN;
                            if (Core.Query.TryGetValue(apertureConstruction, Analytical.Query.ParameterName_FrameWidth(), out frameWidth, true))
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
