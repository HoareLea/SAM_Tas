using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using TBD;
using TCD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool Update(this profile profile_TBD, Profile profile, double factor)
        {
            if (profile_TBD == null || profile == null || profile.Count == -1)
                return false;

            profile_TBD.name = profile.Name;
            profile_TBD.description = profile.Name;

            if (profile.Count == 1)
            {
                profile_TBD.type = ProfileTypes.ticValueProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);
                profile_TBD.value = System.Convert.ToSingle(profile.GetValues()[0]);
                return true;
            }

            if(profile.Count <= 24)
            {
                profile_TBD.type = ProfileTypes.ticHourlyProfile;
                profile_TBD.factor = System.Convert.ToSingle(factor);

                for (int i = 0; i <= 23; i++)
                    profile_TBD.hourlyValues[i + 1] = System.Convert.ToSingle(profile[i]);

                return true;
            }

            profile_TBD.type = ProfileTypes.ticYearlyProfile;
            profile_TBD.factor = System.Convert.ToSingle(factor);

            //object yearlyValues_TBD = profile_TBD.GetYearlyValues();

            //float[] array = Query.Array<float>(yearlyValues_TBD);

            double[] yearlyValues =  profile.GetYearlyValues();
            float[] yearlyValues_float = new float[yearlyValues.Length];
            for (int i = 0; i < yearlyValues_float.Length; i++)
                yearlyValues_float[i] = System.Convert.ToSingle(yearlyValues[i]);

            profile_TBD.SetYearlyValues(yearlyValues_float);

            //for (int i = 0; i < 8759; i++)
            //    profile_TBD.yearlyValues[i] = System.Convert.ToSingle(profile[i]);

            return true;
        }

        public static bool Update(this CoolingDesignDay coolingDesignDay_TBD, DesignDay designDay, dayType dayType = null, int repetitions = 30)
        {
            if(coolingDesignDay_TBD == null || designDay == null)
            {
                return false;
            }

            coolingDesignDay_TBD.name = designDay.Name;
            foreach(TBD.DesignDay designDay_TBD in coolingDesignDay_TBD.DesignDays())
            {
                designDay_TBD?.Update(designDay, dayType, repetitions);
            }

            return true;
        }

        public static bool Update(this HeatingDesignDay heatingDesignDay_TBD, DesignDay designDay, dayType dayType = null, int repetitions = 30)
        {
            if (heatingDesignDay_TBD == null || designDay == null)
            {
                return false;
            }

            heatingDesignDay_TBD.name = designDay.Name;
            foreach (TBD.DesignDay designDay_TBD in heatingDesignDay_TBD.DesignDays())
            {
                designDay_TBD?.Update(designDay, dayType, repetitions);
            }

            return true;
        }

        public static bool Update(this TBD.DesignDay designDay_TBD, DesignDay designDay, dayType dayType = null, int repetitions = 30)
        {
            if(designDay_TBD == null)
            {
                return false;
            }

            designDay_TBD.yearDay = designDay.GetDateTime().DayOfYear;
            designDay_TBD.repetitions = repetitions;
            designDay_TBD.description = designDay.Description;

            if(dayType != null)
            {
                designDay_TBD.SetDayType(dayType);
            }

            //TODO: TAS MEETING: Discuss with Tas how to faster copy data
            return Weather.Tas.Modify.Update(designDay_TBD?.GetWeatherDay(), designDay);
        }

        public static bool Update(this ConstructionFolder constructionFolder, ConstructionManager constructionManager, ApertureConstruction apertureConstruction)
        {
            List<TCD.Construction> constructions = Convert.ToTCD_Constructions(apertureConstruction, constructionFolder, constructionManager);
            return constructions != null && constructions.Count > 0;
        }
        
        public static bool Update(this ConstructionManager constructionManager, IThermalTransmittanceCalculationResult thermalTransmittanceCalculationResult, double tolerance = Tolerance.MacroDistance)
        {
            if (constructionManager == null || thermalTransmittanceCalculationResult == null)
            {
                return false;
            }

            if(thermalTransmittanceCalculationResult is LayerThicknessCalculationResult)
            {
                return Update(constructionManager, (LayerThicknessCalculationResult)thermalTransmittanceCalculationResult, tolerance);
            }

            return Update(constructionManager, thermalTransmittanceCalculationResult as dynamic);
        }

        public static bool Update(this ConstructionManager constructionManager, LayerThicknessCalculationResult layerThicknessCalculationResult, double tolerance = Tolerance.MacroDistance)
        {
            if(constructionManager == null || layerThicknessCalculationResult == null)
            {
                return false;
            }

            double thickness = layerThicknessCalculationResult.Thickness;
            if (double.IsNaN(thickness))
            {
                return false;
            }

            thickness = Core.Query.Round(thickness, tolerance);

            Construction construction = constructionManager.GetConstructions(layerThicknessCalculationResult.ConstructionName, TextComparisonType.Equals, true)?.FirstOrDefault();
            if(construction == null)
            {
                return false;
            }

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if(constructionLayers == null || constructionLayers.Count == 0)
            {
                return false;
            } 

            int layerIndex = layerThicknessCalculationResult.LayerIndex;
            if(constructionLayers.Count <= layerIndex)
            {
                return false;
            }

            ConstructionLayer constructionLayer = constructionLayers[layerIndex];
            if(constructionLayer == null)
            {
                return false;
            }

            string materialName = string.Format("{0}_{1}m", constructionLayer.Name, thickness);
            Core.IMaterial material = constructionManager.GetMaterial(materialName);
            if(material == null)
            {
                material = constructionManager.GetMaterial(constructionLayer.Name);
                if (material == null)
                {
                    return false;
                }
            }

            if(!material.TryGetValue(Core.MaterialParameter.DefaultThickness, out double defaultThickness))
            {
                if(layerThicknessCalculationResult.Thickness == defaultThickness)
                {
                    return true;
                }
            }

            if(material.Name != materialName && material is Material)
            {
                string description =  ((Material)material).Description;

                material = Core.Create.Material((Material)material, materialName, materialName, description);
            }

            material.SetValue(Core.MaterialParameter.DefaultThickness, thickness);

            constructionLayers[layerIndex] = new ConstructionLayer(materialName, thickness);

            construction = new Construction(construction, constructionLayers);

            constructionManager.Add(construction);

            constructionManager.Add(material);

            return true;
        }

        public static bool Update(this ConstructionManager constructionManager, ConstructionCalculationResult constructionCalculationResult)
        {
            if(constructionManager == null || constructionCalculationResult == null)
            {
                return false;
            }

            string initialConstructionName = constructionCalculationResult.InitialConstructionName;
            if(initialConstructionName == null)
            {
                return false;
            }

            string constructionName = constructionCalculationResult.ConstructionName;
            if(constructionName == null)
            {
                return false;
            }

            Construction initialConstruction = constructionManager.GetConstructions(initialConstructionName)?.FirstOrDefault();
            if(initialConstruction == null)
            {
                return false;
            }

            Construction construction = constructionManager.GetConstructions(constructionName)?.FirstOrDefault();
            if(construction == null)
            {
                return false;
            }

            Construction construction_Updated = new Construction(initialConstruction.Guid, construction, initialConstruction.Name);

            return constructionManager.Add(construction_Updated);
        }

        public static bool Update(this ConstructionManager constructionManager, ApertureConstructionCalculationResult apertureConstructionCalculationResult)
        {
            if (constructionManager == null || apertureConstructionCalculationResult == null)
            {
                return false;
            }

            if (constructionManager == null || apertureConstructionCalculationResult == null)
            {
                return false;
            }

            string initialApertureConstructionName = apertureConstructionCalculationResult.InitialApertureConstructionName;
            if (initialApertureConstructionName == null)
            {
                return false;
            }

            string apertureConstructionName = apertureConstructionCalculationResult.ApertureConstructionName;
            if (apertureConstructionName == null)
            {
                return false;
            }

            ApertureConstruction initialApertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, initialApertureConstructionName)?.FirstOrDefault();
            if (initialApertureConstruction == null)
            {
                return false;
            }

            ApertureConstruction apertureConstruction = constructionManager.GetApertureConstructions(apertureConstructionCalculationResult.ApertureType, apertureConstructionName)?.FirstOrDefault();
            if (apertureConstruction == null)
            {
                return false;
            }

            ApertureConstruction apertureConstruction_Updated = new ApertureConstruction(initialApertureConstruction.Guid, apertureConstruction, initialApertureConstruction.Name);

            return constructionManager.Add(apertureConstruction_Updated);
        }

        public static bool Update(this ConstructionManager constructionManager, ConstructionFolder constructionFolder, Category category = null, double tolerance = Tolerance.MacroDistance)
        {
            if(constructionManager == null || constructionFolder == null)
            {
                return false;
            }

            bool result = false;

            Category category_Temp = Core.Create.Category(constructionFolder.name, category);

            int index;

            index = 1;
            TCD.Construction construction_TCD = constructionFolder.constructions(index);
            while(construction_TCD != null)
            {
                Construction construction = construction_TCD.ToSAM(tolerance);
                if(construction != null)
                {
                    construction.SetValue(ParameterizedSAMObjectParameter.Category, category_Temp);

                    List<Core.IMaterial> materials = construction_TCD.ToSAM_Materials();
                    if (materials != null)
                    {
                        materials.ForEach(x => constructionManager.Add(x));
                    }

                    constructionManager.Add(construction);
                    result = true;
                }

                index++;
                construction_TCD = constructionFolder.constructions(index);
            }

            index = 1;
            ConstructionFolder constructionFolder_Child = constructionFolder.childFolders(index);
            while(constructionFolder_Child != null)
            {
                Update(constructionManager, constructionFolder_Child, category_Temp, tolerance);
                index++;
                constructionFolder_Child = constructionFolder.childFolders(index);
            }

            return result;
        }

        public static bool Update(this ConstructionManager constructionManager, MaterialFolder materialFolder, Category category = null)
        {
            if (constructionManager == null || materialFolder == null)
            {
                return false;
            }

            bool result = false;

            Category category_Temp = Core.Create.Category(materialFolder.name, category);

            int index;

            index = 1;
            TCD.material material_TCD = materialFolder.materials(index);
            while (material_TCD != null)
            {
                Core.IMaterial material = material_TCD.ToSAM();
                if (material != null)
                {
                    material.SetValue(ParameterizedSAMObjectParameter.Category, category_Temp);
                    constructionManager.Add(material);
                    result = true;
                }

                index++;
                material_TCD = materialFolder.materials(index);
            }

            index = 1;
            MaterialFolder materialFolder_Child = materialFolder.childFolders(index);
            while (materialFolder_Child != null)
            {
                Update(constructionManager, materialFolder_Child, category_Temp);
                index++;
                materialFolder_Child = materialFolder_Child.childFolders(index);
            }

            return result;
        }

        public static bool Update(this ConstructionManager constructionManager, Guid source, Guid destination, bool replace)
        {
            IAnalyticalObject analyticalObject_Source = constructionManager.Constructions?.Find(x => x.Guid == source);
            if (analyticalObject_Source == null)
            {
                analyticalObject_Source = constructionManager.ApertureConstructions?.Find(x => x.Guid == source);
            }

            if (analyticalObject_Source == null)
            {
                return false;
            }

            IAnalyticalObject analyticalObject_Destination = constructionManager.Constructions?.Find(x => x.Guid == destination);
            if (analyticalObject_Destination == null)
            {
                analyticalObject_Destination = constructionManager.ApertureConstructions?.Find(x => x.Guid == destination);
            }

            if (analyticalObject_Destination == null)
            {
                return false;
            }

            if(replace && analyticalObject_Destination.GetType() == analyticalObject_Source.GetType())
            {
                if(analyticalObject_Source is Construction)
                {
                    Construction construction_Source = analyticalObject_Source as Construction;
                    Construction construction_Destionation = analyticalObject_Destination as Construction;

                    Construction construction_Updated = new Construction(construction_Destionation.Guid, construction_Source, construction_Source.Name);

                    return constructionManager.Add(construction_Updated);
                }
                else if (analyticalObject_Source is ApertureConstruction)
                {
                    ApertureConstruction apertureConstruction_Source = analyticalObject_Source as ApertureConstruction;
                    ApertureConstruction apertureConstruction_Destionation = analyticalObject_Destination as ApertureConstruction;

                    ApertureConstruction apertureConstruction_Updated = new ApertureConstruction(apertureConstruction_Destionation.Guid, apertureConstruction_Source, apertureConstruction_Source.Name);

                    return constructionManager.Add(apertureConstruction_Updated);
                }

                return false;
            }
            else
            {
                List<ConstructionLayer> constructionLayers = null;
                if(analyticalObject_Source is Construction)
                {
                    constructionLayers = ((Construction)analyticalObject_Source).ConstructionLayers;

                }
                else if(analyticalObject_Source is ApertureConstruction)
                {
                    constructionLayers = ((ApertureConstruction)analyticalObject_Source).PaneConstructionLayers;
                }
                else
                {
                    return false;
                }

                if(analyticalObject_Destination is Construction)
                {
                    Construction construction_Updated = new Construction((Construction)analyticalObject_Destination, constructionLayers);
                    return constructionManager.Add(construction_Updated);
                }
                else if(analyticalObject_Destination is ApertureConstruction)
                {
                    ApertureConstruction apertureConstruction_Updated = new ApertureConstruction((ApertureConstruction)analyticalObject_Destination, constructionLayers);
                    return constructionManager.Add(apertureConstruction_Updated);
                }
            }

            return false;
        }

        public static void Update(this Building building, AdjacencyCluster adjacencyCluster, MaterialLibrary materialLibrary, bool updateGuids = false)
        {
            adjacencyCluster = adjacencyCluster.UpdateNormals(true, false, false);
            adjacencyCluster.Normalize(true, Geometry.Orientation.Clockwise);

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null)
            {
                return;
            }

            List<DaysShade> daysShades = new List<DaysShade>();

            Plane plane = Plane.WorldXY;

            Dictionary<Guid, List<Tuple<zoneSurface, bool>>> dictionary_Panel = new Dictionary<Guid, List<Tuple<zoneSurface, bool>>>();
            Dictionary<Guid, List<Tuple<AperturePart, zoneSurface, bool>>> dictionary_Aperture = new Dictionary<Guid, List<Tuple<AperturePart, zoneSurface, bool>>>();
            foreach (Space space in spaces)
            {
                Shell shell = adjacencyCluster.Shell(space);
                BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
                if (shell == null || boundingBox3D == null)
                {
                    return;
                }

                zone zone = building.AddZone();
                if (updateGuids)
                {
                    Space space_Temp = new Space(space);
                    space_Temp.SetValue(SpaceParameter.ZoneGuid, zone.GUID);
                    adjacencyCluster.AddSpace(space_Temp);
                }

                zone.name = space.Name;
                double volume = shell.Volume();
                if (double.IsNaN(volume))
                {
                    volume = space.GetValue<double>(Analytical.SpaceParameter.Volume);
                }

                zone.volume = System.Convert.ToSingle(volume);

                if (space.TryGetValue(Analytical.SpaceParameter.Color, out SAMColor sAMColor) && sAMColor != null)
                {
                    zone.colour = Core.Convert.ToUint(sAMColor.ToColor());
                }

                List<Face3D> face3Ds = Geometry.Spatial.Query.Section(shell, (boundingBox3D.Max.Z - boundingBox3D.Min.Z) / 2, false);
                if (face3Ds != null && face3Ds.Count != 0)
                {
                    face3Ds.RemoveAll(x => x == null || !x.IsValid());
                    zone.floorArea = System.Convert.ToSingle(face3Ds.ConvertAll(x => x.GetArea()).Sum());
                    zone.exposedPerimeter = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Planar.Query.Perimeter(x.ExternalEdge2D)).Sum());
                    zone.length = System.Convert.ToSingle(face3Ds.ConvertAll(x => Geometry.Tas.Query.Length(x)).Sum());
                }

                room room = zone.AddRoom();

                List<buildingElement> buildingElements = building.BuildingElements();
                List<TBD.Construction> constructions = building.Constructions();

                int index_Space = adjacencyCluster.GetIndex(space);

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

                        zoneSurface zoneSurface_Panel = zone.AddSurface();

                        Core.Tas.ZoneSurfaceReference zoneSurfaceReference = new Core.Tas.ZoneSurfaceReference(zoneSurface_Panel.number, zone.GUID);

                        Panel panel_Temp = Analytical.Create.Panel(panel);

                        PanelParameter panelParameter = panel.HasValue(PanelParameter.ZoneSurfaceReference_1) ? PanelParameter.ZoneSurfaceReference_2 : PanelParameter.ZoneSurfaceReference_1;
                        panel_Temp.SetValue(panelParameter, zoneSurfaceReference);

                        float orientation = System.Convert.ToSingle(Geometry.Object.Spatial.Query.Azimuth(panel, Vector3D.WorldY));
                        orientation += 180;
                        if (orientation >= 360)
                        {
                            orientation -= 360;
                        }
                        zoneSurface_Panel.orientation = orientation;

                        float inclination = System.Convert.ToSingle(Geometry.Spatial.Query.Tilt(normal));
                        if (inclination == 0 || inclination == 180)
                        {
                            inclination -= 180;
                            if (inclination < 0)
                            {
                                inclination += 360;
                            }
                        }
                        else
                        {
                            inclination = Math.Min(inclination, 180 - inclination);
                        }

                        zoneSurface_Panel.inclination = inclination;

                        zoneSurface_Panel.altitude = System.Convert.ToSingle(boundingBox3D_Panel.GetCentroid().Z);
                        zoneSurface_Panel.altitudeRange = System.Convert.ToSingle(boundingBox3D_Panel.Max.Z - boundingBox3D_Panel.Min.Z);
                        zoneSurface_Panel.area = System.Convert.ToSingle(face3D_Panel.GetArea());
                        zoneSurface_Panel.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D_Panel));

                        RoomSurface roomSurface_Panel = room.AddSurface();
                        roomSurface_Panel.area = zoneSurface_Panel.area;
                        roomSurface_Panel.zoneSurface = zoneSurface_Panel;

                        Face3D face3D = panel.GetFace3D(true);

                        if (dictionary_Panel.ContainsKey(panel.Guid))
                        {
                            face3D.FlipNormal(false);
                        }

                        List<Space> spaces_Adjacent = adjacencyCluster.GetSpaces(panel);
                        bool reverse = false;
                        if (panel.PanelGroup == PanelGroup.Floor && spaces_Adjacent != null && spaces_Adjacent.Count > 1)
                        {
                            Vector3D vector3D_1 = Vector3D.WorldZ.GetNegated();
                            Vector3D vector3D_2 = face3D.GetPlane().Normal;
                            if (!vector3D_1.SameHalf(vector3D_2))
                            {
                                face3D.FlipNormal(false);
                            }

                            vector3D_1 *= 0.01;
                            Point3D point3D = face3D.GetInternalPoint3D();
                            point3D.Move(vector3D_1);
                            if (!shell.Inside(point3D))
                            {
                                face3D.FlipNormal(false);
                                reverse = true;
                            }
                        }

                        face3D.Normalize(Geometry.Orientation.Clockwise);

                        Perimeter perimeter_Panel = Geometry.Tas.Convert.ToTBD(face3D, roomSurface_Panel);
                        if (perimeter_Panel == null)
                        {
                            continue;
                        }

                        PanelType panelType = panel.PanelType;

                        buildingElement buildingElement_Panel = buildingElements.Find(x => x.name == name_Panel);
                        if (buildingElement_Panel == null)
                        {
                            TBD.Construction construction_TBD = null;

                            if (panel.PanelType != PanelType.Air)
                            {
                                Construction construction = panel.Construction;
                                if (construction != null)
                                {
                                    construction_TBD = constructions.Find(x => x.name == construction.Name);
                                    if (construction_TBD == null)
                                    {
                                        construction_TBD = building.AddConstruction(null);
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

                                    if (panelType == PanelType.Undefined && construction != null)
                                    {
                                        panelType = construction.PanelType();
                                        if (panelType == PanelType.Undefined && construction.TryGetValue(Analytical.ConstructionParameter.DefaultPanelType, out string panelTypeString))
                                        {
                                            panelType = Core.Query.Enum<PanelType>(panelTypeString);
                                        }
                                    }
                                }
                            }

                            buildingElement_Panel = building.AddBuildingElement();
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
                            panel_Temp.SetValue(PanelParameter.BuildingElementGuid, buildingElement_Panel.GUID);
                        }

                        //FeatureShade
                        //if (buildingElement_Panel != null)
                        //{
                        //    buildingElement_Panel.RemoveFeatureShades();

                        //    FeatureShade featureShade = panel_Temp.GetValue<FeatureShade>(Analytical.PanelParameter.FeatureShade);
                        //    if (featureShade != null)
                        //    {
                        //        TBD.FeatureShade featureShade_TBD = Convert.ToTBD(featureShade, building);
                        //        if(featureShade_TBD != null)
                        //        {
                        //            buildingElement_Panel.AssignFeatureShade(featureShade_TBD);
                        //        }
                        //    }
                        //}

                        zoneSurface_Panel.type = SurfaceType.tbdExposed;

                        Geometry.SolarCalculator.SolarFaceSimulationResult solarFaceSimulationResult = adjacencyCluster.GetResults<Geometry.SolarCalculator.SolarFaceSimulationResult>(panel, null)?.FirstOrDefault();

                        bool adiabatic = Analytical.Query.Adiabatic(panel);

                        List<Aperture> apertures = panel.Apertures;
                        if (apertures != null && apertures.Count != 0)
                        {
                            bool @internal = adjacencyCluster.Internal(panel);

                            Func<Face3D, zoneSurface> func = delegate (Face3D face3D_ZoneSurface)
                            {
                                BoundingBox3D boundingBox3D_Aperture = face3D_ZoneSurface.GetBoundingBox();

                                float area = System.Convert.ToSingle(face3D_ZoneSurface.GetArea());

                                zoneSurface zoneSurface_Aperture = zoneSurface_Panel.AddChildSurface(area);
                                if (zoneSurface_Aperture == null)
                                {
                                    return null;
                                };


                                //zoneSurface_Aperture.orientation = zoneSurface_Panel.orientation; 
                                zoneSurface_Aperture.inclination = zoneSurface_Panel.inclination;
                                zoneSurface_Aperture.altitude = System.Convert.ToSingle(boundingBox3D_Aperture.GetCentroid().Z);
                                zoneSurface_Aperture.altitudeRange = System.Convert.ToSingle(boundingBox3D_Aperture.Max.Z - boundingBox3D_Aperture.Min.Z);
                                zoneSurface_Aperture.planHydraulicDiameter = System.Convert.ToSingle(Geometry.Tas.Query.HydraulicDiameter(face3D_ZoneSurface));

                                zoneSurface_Aperture.type = @internal ? SurfaceType.tbdLink : zoneSurface_Panel.type;
                                if (adiabatic)
                                {
                                    zoneSurface_Aperture.type = SurfaceType.tbdNullLink;
                                }

                                RoomSurface roomSurface_Aperture = room.AddSurface();
                                roomSurface_Aperture.area = zoneSurface_Aperture.area;
                                roomSurface_Aperture.zoneSurface = zoneSurface_Aperture;

                                Perimeter perimeter_Aperture = Geometry.Tas.Convert.ToTBD(face3D_ZoneSurface, roomSurface_Aperture);
                                if (perimeter_Aperture == null)
                                {
                                    return null;
                                }

                                if (solarFaceSimulationResult != null)
                                {
                                    List<SurfaceShade> surfaceShades = UpdateSurfaceShades(building, daysShades, zoneSurface_Aperture, face3D_ZoneSurface, solarFaceSimulationResult);
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

                                string name = Query.Name(aperture.UniqueName(), false, true, true, false);

                                Dictionary<string, Tuple<AperturePart, List<zoneSurface>>> dictionary = new Dictionary<string, Tuple<AperturePart, List<zoneSurface>>>();

                                double thickness = double.NaN;

                                thickness = aperture.GetThickness(AperturePart.Pane);
                                if (!double.IsNaN(thickness) && thickness > 0)
                                {
                                    List<Face3D> face3Ds_Pane = aperture.GetFace3Ds(AperturePart.Pane);
                                    if (face3Ds_Pane != null)
                                    {
                                        string apertureName_Pane = string.Format("{0} {1}", name, AperturePart.Pane.Sufix());
                                        dictionary[apertureName_Pane] = new Tuple<AperturePart, List<zoneSurface>>(AperturePart.Pane, new List<zoneSurface>());
                                        foreach (Face3D face3D_Pane in face3Ds_Pane)
                                        {
                                            // here we added fix so Pane/Frame on secnd side will be correctyl shaded...
                                            if (dictionary_Panel.ContainsKey(panel.Guid))
                                            {
                                                face3D_Pane.FlipNormal(false);
                                            }
                                            face3D_Pane.Normalize(Geometry.Orientation.Clockwise);

                                            zoneSurface zoneSurface = func.Invoke(face3D_Pane);
                                            if (zoneSurface != null)
                                            {
                                                dictionary[apertureName_Pane].Item2.Add(zoneSurface);

                                                if (updateGuids)
                                                {
                                                    Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
                                                    ApertureParameter apertureParameter = aperture_Temp.HasValue(ApertureParameter.PaneZoneSurfaceReference_1) ? ApertureParameter.PaneZoneSurfaceReference_2 : ApertureParameter.PaneZoneSurfaceReference_1;
                                                    aperture_Temp.SetValue(apertureParameter, new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID));
                                                    panel_Temp.RemoveAperture(aperture_Temp.Guid);
                                                    panel_Temp.AddAperture(aperture_Temp);
                                                }
                                            }
                                        }
                                    }
                                }

                                thickness = aperture.GetThickness(AperturePart.Frame);
                                if (!double.IsNaN(thickness) && thickness > 0)
                                {
                                    List<Face3D> face3Ds_Frame = aperture.GetFace3Ds(AperturePart.Frame);
                                    if (face3Ds_Frame != null)
                                    {
                                        string apertureName_Frame = string.Format("{0} {1}", name, AperturePart.Frame.Sufix());
                                        dictionary[apertureName_Frame] = new Tuple<AperturePart, List<zoneSurface>>(AperturePart.Frame, new List<zoneSurface>());
                                        foreach (Face3D face3D_Frame in face3Ds_Frame)
                                        {
                                            // here we added fix so Pane/Frame on secnd side will be correctyl shaded...
                                            if (dictionary_Panel.ContainsKey(panel.Guid))
                                            {
                                                face3D_Frame.FlipNormal(false);
                                            }
                                            face3D_Frame.Normalize(Geometry.Orientation.Clockwise);

                                            zoneSurface zoneSurface = func.Invoke(face3D_Frame);
                                            if (zoneSurface != null)
                                            {
                                                //zoneSurface.reversed = 1;
                                                dictionary[apertureName_Frame].Item2.Add(zoneSurface);

                                                if (updateGuids)
                                                {
                                                    Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
                                                    ApertureParameter apertureParameter = aperture_Temp.HasValue(ApertureParameter.FrameZoneSurfaceReference_1) ? ApertureParameter.FrameZoneSurfaceReference_2 : ApertureParameter.FrameZoneSurfaceReference_1;
                                                    aperture_Temp.SetValue(apertureParameter, new Core.Tas.ZoneSurfaceReference(zoneSurface.number, zone.GUID));
                                                    panel_Temp.RemoveAperture(aperture_Temp.Guid);
                                                    panel_Temp.AddAperture(aperture_Temp);
                                                }
                                            }
                                        }
                                    }
                                }

                                foreach (KeyValuePair<string, Tuple<AperturePart, List<zoneSurface>>> keyValuePair in dictionary)
                                {
                                    buildingElement buildingElement_Aperture = buildingElements.Find(x => x.name == keyValuePair.Key);
                                    if (buildingElement_Aperture == null)
                                    {

                                        AperturePart aperturePart = keyValuePair.Value.Item1;

                                        TBD.Construction construction_TBD = null;

                                        ApertureConstruction apertureConstruction = aperture.ApertureConstruction;
                                        if (apertureConstruction != null)
                                        {
                                            string constructionName = string.Format("{0} {1}", Query.Name(aperture.UniqueName(), false, true, false, false), aperturePart.Sufix());

                                            construction_TBD = constructions.Find(x => x.name == constructionName);
                                            if (construction_TBD == null)
                                            {
                                                construction_TBD = building.AddConstruction(null);
                                                construction_TBD.name = constructionName;

                                                if (apertureConstruction.Transparent(materialLibrary, keyValuePair.Value.Item1))
                                                {
                                                    construction_TBD.type = TBD.ConstructionTypes.tcdTransparentConstruction;
                                                }

                                                List<ConstructionLayer> constructionLayers = apertureConstruction.GetConstructionLayers(aperturePart);
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

                                        if (construction_TBD != null)
                                        {
                                            ApertureType apertureType = aperture.ApertureType;

                                            buildingElement_Aperture = building.AddBuildingElement();
                                            buildingElement_Aperture.name = keyValuePair.Key;

                                            buildingElement_Aperture.SetColor(aperture, aperturePart);

                                            buildingElement_Aperture.BEType = Query.BEType(keyValuePair.Value.Item1);
                                            buildingElement_Aperture.AssignConstruction(construction_TBD);
                                            buildingElements.Add(buildingElement_Aperture);
                                        }



                                        if (aperturePart == AperturePart.Pane && aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties))
                                        {
                                            List<TBD.ApertureType> apertureTypes = SetApertureTypes(building, buildingElement_Aperture, openingProperties);
                                        }
                                    }

                                    if (updateGuids && buildingElement_Aperture != null)
                                    {
                                        ApertureParameter apertureParameter = keyValuePair.Value.Item1 == AperturePart.Frame ? ApertureParameter.FrameBuildingElementGuid : ApertureParameter.PaneBuildingElementGuid;

                                        Aperture aperture_Temp = panel_Temp.GetAperture(aperture.Guid);
                                        aperture_Temp.SetValue(apertureParameter, buildingElement_Aperture.GUID);
                                        panel_Temp.RemoveAperture(aperture_Temp.Guid);
                                        panel_Temp.AddAperture(aperture_Temp);
                                    }

                                    foreach (zoneSurface zoneSurface in keyValuePair.Value.Item2)
                                    {
                                        if (buildingElement_Aperture != null)
                                        {
                                            zoneSurface.buildingElement = buildingElement_Aperture;
                                        }

                                        if (!dictionary_Aperture.TryGetValue(aperture.Guid, out List<Tuple<AperturePart, zoneSurface, bool>> zoneSurfaces_Aperture) || zoneSurfaces_Aperture == null)
                                        {
                                            zoneSurfaces_Aperture = new List<Tuple<AperturePart, zoneSurface, bool>>();
                                            dictionary_Aperture[aperture.Guid] = zoneSurfaces_Aperture;
                                        }

                                        zoneSurfaces_Aperture.Add(new Tuple<AperturePart, zoneSurface, bool>(keyValuePair.Value.Item1, zoneSurface, dictionary_Panel.ContainsKey(panel.Guid)));
                                    }
                                }
                            }
                        }

                        if (solarFaceSimulationResult != null)
                        {
                            List<SurfaceShade> surfaceShades = UpdateSurfaceShades(building, daysShades, zoneSurface_Panel, adjacencyCluster, solarFaceSimulationResult);
                        }

                        zoneSurface_Panel.type = adiabatic ? SurfaceType.tbdNullLink : Query.SurfaceType(panelType);

                        if (!dictionary_Panel.TryGetValue(panel.Guid, out List<Tuple<zoneSurface, bool>> zoneSurfaces_Panel) || zoneSurfaces_Panel == null)
                        {
                            zoneSurfaces_Panel = new List<Tuple<zoneSurface, bool>>();
                            dictionary_Panel[panel.Guid] = zoneSurfaces_Panel;
                        }

                        zoneSurfaces_Panel.Add(new Tuple<zoneSurface, bool>(zoneSurface_Panel, reverse));

                        if (updateGuids)
                        {
                            adjacencyCluster.AddObject(Analytical.Create.Panel(panel_Temp));
                        }
                    }
                }
            }

            foreach (KeyValuePair<Guid, List<Tuple<zoneSurface, bool>>> keyValuePair in dictionary_Panel)
            {
                if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
                {
                    continue;
                }

                keyValuePair.Value[1].Item1.linkSurface = keyValuePair.Value[0].Item1;
                keyValuePair.Value[0].Item1.linkSurface = keyValuePair.Value[1].Item1;

                Panel panel = adjacencyCluster.GetObject<Panel>(keyValuePair.Key);

                bool reverse_1 = keyValuePair.Value[0].Item2;
                bool reverse_2 = keyValuePair.Value[1].Item2;

                if (keyValuePair.Value[0].Item1.inclination == 0 || keyValuePair.Value[0].Item1.inclination == 180)
                {
                    if (reverse_1)
                    {
                        keyValuePair.Value[0].Item1.reversed = 1;
                    }
                    if (reverse_2)
                    {
                        //reverse only one that are outside when test if in shell
                        keyValuePair.Value[1].Item1.reversed = 1;
                    }
                    if (!reverse_1 && !reverse_2)
                    {
                        keyValuePair.Value.Last().Item1.reversed = 1;
                    }
                }
                else
                {
                    float orientation = keyValuePair.Value[1].Item1.orientation;
                    orientation += 180;
                    if (orientation >= 360)
                    {
                        orientation -= 360;
                    }

                    keyValuePair.Value[1].Item1.orientation = orientation;

                    List<Space> spaces_Adjacent = adjacencyCluster.GetSpaces(panel);
                    bool adjacent = spaces_Adjacent != null && spaces_Adjacent.Count > 1;



                    if (panel.PanelGroup == PanelGroup.Floor && adjacent)
                    {
                        keyValuePair.Value[1].Item1.inclination = Math.Abs(180 - keyValuePair.Value[1].Item1.inclination);

                        if (reverse_1)
                        {
                            //reverse only one that are outside when test if in shell
                            keyValuePair.Value[0].Item1.reversed = 1;
                        }


                        if (reverse_2)
                        {
                            //reverse only one that are outside when test if in shell
                            keyValuePair.Value[1].Item1.reversed = 1;
                        }
                    }
                    else
                    {
                        keyValuePair.Value[1].Item1.reversed = 1;
                    }

                    float inclination = keyValuePair.Value[1].Item1.inclination;
                    while (inclination > 180)
                    {
                        inclination -= 180;
                    }
                    keyValuePair.Value[1].Item1.inclination = inclination;
                }
            }

            foreach (KeyValuePair<Guid, List<Tuple<AperturePart, zoneSurface, bool>>> keyValuePair in dictionary_Aperture)
            {
                if (keyValuePair.Value == null || keyValuePair.Value.Count <= 1)
                {
                    continue;
                }

                List<zoneSurface> zoneSurfaces = null;

                zoneSurfaces = keyValuePair.Value.FindAll(x => x.Item1 == AperturePart.Frame).ConvertAll(x => x.Item2);
                if (zoneSurfaces.Count == 2)
                {
                    zoneSurfaces[1].linkSurface = zoneSurfaces[0];
                    zoneSurfaces[0].linkSurface = zoneSurfaces[1];
                }


                zoneSurfaces = keyValuePair.Value.FindAll(x => x.Item1 == AperturePart.Pane).ConvertAll(x => x.Item2);
                if (zoneSurfaces.Count == 2)
                {
                    zoneSurfaces[1].linkSurface = zoneSurfaces[0];
                    zoneSurfaces[0].linkSurface = zoneSurfaces[1];
                }

                foreach (Tuple<AperturePart, zoneSurface, bool> tuple in keyValuePair.Value)
                {
                    if (!tuple.Item3)
                    {
                        continue;
                    }

                    float orientation = tuple.Item2.orientation;
                    orientation += 180;
                    if (orientation >= 360)
                    {
                        orientation -= 360;
                    }
                    tuple.Item2.orientation = orientation;

                    tuple.Item2.reversed = 1;  // only second panel window does not workk internla
                }
            }
        }
    }
}