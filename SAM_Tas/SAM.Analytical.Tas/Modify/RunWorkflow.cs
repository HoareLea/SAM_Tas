using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings)
        {
            if (analyticalModel == null || workflowSettings == null)
            {
                return null;
            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel);

            string directory = System.IO.Path.GetDirectoryName(workflowSettings.Path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(workflowSettings.Path_TBD);

            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            int count = 6;
            if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
            {
                count = count + 9;
            }

            if (workflowSettings.UpdateZones)
            {
                count++;
            }

            if (workflowSettings.AddIZAMs)
            {
                count++;
            }

            if (workflowSettings.DesignDays_Cooling != null || workflowSettings.DesignDays_Heating != null)
            {
                count++;
            }

            AdjacencyCluster adjacencyCluster = null;

            bool hasWeatherData = false;

            Core.Tas.Modify.SetProjectDirectory(directory);

            using (Core.Windows.Forms.ProgressForm simpleProgressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Preparing TBD", count))
            {
                if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
                {
                    string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));
                    if (System.IO.File.Exists(path_T3D))
                    {
                        System.IO.File.Delete(path_T3D);
                    }

                    simpleProgressForm.Update("Extracting GUID");
                    string guid = null;
                    using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                    {
                        TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;
                        guid = t3DDocument.Building.GUID;
                        sAMT3DDocument.Save();
                    }

                    float latitude = float.NaN;
                    float longitude = float.NaN;
                    float timeZone = float.NaN;
                    simpleProgressForm.Update("Opening TBD file");
                    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
                    {
                        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                        simpleProgressForm.Update("Updating Weather Data");
                        if (workflowSettings.WeatherData != null)
                        {
                            Weather.Tas.Modify.UpdateWeatherData(tBDDocument, workflowSettings.WeatherData, adjacencyCluster.BuildingHeight());
                        }

                        if (!string.IsNullOrWhiteSpace(guid))
                        {
                            tBDDocument.Building.GUID = guid;
                        }

                        simpleProgressForm.Update("Updating HDD and CDD Day Types");

                        TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                        List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
                        if (dayTypes.Find(x => x.name == "HDD") == null)
                        {
                            TBD.dayType dayType = calendar.AddDayType();
                            dayType.name = "HDD";
                        }

                        if (dayTypes.Find(x => x.name == "CDD") == null)
                        {
                            TBD.dayType dayType = calendar.AddDayType();
                            dayType.name = "CDD";
                        }

                        latitude = tBDDocument.Building.latitude;
                        longitude = tBDDocument.Building.longitude;
                        timeZone = tBDDocument.Building.timeZone;

                        sAMTBDDocument.Save();
                    }

                    simpleProgressForm.Update("Opening T3D file");
                    using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                    {
                        TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;

                        simpleProgressForm.Update("Importing gbXML");
                        t3DDocument.TogbXML(workflowSettings.Path_gbXML, true, true, true);

                        simpleProgressForm.Update("Updating T3D file");
                        t3DDocument.SetUseBEWidths(workflowSettings.UseWidths);
                        result = Query.UpdateT3D(result, t3DDocument);

                        t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                        t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                        t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                        sAMT3DDocument.Save();

                        simpleProgressForm.Update("T3D to TBD -> Shading");
                        Convert.ToTBD(t3DDocument, workflowSettings.Path_TBD, 1, 365, 15, true, workflowSettings.RemoveExistingTBD);
                    }
                }

                simpleProgressForm.Update("Opening TBD");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

                    simpleProgressForm.Update("Updating Facing External Elements");
                    result = Query.UpdateFacingExternal(result, tBDDocument);

                    simpleProgressForm.Update("Assigning Adiabatic Constructions");
                    AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);

                    simpleProgressForm.Update("Setting Adiabatic");
                    UpdateAdiabatic(tBDDocument, result, Core.Tolerance.MacroDistance);

                    simpleProgressForm.Update("Updating Building Elements");
                    UpdateBuildingElements(tBDDocument, result);

                    adjacencyCluster = result.AdjacencyCluster;
                    simpleProgressForm.Update("Updating Ids");
                    UpdateIds(adjacencyCluster, tBDDocument.Building);

                    simpleProgressForm.Update("Updating Thermal Parameters");
                    UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                    result = new AnalyticalModel(result, adjacencyCluster);

                    if (workflowSettings.UpdateZones)
                    {
                        simpleProgressForm.Update("Updating Updating Zones");
                        UpdateZones(tBDDocument.Building, result, true);
                    }

                    if (workflowSettings.DesignDays_Cooling != null || workflowSettings.DesignDays_Heating != null)
                    {
                        simpleProgressForm.Update("Adding Design Days");
                        AddDesignDays(tBDDocument, workflowSettings.DesignDays_Cooling, workflowSettings.DesignDays_Heating, 30);
                    }

                    simpleProgressForm.Update("Creating Zone Groups");
                    AddDefaultZoneGroups(tBDDocument?.Building, adjacencyCluster);

                    if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
                    {
                        simpleProgressForm.Update("Updating Aperture Types");
                        SetApertureTypes(tBDDocument.Building, adjacencyCluster, Core.Tolerance.MacroDistance);
                    }

                    if(workflowSettings.AddIZAMs)
                    {
                        simpleProgressForm.Update("Add IZAMs");
                        UpdateIZAMs(tBDDocument, adjacencyCluster);
                    }

                    sAMTBDDocument.Save();
                }

            }

            if (workflowSettings.DesignDays_Cooling != null || workflowSettings.DesignDays_Heating != null)
            {
                if (adjacencyCluster == null)
                {
                    adjacencyCluster = result.AdjacencyCluster;
                }

                if (adjacencyCluster != null)
                {
                    if (workflowSettings.DesignDays_Cooling != null)
                    {
                        for (int i = 0; i < workflowSettings.DesignDays_Cooling.Count; i++)
                        {
                            adjacencyCluster.AddObject(new DesignDay(workflowSettings.DesignDays_Cooling[i], LoadType.Cooling));
                        }
                    }

                    if (workflowSettings.DesignDays_Heating != null)
                    {
                        for (int i = 0; i < workflowSettings.DesignDays_Heating.Count; i++)
                        {
                            adjacencyCluster.AddObject(new DesignDay(workflowSettings.DesignDays_Heating[i], LoadType.Heating));
                        }
                    }
                }
            }

            result = new AnalyticalModel(result, adjacencyCluster);

            if (!hasWeatherData)
            {
                return result;
            }

            count = 2;
            if(workflowSettings.SurfaceOutputSpecs != null && workflowSettings.SurfaceOutputSpecs.Count > 0)
            {
                count = count + 2;
            }

            if(workflowSettings.Simulate)
            {
                count = count + 2;
            }

            if(workflowSettings.UnmetHours)
            {
                count++;
            }

            if(!workflowSettings.Sizing)
            {
                count--;
            }

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Simulation", count))
            {

                
                
                if(workflowSettings.Sizing)
                {
                    progressForm.Update("Sizing");
                    Query.Sizing(workflowSettings.Path_TBD, new SizingSettings() { ExcludeOutdoorAir = false, ExcludePositiveInternalGains = true }, result);
                }


                progressForm.Update("Opening TBD");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    if (workflowSettings.SurfaceOutputSpecs != null && workflowSettings.SurfaceOutputSpecs.Count > 0)
                    {
                        progressForm.Update("Updating Surface Output Specs");
                        Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, workflowSettings.SurfaceOutputSpecs);
                        progressForm.Update("Assigning Surface Output Specs");
                        Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, workflowSettings.SurfaceOutputSpecs[0].Name);
                        sAMTBDDocument.Save();
                    }

                    if (workflowSettings.Simulate)
                    {
                        progressForm.Update("Simulating Model");
                        Simulate(tBDDocument, path_TSD, workflowSettings.SimulateFrom, workflowSettings.SimulateTo);
                    }
                }

                if (!workflowSettings.Simulate)
                {
                    return result;
                }

                progressForm.Update("Adding Results");
                adjacencyCluster = result.AdjacencyCluster;
                List<Core.Result> results = AddResults(path_TSD, adjacencyCluster);

                if (workflowSettings.UnmetHours)
                {
                    progressForm.Update("Calculating Unmet Hours");
                    List<Core.Result> results_UnmetHours = Query.UnmetHours(path_TSD, workflowSettings.Path_TBD, 0.5);
                    if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                    {
                        foreach (Core.Result result_UnmetHours in results_UnmetHours)
                        {
                            if (result_UnmetHours is AdjacencyClusterSimulationResult)
                            {
                                adjacencyCluster.AddObject(result_UnmetHours);
                            }
                            else if (result_UnmetHours is SpaceSimulationResult)
                            {
                                SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result_UnmetHours;

                                List<SpaceSimulationResult> spaceSimulationResults = Query.Results(results, spaceSimulationResult);
                                if (spaceSimulationResults == null)
                                    results.Add(spaceSimulationResult);
                                else
                                    spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, Analytical.SpaceSimulationResultParameter.UnmetHours, Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours));
                            }
                        }
                    }
                }

                progressForm.Update("Updating Design Loads");
                adjacencyCluster = UpdateDesignLoads(workflowSettings.Path_TBD, adjacencyCluster);
            }

            return new AnalyticalModel(result, adjacencyCluster);
        }
    }
}