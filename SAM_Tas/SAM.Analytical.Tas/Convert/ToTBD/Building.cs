using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {

        public static TBD.Building ToTBD(this AnalyticalModel analyticalModel, TBD.TBDDocument tBDDocument)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            TBD.Building result = tBDDocument.Building;
            if(result == null)
            {
                return null;
            }

            result.northAngle = 180;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            adjacencyCluster.UpdateNormals(true);

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces == null)
            {
                return result;
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

            Plane plane = Plane.WorldXY;

            List<TBD.DaysShade> daysShades = new List<TBD.DaysShade>();
            result.ClearShadingData();

            Dictionary<System.Guid, List<TBD.zoneSurface>> dictionary_Panel = new Dictionary<System.Guid, List<TBD.zoneSurface>>();
            Dictionary<System.Guid, List<TBD.zoneSurface>> dictionary_Aperture = new Dictionary<System.Guid, List<TBD.zoneSurface>>();
            foreach (Space space in spaces)
            {
                Shell shell = adjacencyCluster.Shell(space);
                BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
                if (shell == null || boundingBox3D == null)
                {
                    return null;
                }

                TBD.zone zone = result.AddZone();
                zone.name = space.Name;
                zone.volume = System.Convert.ToSingle(shell.Volume());

                if (space.TryGetValue(Analytical.SpaceParameter.Color, out SAMColor sAMColor) && sAMColor != null)
                {
                    zone.colour = Core.Convert.ToUint(sAMColor.ToColor());
                }

                List<Face3D> face3Ds = Geometry.Spatial.Query.Section(shell, (boundingBox3D.Max.Z - boundingBox3D.Min.Z) / 2, false);
                if(face3Ds != null && face3Ds.Count != 0)
                {
                    face3Ds.RemoveAll(x => x == null || !x.IsValid());
                    zone.floorArea = System.Convert.ToSingle(face3Ds.ConvertAll(x => x.GetArea()).Sum());
                    zone.exposedPerimeter = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Planar.Query.Perimeter(x.ExternalEdge2D)).Sum());
                    zone.length = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Tas.Query.Length(x)).Sum());
                }

                TBD.room room = zone.AddRoom();

                List<TBD.buildingElement> buildingElements = result.BuildingElements();
                List<TBD.Construction> constructions = result.Constructions();

                List<Panel> panels = adjacencyCluster?.GetPanels(space);
                if (panels != null || panels.Count != 0)
                {
                    foreach (Panel panel in panels)
                    {
                        string name_Panel = panel.Name;
                        if (string.IsNullOrWhiteSpace(name_Panel) || panel.PanelType == PanelType.Air)
                        {
                            name_Panel = "Air";
                        }

                        Face3D face3D_Panel = panel.Face3D;
                        if (face3D_Panel == null)
                        {
                            continue;
                        }

                        BoundingBox3D boundingBox3D_Panel = face3D_Panel.GetBoundingBox();

                        Vector3D normal = dictionary_Panel.ContainsKey(panel.Guid) ? panel.Normal.GetNegated() : panel.Normal;

                        TBD.zoneSurface zoneSurface_Panel = zone.AddSurface();
                        zoneSurface_Panel.orientation = System.Convert.ToSingle(Geometry.Spatial.Query.Azimuth(panel, Vector3D.WorldY));
                        zoneSurface_Panel.inclination = System.Convert.ToSingle(Geometry.Spatial.Query.Tilt(normal));
                        
                        zoneSurface_Panel.altitude = System.Convert.ToSingle(boundingBox3D_Panel.GetCentroid().Z);
                        zoneSurface_Panel.altitudeRange = System.Convert.ToSingle(boundingBox3D_Panel.Max.Z - boundingBox3D_Panel.Min.Z);
                        zoneSurface_Panel.area = System.Convert.ToSingle(face3D_Panel.GetArea());
                        zoneSurface_Panel.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D_Panel));

                        TBD.RoomSurface roomSurface_Panel = room.AddSurface();
                        roomSurface_Panel.area = zoneSurface_Panel.area;
                        roomSurface_Panel.zoneSurface = zoneSurface_Panel;

                        Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult = analyticalModel.GetResults<Geometry.SolarCalculator.SolarFaceSimulationResult>(panel)?.FirstOrDefault();
                        if(solarFaceSimulationResult != null)
                        {
                            List<TBD.SurfaceShade> surfaceShades = Modify.UpdateSurfaceShades(result, daysShades, zoneSurface_Panel, analyticalModel, solarFaceSimulationResult);
                        }

                        TBD.Perimeter perimeter_Panel = Geometry.Tas.Convert.ToTBD(panel.GetFace3D(true), roomSurface_Panel);
                        if (perimeter_Panel == null)
                        {
                            continue;
                        }

                        PanelType panelType = panel.PanelType;

                        TBD.buildingElement buildingElement_Panel = buildingElements.Find(x => x.name == name_Panel);
                        if (buildingElement_Panel == null)
                        {
                            TBD.Construction construction_TBD = null;

                            if(panel.PanelType != PanelType.Air)
                            {
                                Construction construction = panel.Construction;
                                if (construction != null)
                                {
                                    construction_TBD = constructions.Find(x => x.name == construction.Name);
                                    if (construction_TBD == null)
                                    {
                                        construction_TBD = result.AddConstruction(null);
                                        construction_TBD.name = construction.Name;

                                        if (construction.Transparent(materialLibrary))
                                        {
                                            construction_TBD.type = TBD.ConstructionTypes.tcdTransparentConstruction;
                                        }

                                        List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                                        if (constructionLayers != null && constructionLayers.Count != 0)
                                        {
                                            int index = 1;
                                            foreach (ConstructionLayer constructionLayer in constructionLayers)
                                            {
                                                Material material = analyticalModel?.MaterialLibrary?.GetMaterial(constructionLayer.Name) as Material;
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
                            }


                            buildingElement_Panel = result.AddBuildingElement();
                            buildingElement_Panel.name = name_Panel;
                            buildingElement_Panel.colour = Core.Convert.ToUint(Analytical.Query.Color(panelType));
                            buildingElement_Panel.BEType = Query.BEType(panelType.Text());
                            buildingElement_Panel.AssignConstruction(construction_TBD);
                            buildingElement_Panel.ghost = panelType == PanelType.Air ? 1 : 0;
                            buildingElements.Add(buildingElement_Panel);
                        }

                        if (buildingElement_Panel != null)
                        {
                            zoneSurface_Panel.buildingElement = buildingElement_Panel;
                        }

                        zoneSurface_Panel.type = TBD.SurfaceType.tbdExposed;

                        List<Aperture> apertures = panel.Apertures;
                        if (apertures != null && apertures.Count != 0)
                        {
                            bool @internal = adjacencyCluster.Internal(panel);

                            Func<Face3D, TBD.zoneSurface> func = delegate (Face3D face3D)
                            {
                                BoundingBox3D boundingBox3D_Aperture = face3D.GetBoundingBox();

                                float area = System.Convert.ToSingle(face3D.GetArea());

                                TBD.zoneSurface zoneSurface_Aperture = zoneSurface_Panel.AddChildSurface(area);
                                if (zoneSurface_Aperture == null)
                                {
                                    return null;
                                }

                                zoneSurface_Aperture.orientation = zoneSurface_Panel.orientation;
                                zoneSurface_Aperture.inclination = zoneSurface_Panel.inclination;
                                zoneSurface_Aperture.altitude = System.Convert.ToSingle(boundingBox3D_Aperture.GetCentroid().Z);
                                zoneSurface_Aperture.altitudeRange = System.Convert.ToSingle(boundingBox3D_Aperture.Max.Z - boundingBox3D_Aperture.Min.Z);
                                zoneSurface_Aperture.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D));

                                zoneSurface_Aperture.type = @internal ? TBD.SurfaceType.tbdLink : zoneSurface_Panel.type;

                                TBD.RoomSurface roomSurface_Aperture = room.AddSurface();
                                roomSurface_Aperture.area = zoneSurface_Aperture.area;
                                roomSurface_Aperture.zoneSurface = zoneSurface_Aperture;

                                TBD.Perimeter perimeter_Aperture = Geometry.Tas.Convert.ToTBD(face3D, roomSurface_Aperture);
                                if (perimeter_Aperture == null)
                                {
                                    return null;
                                }

                                return zoneSurface_Aperture;
                            };

                            foreach (Aperture aperture in apertures)
                            {
                                string name_Aperture = aperture.Name;
                                if (string.IsNullOrWhiteSpace(name_Aperture))
                                {
                                    continue;
                                }

                                Dictionary<string, List<TBD.zoneSurface>> dictionary = new Dictionary<string, List<TBD.zoneSurface>>();

                                List<Face3D> face3Ds_Pane = aperture.GetFace3Ds(AperturePart.Pane);
                                if(face3Ds_Pane != null)
                                {
                                    string apertureName_Pane = string.Format("{0} -pane", aperture.Name);
                                    dictionary[apertureName_Pane] = new List<TBD.zoneSurface>();
                                    foreach (Face3D face3D in face3Ds_Pane)
                                    {
                                        TBD.zoneSurface zoneSurface = func.Invoke(face3D);
                                        if(zoneSurface != null)
                                        {
                                            dictionary[apertureName_Pane].Add(zoneSurface);
                                        }
                                    }
                                }

                                List<Face3D> face3Ds_Frame = aperture.GetFace3Ds(AperturePart.Frame);
                                if (face3Ds_Frame != null)
                                {
                                    string apertureName_Frame = string.Format("{0} -frame", aperture.Name);
                                    dictionary[apertureName_Frame] = new List<TBD.zoneSurface>();
                                    foreach (Face3D face3D in face3Ds_Frame)
                                    {
                                        TBD.zoneSurface zoneSurface = func.Invoke(face3D);
                                        if (zoneSurface != null)
                                        {
                                            dictionary[apertureName_Frame].Add(zoneSurface);
                                        }
                                    }
                                }

                                foreach(KeyValuePair<string, List<TBD.zoneSurface>> keyValuePair in dictionary)
                                {
                                    TBD.buildingElement buildingElement_Aperture = buildingElements.Find(x => x.name == keyValuePair.Key);
                                    if (buildingElement_Aperture == null)
                                    {
                                        TBD.Construction construction_TBD = null;

                                        ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                                        if (apertureConstruction != null)
                                        {
                                            construction_TBD = constructions.Find(x => x.name == apertureConstruction.Name);
                                            if (construction_TBD == null)
                                            {
                                                construction_TBD = result.AddConstruction(null);
                                                construction_TBD.name = keyValuePair.Key;

                                                if (apertureConstruction.Transparent(materialLibrary))
                                                {
                                                    construction_TBD.type = TBD.ConstructionTypes.tcdTransparentConstruction;
                                                }

                                                List<ConstructionLayer> constructionLayers = apertureConstruction.PaneConstructionLayers;
                                                if (constructionLayers != null && constructionLayers.Count != 0)
                                                {
                                                    int index = 1;
                                                    foreach (ConstructionLayer constructionLayer in constructionLayers)
                                                    {
                                                        Material material = materialLibrary?.GetMaterial(constructionLayer.Name) as Material;
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
                                        }

                                        ApertureType apertureType = aperture.ApertureType;

                                        buildingElement_Aperture = result.AddBuildingElement();
                                        buildingElement_Aperture.name = keyValuePair.Key;
                                        buildingElement_Aperture.colour = Core.Convert.ToUint(Analytical.Query.Color(apertureType));
                                        buildingElement_Aperture.BEType = Query.BEType(apertureType, false);
                                        buildingElement_Aperture.AssignConstruction(construction_TBD);
                                        buildingElements.Add(buildingElement_Aperture);
                                    }

                                    foreach(TBD.zoneSurface zoneSurface in keyValuePair.Value)
                                    {
                                        if (buildingElement_Aperture != null)
                                        {
                                            zoneSurface.buildingElement = buildingElement_Aperture;
                                        }

                                        if (!dictionary_Aperture.TryGetValue(aperture.Guid, out List<TBD.zoneSurface> zoneSurfaces_Aperture) || zoneSurfaces_Aperture == null)
                                        {
                                            zoneSurfaces_Aperture = new List<TBD.zoneSurface>();
                                            dictionary_Aperture[aperture.Guid] = zoneSurfaces_Aperture;
                                        }

                                        zoneSurfaces_Aperture.Add(zoneSurface);
                                    }
                                }
                            }
                        }

                        zoneSurface_Panel.type = Analytical.Query.Adiabatic(panel) ? TBD.SurfaceType.tbdNullLink : Query.SurfaceType(panelType);

                        if (!dictionary_Panel.TryGetValue(panel.Guid, out List<TBD.zoneSurface> zoneSurfaces_Panel) || zoneSurfaces_Panel == null)
                        {
                            zoneSurfaces_Panel = new List<TBD.zoneSurface>();
                            dictionary_Panel[panel.Guid] = zoneSurfaces_Panel;
                        }

                        zoneSurfaces_Panel.Add(zoneSurface_Panel);
                    }
                }
            }

            foreach (KeyValuePair<System.Guid, List<TBD.zoneSurface>> keyValuePair in dictionary_Panel)
            {
                if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
                {
                    continue;
                }

                keyValuePair.Value[1].linkSurface = keyValuePair.Value[0];
                keyValuePair.Value[0].linkSurface = keyValuePair.Value[1];

                if (keyValuePair.Value[0].inclination == 0 || keyValuePair.Value[0].inclination == 180)
                {
                    //float inclination = keyValuePair.Value[1].inclination;
                    //inclination -= 180;
                    //if (inclination < 0)
                    //{
                    //    inclination += 360;
                    //}

                    //keyValuePair.Value[1].inclination = inclination;
                    keyValuePair.Value[1].reversed = 1;
                }
                else
                {
                    float orientation = keyValuePair.Value[1].orientation;
                    orientation += 180;
                    if (orientation >= 360)
                    {
                        orientation -= 360;
                    }

                    keyValuePair.Value[1].orientation = orientation;
                    keyValuePair.Value[1].reversed = 1;

                    float inclination = keyValuePair.Value[1].inclination;
                    if(inclination > 180)
                    {
                        inclination -= 180;
                    }
                    keyValuePair.Value[1].inclination = inclination;
                }
            }

            foreach(KeyValuePair<System.Guid, List<TBD.zoneSurface>> keyValuePair in dictionary_Aperture)
            {
                if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
                {
                    continue;
                }

                keyValuePair.Value[1].linkSurface = keyValuePair.Value[0];
                keyValuePair.Value[0].linkSurface = keyValuePair.Value[1];

                if (keyValuePair.Value[0].inclination == 0 || keyValuePair.Value[0].inclination == 180)
                {
                    float inclination = keyValuePair.Value[0].inclination;
                    inclination -= 180;
                    if (inclination < 0)
                    {
                        inclination += 360;
                    }

                    keyValuePair.Value[0].inclination = inclination;
                    keyValuePair.Value[0].reversed = 1;
                }
                else
                {
                    float orientation = keyValuePair.Value[1].orientation;
                    orientation += 180;
                    if (orientation >= 360)
                    {
                        orientation -= 360;
                    }

                    keyValuePair.Value[1].orientation = orientation;
                    keyValuePair.Value[1].reversed = 1;


                }
            }

            return result;
        }
    }
}
