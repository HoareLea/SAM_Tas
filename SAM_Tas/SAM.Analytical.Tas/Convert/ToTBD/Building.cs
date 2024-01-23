using SAM.Core;
//using SAM.Geometry.Spatial;
//using System;
//using System.Collections.Generic;
//using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {

        public static TBD.Building ToTBD(this AnalyticalModel analyticalModel, TBD.TBDDocument tBDDocument, bool updateGuids = false, string undefinedZoneGroupName = "Undefined", string allZoneGroupName = "All")
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

            result.northAngle = 0; //180 changed 03.11.2022

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            double buildingHeight = Analytical.Query.BuildingHeight(adjacencyCluster);
            if(!double.IsNaN(buildingHeight) && buildingHeight > 0)
            {
                result.maxBuildingAltitude = (float)buildingHeight;
            }

            MaterialLibrary materialLibrary = analyticalModel.MaterialLibrary;

            //List<TBD.DaysShade> daysShades = new List<TBD.DaysShade>();
            result.ClearShadingData();

            //adjacencyCluster = adjacencyCluster.UpdateNormals(true, false, false);
            //adjacencyCluster.Normalize(true, Geometry.Orientation.Clockwise);

            Modify.Update(result, adjacencyCluster, materialLibrary, updateGuids);

            //List<Space> spaces = adjacencyCluster.GetSpaces();
            //if(spaces == null)
            //{
            //    return result;
            //}
            
            //Plane plane = Plane.WorldXY;

            //Dictionary<Guid, List<Tuple<TBD.zoneSurface, bool>>> dictionary_Panel = new Dictionary<Guid, List<Tuple<TBD.zoneSurface, bool>>>();
            //Dictionary<Guid, List<Tuple<AperturePart, TBD.zoneSurface, bool>>> dictionary_Aperture = new Dictionary<Guid, List<Tuple<AperturePart, TBD.zoneSurface, bool>>>();
            //foreach (Space space in spaces)
            //{
            //    Shell shell = adjacencyCluster.Shell(space);
            //    BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
            //    if (shell == null || boundingBox3D == null)
            //    {
            //        return null;
            //    }

            //    TBD.zone zone = result.AddZone();
            //    if(updateGuids)
            //    {
            //        Space space_Temp = new Space(space);
            //        space_Temp.SetValue(SpaceParameter.ZoneGuid, zone.GUID);
            //        analyticalModel.AddSpace(space_Temp);
            //    }

            //    zone.name = space.Name;
            //    double volume = shell.Volume();
            //    if(double.IsNaN(volume))
            //    {
            //        volume = space.GetValue<double>(Analytical.SpaceParameter.Volume);
            //    }

            //    zone.volume = System.Convert.ToSingle(volume);

            //    if (space.TryGetValue(Analytical.SpaceParameter.Color, out SAMColor sAMColor) && sAMColor != null)
            //    {
            //        zone.colour = Core.Convert.ToUint(sAMColor.ToColor());
            //    }

            //    List<Face3D> face3Ds = Geometry.Spatial.Query.Section(shell, (boundingBox3D.Max.Z - boundingBox3D.Min.Z) / 2, false);
            //    if(face3Ds != null && face3Ds.Count != 0)
            //    {
            //        face3Ds.RemoveAll(x => x == null || !x.IsValid());
            //        zone.floorArea = System.Convert.ToSingle(face3Ds.ConvertAll(x => x.GetArea()).Sum());
            //        zone.exposedPerimeter = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Planar.Query.Perimeter(x.ExternalEdge2D)).Sum());
            //        zone.length = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Tas.Query.Length(x)).Sum());
            //    }

            //    TBD.room room = zone.AddRoom();

            //    List<TBD.buildingElement> buildingElements = result.BuildingElements();
            //    List<TBD.Construction> constructions = result.Constructions();

            //    int index_Space = adjacencyCluster.GetIndex(space);

            //    List<Panel> panels = adjacencyCluster?.GetPanels(space);
            //    if (panels != null || panels.Count != 0)
            //    {
            //        foreach (Panel panel in panels)
            //        {
            //            string name_Panel = panel.Name;
            //            if (string.IsNullOrWhiteSpace(name_Panel) || panel.PanelType == PanelType.Air)
            //            {
            //                name_Panel = "Air";
            //            }

            //            Face3D face3D_Panel = panel.Face3D;
            //            if (face3D_Panel == null)
            //            {
            //                continue;
            //            }

            //            BoundingBox3D boundingBox3D_Panel = face3D_Panel.GetBoundingBox();

            //            Vector3D normal = dictionary_Panel.ContainsKey(panel.Guid) ? panel.Normal.GetNegated() : panel.Normal;

            //            TBD.zoneSurface zoneSurface_Panel = zone.AddSurface();

            //            Core.Tas.ZoneSurfaceReference zoneSurfaceReference = new Core.Tas.ZoneSurfaceReference(zoneSurface_Panel.number, zone.GUID);

            //            Panel panel_Temp = Analytical.Create.Panel(panel);

            //            PanelParameter panelParameter = panel.HasValue(PanelParameter.ZoneSurfaceReference_1) ? PanelParameter.ZoneSurfaceReference_2 : PanelParameter.ZoneSurfaceReference_1;
            //            panel_Temp.SetValue(panelParameter, zoneSurfaceReference);

            //            float orientation = System.Convert.ToSingle(Geometry.Object.Spatial.Query.Azimuth(panel, Vector3D.WorldY));
            //            orientation += 180;
            //            if (orientation >= 360)
            //            {
            //                orientation -= 360;
            //            }
            //            zoneSurface_Panel.orientation = orientation;

            //            //test
            //            //zoneSurface_Panel.reversed = 1;

            //            float inclination = System.Convert.ToSingle(Geometry.Spatial.Query.Tilt(normal));
            //            if(inclination == 0 || inclination == 180)
            //            {
            //                inclination -= 180;
            //                if (inclination < 0)
            //                {
            //                    inclination += 360;
            //                }
            //            }
            //            else
            //            {
            //                inclination = Math.Min(inclination, 180 - inclination);
            //            }

            //            //it should be dependant on normal
            //            //PanelGroup panelGroup = panel.PanelGroup;
            //            //if (inclination != 0 && inclination != 180 && panelGroup == PanelGroup.Floor)
            //            //{
            //            //    inclination = 180 - inclination;
            //            //}

            //            zoneSurface_Panel.inclination = inclination;
                        
            //            zoneSurface_Panel.altitude = System.Convert.ToSingle(boundingBox3D_Panel.GetCentroid().Z);
            //            zoneSurface_Panel.altitudeRange = System.Convert.ToSingle(boundingBox3D_Panel.Max.Z - boundingBox3D_Panel.Min.Z);
            //            zoneSurface_Panel.area = System.Convert.ToSingle(face3D_Panel.GetArea());
            //            zoneSurface_Panel.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D_Panel));

            //            //List<Space> spaces_Adjacent = adjacencyCluster?.GetSpaces(panel);
            //            //if (spaces_Adjacent != null && spaces_Adjacent.Count > 1)
            //            //{
            //            //    bool reverse = adjacencyCluster.GetIndex(spaces[0]) == index_Space;
            //            //    if(reverse)
            //            //    {
            //            //        zoneSurface_Panel.reversed = 1;
            //            //    }
            //            //}
            //            //if (panel.PanelGroup == PanelGroup.Roof)
            //            //{
            //            //    zoneSurface_Panel.reversed = 1;
            //            //}

            //            TBD.RoomSurface roomSurface_Panel = room.AddSurface();
            //            roomSurface_Panel.area = zoneSurface_Panel.area;
            //            roomSurface_Panel.zoneSurface = zoneSurface_Panel;

            //            Face3D face3D = panel.GetFace3D(true);

            //            //Commented on 14.04.2023
            //            //List<Face3D> face3Ds_FixEdges = face3D.FixEdges();
            //            //if(face3Ds_FixEdges != null && face3Ds_FixEdges.Count != 0)
            //            //{
            //            //    face3D = face3Ds_FixEdges[0];
            //            //}

            //            if (dictionary_Panel.ContainsKey(panel.Guid))
            //            {
            //                //face3D.SimplifyByNTS_TopologyPreservingSimplifier();
            //                face3D.FlipNormal(false);
            //            }


            //            List<Space> spaces_Adjacent = adjacencyCluster.GetSpaces(panel);
            //            bool reverse = false;
            //            //if (panel.PanelGroup != PanelGroup.Roof && dictionary_Panel.ContainsKey(panel.Guid))
            //            if(panel.PanelGroup == PanelGroup.Floor && spaces_Adjacent != null && spaces_Adjacent.Count > 1)
            //            {
            //                Vector3D vector3D_1 = Vector3D.WorldZ.GetNegated();
            //                Vector3D vector3D_2 = face3D.GetPlane().Normal;
            //                if(!vector3D_1.SameHalf(vector3D_2))
            //                {

            //                    face3D.FlipNormal(false);
            //                    //face3D.Normalize(Geometry.Orientation.Clockwise);
            //                }

            //                vector3D_1 *= 0.01;
            //                Point3D point3D = face3D.GetInternalPoint3D();
            //                point3D.Move(vector3D_1);
            //                if (!shell.Inside(point3D))
            //                {
            //                    face3D.FlipNormal(false);
            //                    reverse = true;
            //                }
            //            }

            //            face3D.Normalize(Geometry.Orientation.Clockwise);

            //            //face3D.Normalize(Geometry.Orientation.Clockwise);
            //            //if (panel.PanelGroup == PanelGroup.Roof)
            //            //{
            //            //    face3D.FlipNormal(false);
            //            //}

            //            TBD.Perimeter perimeter_Panel = Geometry.Tas.Convert.ToTBD(face3D, roomSurface_Panel);
            //            if (perimeter_Panel == null)
            //            {
            //                continue;
            //            }

            //            PanelType panelType = panel.PanelType;

            //            TBD.buildingElement buildingElement_Panel = buildingElements.Find(x => x.name == name_Panel);
            //            if (buildingElement_Panel == null)
            //            {
            //                TBD.Construction construction_TBD = null;

            //                if(panel.PanelType != PanelType.Air)
            //                {
            //                    Construction construction = panel.Construction;
            //                    if (construction != null)
            //                    {
            //                        construction_TBD = constructions.Find(x => x.name == construction.Name);
            //                        if (construction_TBD == null)
            //                        {
            //                            construction_TBD = result.AddConstruction(null);
            //                            construction_TBD.name = construction.Name;

            //                            if (construction.Transparent(materialLibrary))
            //                            {
            //                                construction_TBD.type = TBD.ConstructionTypes.tcdTransparentConstruction;
            //                            }

            //                            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            //                            if (constructionLayers != null && constructionLayers.Count != 0)
            //                            {
            //                                //constructionLayers.Reverse();

            //                                int index = 1;
            //                                foreach (ConstructionLayer constructionLayer in constructionLayers)
            //                                {
            //                                    Material material = analyticalModel?.MaterialLibrary?.GetMaterial(constructionLayer.Name) as Material;
            //                                    if (material == null)
            //                                    {
            //                                        continue;
            //                                    }

            //                                    TBD.material material_TBD = construction_TBD.AddMaterial(material);
            //                                    if (material_TBD != null)
            //                                    {
            //                                        material_TBD.width = System.Convert.ToSingle(constructionLayer.Thickness);
            //                                        construction_TBD.materialWidth[index] = System.Convert.ToSingle(constructionLayer.Thickness);
            //                                        index++;
            //                                    }
            //                                }
            //                            }

            //                            constructions.Add(construction_TBD);
            //                        }

            //                        if (panelType == PanelType.Undefined && construction != null)
            //                        {
            //                            panelType = construction.PanelType();
            //                            if (panelType == PanelType.Undefined && construction.TryGetValue(Analytical.ConstructionParameter.DefaultPanelType, out string panelTypeString))
            //                            {
            //                                panelType = Core.Query.Enum<PanelType>(panelTypeString);
            //                            }
            //                        }
            //                    }
            //                }

            //                buildingElement_Panel = result.AddBuildingElement();
            //                buildingElement_Panel.name = name_Panel;
            //                buildingElement_Panel.colour = Core.Convert.ToUint(Analytical.Query.Color(panelType));
            //                buildingElement_Panel.BEType = Query.BEType(panelType.Text());
            //                buildingElement_Panel.AssignConstruction(construction_TBD);
            //                buildingElement_Panel.ghost = panelType == PanelType.Air ? 1 : 0;
            //                buildingElements.Add(buildingElement_Panel);
            //            }

            //            if (buildingElement_Panel != null)
            //            {
            //                zoneSurface_Panel.buildingElement = buildingElement_Panel;
            //                panel_Temp.SetValue(PanelParameter.BuildingElementGuid, buildingElement_Panel.GUID);
            //            }

            //            zoneSurface_Panel.type = TBD.SurfaceType.tbdExposed;

            //            Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult = analyticalModel.GetResults<Geometry.SolarCalculator.SolarFaceSimulationResult>(panel)?.FirstOrDefault();

            //            bool adiabatic = Analytical.Query.Adiabatic(panel);

            //            List<Aperture> apertures = panel.Apertures;
            //            if (apertures != null && apertures.Count != 0)
            //            {
            //                bool @internal = adjacencyCluster.Internal(panel);

            //                Func<Face3D, TBD.zoneSurface> func = delegate (Face3D face3D_ZoneSurface)
            //                {
            //                    BoundingBox3D boundingBox3D_Aperture = face3D_ZoneSurface.GetBoundingBox();

            //                    float area = System.Convert.ToSingle(face3D_ZoneSurface.GetArea());

            //                    TBD.zoneSurface zoneSurface_Aperture = zoneSurface_Panel.AddChildSurface(area);
            //                    if (zoneSurface_Aperture == null)
            //                    {
            //                        return null;
            //                    };


            //                    //zoneSurface_Aperture.orientation = zoneSurface_Panel.orientation; 
            //                    zoneSurface_Aperture.inclination = zoneSurface_Panel.inclination;
            //                    zoneSurface_Aperture.altitude = System.Convert.ToSingle(boundingBox3D_Aperture.GetCentroid().Z);
            //                    zoneSurface_Aperture.altitudeRange = System.Convert.ToSingle(boundingBox3D_Aperture.Max.Z - boundingBox3D_Aperture.Min.Z);
            //                    zoneSurface_Aperture.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D_ZoneSurface));

            //                    zoneSurface_Aperture.type = @internal ? TBD.SurfaceType.tbdLink : zoneSurface_Panel.type;
            //                    if(adiabatic)
            //                    {
            //                        zoneSurface_Aperture.type = TBD.SurfaceType.tbdNullLink;
            //                    }

            //                    TBD.RoomSurface roomSurface_Aperture = room.AddSurface();
            //                    roomSurface_Aperture.area = zoneSurface_Aperture.area;
            //                    roomSurface_Aperture.zoneSurface = zoneSurface_Aperture;

            //                    TBD.Perimeter perimeter_Aperture = Geometry.Tas.Convert.ToTBD(face3D_ZoneSurface, roomSurface_Aperture);
            //                    if (perimeter_Aperture == null)
            //                    {
            //                        return null;
            //                    }

            //                    if (solarFaceSimulationResult != null)
            //                    {
            //                        List<TBD.SurfaceShade> surfaceShades = Modify.UpdateSurfaceShades(result, daysShades, zoneSurface_Aperture, face3D_ZoneSurface, solarFaceSimulationResult);
            //                    }

            //                    return zoneSurface_Aperture;
            //                };

            //                foreach (Aperture aperture in apertures)
            //                {
            //                    string name_Aperture = aperture.Name;
            //                    if (string.IsNullOrWhiteSpace(name_Aperture))
            //                    {
            //                        continue;
            //                    }

            //                    string name = Query.Name(aperture.UniqueName(), false, true, true, false);

            //                    Dictionary<string, Tuple<AperturePart, List<TBD.zoneSurface>>> dictionary = new Dictionary<string, Tuple<AperturePart, List<TBD.zoneSurface>>>();

            //                    double thickness = double.NaN;

            //                    thickness = aperture.GetThickness(AperturePart.Pane);
            //                    if(!double.IsNaN(thickness) && thickness > 0)
            //                    {
            //                        List<Face3D> face3Ds_Pane = aperture.GetFace3Ds(AperturePart.Pane);
            //                        if (face3Ds_Pane != null)
            //                        {
            //                            string apertureName_Pane = string.Format("{0} {1}", name, AperturePart.Pane.Sufix());
            //                            dictionary[apertureName_Pane] = new Tuple<AperturePart, List<TBD.zoneSurface>>(AperturePart.Pane, new List<TBD.zoneSurface>());
            //                            foreach (Face3D face3D_Pane in face3Ds_Pane)
            //                            {
            //                                // here we added fix so Pane/Frame on secnd side will be correctyl shaded...
            //                                if (dictionary_Panel.ContainsKey(panel.Guid))
            //                                {
            //                                    face3D_Pane.FlipNormal(false);
            //                                }
            //                                face3D_Pane.Normalize(Geometry.Orientation.Clockwise);
            //                                //face3D_Pane.FlipNormal(false);
            //                                //Face3D face3D_Pane_Temp = Geometry.Spatial.Query.Normalize<Face3D>(face3D_Pane, Geometry.Orientation.Clockwise);
            //                                //if (panel.PanelGroup != PanelGroup.Roof && dictionary_Panel.ContainsKey(panel.Guid))
            //                                //{
            //                                //    face3D_Pane.FlipNormal(false);
            //                                //}

            //                                TBD.zoneSurface zoneSurface = func.Invoke(face3D_Pane);
            //                                if (zoneSurface != null)
            //                                {
            //                                    dictionary[apertureName_Pane].Item2.Add(zoneSurface);

            //                                    if (updateGuids)
            //                                    {
            //                                        Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
            //                                        ApertureParameter apertureParameter = aperture_Temp.HasValue(ApertureParameter.PaneZoneSurfaceReference_1) ? ApertureParameter.PaneZoneSurfaceReference_2 : ApertureParameter.PaneZoneSurfaceReference_1;
            //                                        aperture_Temp.SetValue(apertureParameter, new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID));
            //                                        panel_Temp.RemoveAperture(aperture_Temp.Guid);
            //                                        panel_Temp.AddAperture(aperture_Temp);
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }

            //                    thickness = aperture.GetThickness(AperturePart.Frame);
            //                    if (!double.IsNaN(thickness) && thickness > 0)
            //                    {
            //                        List<Face3D> face3Ds_Frame = aperture.GetFace3Ds(AperturePart.Frame);
            //                        if (face3Ds_Frame != null)
            //                        {
            //                            string apertureName_Frame = string.Format("{0} {1}", name, AperturePart.Frame.Sufix());
            //                            dictionary[apertureName_Frame] = new Tuple<AperturePart, List<TBD.zoneSurface>>(AperturePart.Frame, new List<TBD.zoneSurface>());
            //                            foreach (Face3D face3D_Frame in face3Ds_Frame)
            //                            {
            //                                // here we added fix so Pane/Frame on secnd side will be correctyl shaded...
            //                                if (dictionary_Panel.ContainsKey(panel.Guid))
            //                                {
            //                                    face3D_Frame.FlipNormal(false);
            //                                }
            //                                face3D_Frame.Normalize(Geometry.Orientation.Clockwise);
            //                                //face3D_Frame.FlipNormal(false);

            //                                //if (panel.PanelGroup != PanelGroup.Roof && dictionary_Panel.ContainsKey(panel.Guid))
            //                                //{
            //                                //    face3D_Frame.FlipNormal(false);
            //                                //}

            //                                TBD.zoneSurface zoneSurface = func.Invoke(face3D_Frame);
            //                                if (zoneSurface != null)
            //                                {
            //                                    //zoneSurface.reversed = 1;
            //                                    dictionary[apertureName_Frame].Item2.Add(zoneSurface);

            //                                    if (updateGuids)
            //                                    {
            //                                        Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
            //                                        ApertureParameter apertureParameter = aperture_Temp.HasValue(ApertureParameter.FrameZoneSurfaceReference_1) ? ApertureParameter.FrameZoneSurfaceReference_2 : ApertureParameter.FrameZoneSurfaceReference_1;
            //                                        aperture_Temp.SetValue(apertureParameter, new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID));
            //                                        panel_Temp.RemoveAperture(aperture_Temp.Guid);
            //                                        panel_Temp.AddAperture(aperture_Temp);
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }

            //                    foreach(KeyValuePair<string, Tuple<AperturePart, List<TBD.zoneSurface>>> keyValuePair in dictionary)
            //                    {
            //                        TBD.buildingElement buildingElement_Aperture = buildingElements.Find(x => x.name == keyValuePair.Key);
            //                        if (buildingElement_Aperture == null)
            //                        {

            //                            AperturePart aperturePart = keyValuePair.Value.Item1;

            //                            TBD.Construction construction_TBD = null;

            //                            ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
            //                            if (apertureConstruction != null)
            //                            {
            //                                string constructionName= string.Format("{0} {1}", Query.Name(aperture.UniqueName(), false, true, false, false), aperturePart.Sufix());

            //                                construction_TBD = constructions.Find(x => x.name == constructionName);
            //                                if (construction_TBD == null)
            //                                {
            //                                    construction_TBD = result.AddConstruction(null);
            //                                    construction_TBD.name = constructionName;

            //                                    if (apertureConstruction.Transparent(materialLibrary, keyValuePair.Value.Item1))
            //                                    {
            //                                        construction_TBD.type = TBD.ConstructionTypes.tcdTransparentConstruction;
            //                                    }

            //                                    List<ConstructionLayer> constructionLayers = apertureConstruction.GetConstructionLayers(aperturePart);
            //                                    if (constructionLayers != null && constructionLayers.Count != 0)
            //                                    {
            //                                        int index = 1;
            //                                        foreach (ConstructionLayer constructionLayer in constructionLayers)
            //                                        {
            //                                            Material material = materialLibrary?.GetMaterial(constructionLayer.Name) as Material;
            //                                            if (material == null)
            //                                            {
            //                                                continue;
            //                                            }

            //                                            TBD.material material_TBD = construction_TBD.AddMaterial(material);
            //                                            if (material_TBD != null)
            //                                            {
            //                                                material_TBD.width = System.Convert.ToSingle(constructionLayer.Thickness);
            //                                                construction_TBD.materialWidth[index] = System.Convert.ToSingle(constructionLayer.Thickness);
            //                                                index++;
            //                                            }
            //                                        }
            //                                    }

            //                                    constructions.Add(construction_TBD);
            //                                }
            //                            }

            //                            if(construction_TBD != null)
            //                            {
            //                                ApertureType apertureType = aperture.ApertureType;

            //                                buildingElement_Aperture = result.AddBuildingElement();
            //                                buildingElement_Aperture.name = keyValuePair.Key;

            //                                //AperturePart aperturePart_Temp = apertureType == ApertureType.Door ? AperturePart.Frame : aperturePart;

            //                                //System.Drawing.Color color = Analytical.Query.Color(apertureType, aperturePart_Temp, aperture.Openable());
            //                                //if (aperturePart == AperturePart.Pane && aperture.TryGetValue(ApertureParameter.Color, out System.Drawing.Color color_Temp) && color_Temp != System.Drawing.Color.Empty)
            //                                //{
            //                                //    color = color_Temp;
            //                                //}

            //                                //buildingElement_Aperture.colour = Core.Convert.ToUint(color);

            //                                buildingElement_Aperture.SetColor(aperture, aperturePart);

            //                                buildingElement_Aperture.BEType = Query.BEType(keyValuePair.Value.Item1);
            //                                buildingElement_Aperture.AssignConstruction(construction_TBD);
            //                                buildingElements.Add(buildingElement_Aperture);
            //                            }



            //                            if (aperturePart == AperturePart.Pane && aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties))
            //                            {
            //                                List<TBD.ApertureType> apertureTypes = Modify.SetApertureTypes(result, buildingElement_Aperture, openingProperties);

            //                                //TBD.ApertureType apertureType = result.AddApertureType(null);
            //                                //apertureType.name = keyValuePair.Key;

            //                                //if (openingProperties.TryGetValue(OpeningPropertiesParameter.Description, out string description))
            //                                //{
            //                                //    apertureType.description = description;
            //                                //}

            //                                //apertureType.dischargeCoefficient = (float)openingProperties.GetDischargeCoefficient();

            //                                //TBD.profile profile = apertureType.GetProfile();
            //                                //profile.value = 1;

            //                                //if(openingProperties.TryGetValue(OpeningPropertiesParameter.Function, out string function))
            //                                //{
            //                                //    profile.type = TBD.ProfileTypes.ticFunctionProfile;
            //                                //    profile.function = function;
            //                                //}

            //                                //List<TBD.dayType> dayTypes = result.DayTypes();
            //                                //if (dayTypes != null)
            //                                //{
            //                                //    dayTypes.RemoveAll(x => x.name.Equals("HDD") || x.name.Equals("CDD"));
            //                                //    foreach (TBD.dayType dayType in dayTypes)
            //                                //        apertureType.SetDayType(dayType, true);
            //                                //}

            //                                //buildingElement_Aperture.AssignApertureType(apertureType);

            //                            }
            //                        }

            //                        if(updateGuids && buildingElement_Aperture != null)
            //                        {
            //                            ApertureParameter apertureParameter = keyValuePair.Value.Item1 == AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;

            //                            Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
            //                            aperture_Temp.SetValue(apertureParameter, buildingElement_Aperture.GUID);
            //                            panel_Temp.RemoveAperture(aperture_Temp.Guid);
            //                            panel_Temp.AddAperture(aperture_Temp);
            //                        }

            //                        foreach(TBD.zoneSurface zoneSurface in keyValuePair.Value.Item2)
            //                        {
            //                            if (buildingElement_Aperture != null)
            //                            {
            //                                zoneSurface.buildingElement = buildingElement_Aperture;
            //                            }

            //                            if (!dictionary_Aperture.TryGetValue(aperture.Guid, out List<Tuple<AperturePart, TBD.zoneSurface, bool>> zoneSurfaces_Aperture) || zoneSurfaces_Aperture == null)
            //                            {
            //                                zoneSurfaces_Aperture = new List<Tuple<AperturePart, TBD.zoneSurface, bool>>();
            //                                dictionary_Aperture[aperture.Guid] = zoneSurfaces_Aperture;
            //                            }

            //                            zoneSurfaces_Aperture.Add(new Tuple<AperturePart, TBD.zoneSurface, bool>(keyValuePair.Value.Item1, zoneSurface, dictionary_Panel.ContainsKey(panel.Guid)));
            //                        }
            //                    }
            //                }
            //            }

            //            if (solarFaceSimulationResult != null)
            //            {
            //                List<TBD.SurfaceShade> surfaceShades = Modify.UpdateSurfaceShades(result, daysShades, zoneSurface_Panel, analyticalModel, solarFaceSimulationResult);
            //            }

            //            zoneSurface_Panel.type = adiabatic ? TBD.SurfaceType.tbdNullLink : Query.SurfaceType(panelType);

            //            if (!dictionary_Panel.TryGetValue(panel.Guid, out List<Tuple<TBD.zoneSurface, bool>> zoneSurfaces_Panel) || zoneSurfaces_Panel == null)
            //            {
            //                zoneSurfaces_Panel = new List<Tuple<TBD.zoneSurface, bool>>();
            //                dictionary_Panel[panel.Guid] = zoneSurfaces_Panel;
            //            }

            //            zoneSurfaces_Panel.Add(new Tuple<TBD.zoneSurface, bool>( zoneSurface_Panel, reverse));

            //            if (updateGuids)
            //            {
            //                analyticalModel.AddPanel(panel_Temp);
            //            }
            //        }
            //    }
            //}

            //foreach (KeyValuePair<Guid, List<Tuple<TBD.zoneSurface, bool>>> keyValuePair in dictionary_Panel)
            //{
            //    if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
            //    {
            //        continue;
            //    }

            //    keyValuePair.Value[1].Item1.linkSurface = keyValuePair.Value[0].Item1;
            //    keyValuePair.Value[0].Item1.linkSurface = keyValuePair.Value[1].Item1;

            //    Panel panel = adjacencyCluster.GetObject<Panel>(keyValuePair.Key);

            //    bool reverse_1 = keyValuePair.Value[0].Item2;
            //    bool reverse_2 = keyValuePair.Value[1].Item2;

            //    if (keyValuePair.Value[0].Item1.inclination == 0 || keyValuePair.Value[0].Item1.inclination == 180)
            //    {
            //        if (reverse_1)
            //        {
            //            keyValuePair.Value[0].Item1.reversed = 1;
            //        }
            //        if (reverse_2)
            //        {
            //            //reverse only one that are outside when test if in shell
            //            keyValuePair.Value[1].Item1.reversed = 1;
            //        }
            //        if(!reverse_1 && !reverse_2)
            //        {
            //            keyValuePair.Value.Last().Item1.reversed = 1;
            //        }
            //    }
            //    else
            //    {
            //        float orientation = keyValuePair.Value[1].Item1.orientation;
            //        orientation += 180;
            //        if (orientation >= 360)
            //        {
            //            orientation -= 360;
            //        }

            //        keyValuePair.Value[1].Item1.orientation = orientation;

            //        List<Space> spaces_Adjacent = adjacencyCluster.GetSpaces(panel);
            //        bool adjacent = spaces_Adjacent != null && spaces_Adjacent.Count > 1;



            //        if (panel.PanelGroup == PanelGroup.Floor && adjacent)
            //        {
            //            keyValuePair.Value[1].Item1.inclination = Math.Abs(180 - keyValuePair.Value[1].Item1.inclination);

            //            if (reverse_1)
            //            {
            //                //reverse only one that are outside when test if in shell
            //                keyValuePair.Value[0].Item1.reversed = 1;
            //            }


            //            if (reverse_2)
            //            {
            //                //reverse only one that are outside when test if in shell
            //                keyValuePair.Value[1].Item1.reversed = 1;
            //            }
            //        }
            //        else
            //        {
            //            keyValuePair.Value[1].Item1.reversed = 1;
            //        }

            //        float inclination = keyValuePair.Value[1].Item1.inclination;
            //        while (inclination > 180)
            //        {
            //            inclination -= 180;
            //        }
            //        keyValuePair.Value[1].Item1.inclination = inclination;
            //    }
            //}

            //foreach (KeyValuePair<Guid, List<Tuple<AperturePart, TBD.zoneSurface, bool>>> keyValuePair in dictionary_Aperture)
            //{
            //    if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
            //    {
            //        continue;
            //    }

            //    List<TBD.zoneSurface> zoneSurfaces = null;

            //    zoneSurfaces = keyValuePair.Value.FindAll(x => x.Item1 == AperturePart.Frame).ConvertAll(x => x.Item2);
            //    if(zoneSurfaces.Count == 2)
            //    {
            //        zoneSurfaces[1].linkSurface = zoneSurfaces[0];
            //        zoneSurfaces[0].linkSurface = zoneSurfaces[1];
            //    }


            //    zoneSurfaces = keyValuePair.Value.FindAll(x => x.Item1 == AperturePart.Pane).ConvertAll(x => x.Item2);
            //    if (zoneSurfaces.Count == 2)
            //    {
            //        zoneSurfaces[1].linkSurface = zoneSurfaces[0];
            //        zoneSurfaces[0].linkSurface = zoneSurfaces[1];
            //    }

            //    foreach (Tuple<AperturePart, TBD.zoneSurface, bool> tuple in keyValuePair.Value)
            //    {
            //        if (!tuple.Item3)
            //        {
            //            continue;
            //        }

            //        float orientation = tuple.Item2.orientation;
            //        orientation += 180;
            //        if (orientation >= 360)
            //        {
            //            orientation -= 360;
            //        }
            //        tuple.Item2.orientation = orientation;

            //        tuple.Item2.reversed = 1;  // only second panel window does not workk internla
            //    }

            //    //if (keyValuePair.Value[0].inclination == 0 || keyValuePair.Value[0].inclination == 180)
            //    //{
            //    //    float inclination = keyValuePair.Value[0].inclination;
            //    //    inclination -= 180;
            //    //    if (inclination < 0)
            //    //    {
            //    //        inclination += 360;
            //    //    }

            //    //    keyValuePair.Value[0].inclination = inclination;
            //    //    keyValuePair.Value[0].reversed = 1;
            //    //}
            //    //else
            //    //{
            //    //    float orientation = keyValuePair.Value[1].orientation;
            //    //    orientation += 180;
            //    //    if (orientation >= 360)
            //    //    {
            //    //        orientation -= 360;
            //    //    }

            //    //    keyValuePair.Value[1].orientation = orientation;
            //    //    keyValuePair.Value[1].reversed = 1;


            //    //}
            //}

            Modify.AddDefaultZoneGroups(result, adjacencyCluster, undefinedZoneGroupName, allZoneGroupName);

            return result;
        }
    }
}
