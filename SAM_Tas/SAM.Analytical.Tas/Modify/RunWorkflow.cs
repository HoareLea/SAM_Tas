using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, string path_TBD, string path_gbXML = null, Weather.WeatherData weatherData = null, List<DesignDay> heatingDesignDays = null, List<DesignDay> coolingDesignDays = null, List<SurfaceOutputSpec> surfaceOutputSpecs = null, bool unmetHours = true, bool simulate = true, bool updateZones = true, int simulateFrom = 1, int simulateTo = 1)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
            {
                return null;
            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel);

            string directory = System.IO.Path.GetDirectoryName(path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            int count = 5;
            if (!string.IsNullOrWhiteSpace(path_gbXML))
            {
                count = count + 8;
            }

            if (updateZones)
            {
                count++;
            }

            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                count++;
            }

            AdjacencyCluster adjacencyCluster = null;

            bool hasWeatherData = false;

            using (Core.Windows.Forms.ProgressForm simpleProgressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Preparing TBD", count))
            {
                if (!string.IsNullOrWhiteSpace(path_gbXML))
                {
                    string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));

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
                    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                    {
                        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                        simpleProgressForm.Update("Updating Weather Data");
                        if (weatherData != null)
                        {
                            Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, adjacencyCluster.BuildingHeight());
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
                        t3DDocument.TogbXML(path_gbXML, true, true, true);

                        simpleProgressForm.Update("Updating T3D file");
                        t3DDocument.SetUseBEWidths(false);
                        result = Query.UpdateT3D(result, t3DDocument);

                        t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                        t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                        t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                        sAMT3DDocument.Save();

                        simpleProgressForm.Update("Converting to TBD");
                        Convert.ToTBD(t3DDocument, path_TBD, 1, 365, 15, true);
                    }
                }

                simpleProgressForm.Update("Opening TBD");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

                    simpleProgressForm.Update("Updating Facing External Elements");
                    result = Query.UpdateFacingExternal(result, tBDDocument);

                    simpleProgressForm.Update("Assigning Adiabatic Constructions");
                    AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);

                    simpleProgressForm.Update("Updating Building Elements");
                    UpdateBuildingElements(tBDDocument, result);

                    adjacencyCluster = result.AdjacencyCluster;

                    simpleProgressForm.Update("Updating Thermal Parameters");
                    UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                    result = new AnalyticalModel(result, adjacencyCluster);

                    if (updateZones)
                    {
                        simpleProgressForm.Update("Updating Updating Zones");
                        UpdateZones(tBDDocument.Building, result, true);
                    }

                    if (coolingDesignDays != null || heatingDesignDays != null)
                    {
                        simpleProgressForm.Update("Adding Design Days");
                        AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                    }

                    sAMTBDDocument.Save();
                }

            }

            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                if (adjacencyCluster == null)
                {
                    adjacencyCluster = result.AdjacencyCluster;
                }

                if (adjacencyCluster != null)
                {
                    if (coolingDesignDays != null)
                    {
                        for (int i = 0; i < coolingDesignDays.Count; i++)
                        {
                            adjacencyCluster.AddObject(new DesignDay(coolingDesignDays[i], LoadType.Cooling));
                        }
                    }

                    if (heatingDesignDays != null)
                    {
                        for (int i = 0; i < heatingDesignDays.Count; i++)
                        {
                            adjacencyCluster.AddObject(new DesignDay(heatingDesignDays[i], LoadType.Heating));
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
            if(surfaceOutputSpecs != null && surfaceOutputSpecs.Count > 0)
            {
                count = count + 2;
            }

            if(simulate)
            {
                count = count + 2;
            }

            if(unmetHours)
            {
                count++;
            }

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Simulation", count))
            {
                progressForm.Update("Sizing");
                Query.Sizing(path_TBD, result, false, true);

                progressForm.Update("Opening TBD");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    if (surfaceOutputSpecs != null && surfaceOutputSpecs.Count > 0)
                    {
                        progressForm.Update("Updating Surface Output Specs");
                        Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs);
                        progressForm.Update("Assigning Surface Output Specs");
                        Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs[0].Name);
                        sAMTBDDocument.Save();
                    }

                    if (simulate)
                    {
                        progressForm.Update("Simulating Model");
                        Simulate(tBDDocument, path_TSD, simulateFrom, simulateTo);
                    }
                }

                if (!simulate)
                {
                    return result;
                }

                progressForm.Update("Adding Results");
                adjacencyCluster = result.AdjacencyCluster;
                List<Core.Result> results = AddResults(path_TSD, adjacencyCluster);

                if (unmetHours)
                {
                    progressForm.Update("Calculating Unmet Hours");
                    List<Core.Result> results_UnmetHours = Query.UnmetHours(path_TSD, path_TBD, 0.5);
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
                adjacencyCluster = UpdateDesignLoads(path_TBD, adjacencyCluster);
            }

            return new AnalyticalModel(result, adjacencyCluster);
        }
    }
}