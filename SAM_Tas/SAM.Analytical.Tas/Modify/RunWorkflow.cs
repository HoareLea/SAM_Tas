// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, WorkflowSettings workflowSettings)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            if(workflowSettings == null)
            {
                workflowSettings = new WorkflowSettings();
            }

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings);
            return workflowCalculator.Calculate(analyticalModel);

            //if (analyticalModel == null || workflowSettings == null)
            //{
            //    return null;
            //}

            //AnalyticalModel result = new AnalyticalModel(analyticalModel);

            //string directory = System.IO.Path.GetDirectoryName(workflowSettings.Path_TBD);
            //string fileName = System.IO.Path.GetFileNameWithoutExtension(workflowSettings.Path_TBD);

            //string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            //analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData);
            //if(workflowSettings?.WeatherData != null)
            //{
            //    weatherData = workflowSettings.WeatherData;
            //}

            //analyticalModel.TryGetValue(AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays);
            //if (workflowSettings?.DesignDays_Heating != null)
            //{
            //    heatingDesignDays = new SAMCollection<DesignDay>(workflowSettings.DesignDays_Heating);
            //}

            //analyticalModel.TryGetValue(AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> coolingDesignDays);
            //if (workflowSettings?.DesignDays_Cooling != null)
            //{
            //    coolingDesignDays = new SAMCollection<DesignDay>(workflowSettings.DesignDays_Cooling);
            //}

            //int count = 6;
            //if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
            //{
            //    count = count + 9;
            //}

            //if (workflowSettings.UpdateZones)
            //{
            //    count++;
            //}

            //if (workflowSettings.AddIZAMs)
            //{
            //    count++;
            //}

            //if (workflowSettings.DesignDays_Cooling != null || workflowSettings.DesignDays_Heating != null)
            //{
            //    count++;
            //}

            //AdjacencyCluster adjacencyCluster = null;

            //bool hasWeatherData = false;

            //Core.Tas.Modify.SetProjectDirectory(directory);

            //using (Core.Windows.Forms.ProgressForm simpleProgressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Preparing TBD", count))
            //{
            //    if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
            //    {
            //        string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));
            //        if (System.IO.File.Exists(path_T3D))
            //        {
            //            System.IO.File.Delete(path_T3D);
            //        }

            //        if(workflowSettings.RemoveExistingTBD)
            //        {
            //            if (System.IO.File.Exists(workflowSettings.Path_TBD))
            //            {
            //                System.IO.File.Delete(workflowSettings.Path_TBD);
            //            }
            //        }

            //        simpleProgressForm.Update("Extracting GUID");
            //        string guid = null;
            //        using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            //        {
            //            TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;
            //            guid = t3DDocument.Building.GUID;
            //            sAMT3DDocument.Save();
            //        }

            //        float latitude = float.NaN;
            //        float longitude = float.NaN;
            //        float timeZone = float.NaN;
            //        simpleProgressForm.Update("Opening TBD file");
            //        using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
            //        {
            //            TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

            //            simpleProgressForm.Update("Updating Weather Data");
            //            if (weatherData != null)
            //            {
            //                Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, adjacencyCluster.BuildingHeight());
            //            }

            //            if (!string.IsNullOrWhiteSpace(guid))
            //            {
            //                tBDDocument.Building.GUID = guid;
            //            }

            //            simpleProgressForm.Update("Updating HDD and CDD Day Types");

            //            TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

            //            List<TBD.dayType> dayTypes = Query.DayTypes(calendar);
            //            if (dayTypes.Find(x => x.name == "HDD") == null)
            //            {
            //                TBD.dayType dayType = calendar.AddDayType();
            //                dayType.name = "HDD";
            //            }

            //            if (dayTypes.Find(x => x.name == "CDD") == null)
            //            {
            //                TBD.dayType dayType = calendar.AddDayType();
            //                dayType.name = "CDD";
            //            }

            //            latitude = tBDDocument.Building.latitude;
            //            longitude = tBDDocument.Building.longitude;
            //            timeZone = tBDDocument.Building.timeZone;

            //            sAMTBDDocument.Save();
            //        }

            //        simpleProgressForm.Update("Opening T3D file");
            //        using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            //        {
            //            TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;

            //            simpleProgressForm.Update("Importing gbXML");
            //            t3DDocument.TogbXML(workflowSettings.Path_gbXML, true, true, true);

            //            simpleProgressForm.Update("Updating T3D file");
            //            t3DDocument.SetUseBEWidths(workflowSettings.UseWidths);
            //            result = Query.UpdateT3D(result, t3DDocument);

            //            t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
            //            t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
            //            t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

            //            sAMT3DDocument.Save();

            //            simpleProgressForm.Update("T3D to TBD -> Shading");
            //            Convert.ToTBD(t3DDocument, workflowSettings.Path_TBD, 1, 365, 15, true, false);
            //        }
            //    }

            //    simpleProgressForm.Update("Opening TBD");
            //    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
            //    {
            //        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

            //        hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

            //        simpleProgressForm.Update("Updating Facing External Elements");
            //        result = Query.UpdateFacingExternal(result, tBDDocument);

            //        simpleProgressForm.Update("Assigning Adiabatic Constructions");
            //        AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);

            //        simpleProgressForm.Update("Setting Adiabatic");
            //        UpdateAdiabatic(tBDDocument, result, Core.Tolerance.MacroDistance);

            //        simpleProgressForm.Update("Updating Building Elements");
            //        UpdateBuildingElements(tBDDocument, result);

            //        adjacencyCluster = result.AdjacencyCluster;
            //        simpleProgressForm.Update("Updating Ids");
            //        UpdateIds(adjacencyCluster, tBDDocument.Building);

            //        simpleProgressForm.Update("Updating Thermal Parameters");
            //        UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
            //        result = new AnalyticalModel(result, adjacencyCluster);

            //        if (workflowSettings.UpdateZones)
            //        {
            //            simpleProgressForm.Update("Updating Updating Zones");
            //            UpdateZones(tBDDocument.Building, result, true);
            //        }

            //        if (coolingDesignDays != null || heatingDesignDays != null)
            //        {
            //            simpleProgressForm.Update("Adding Design Days");
            //            AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
            //        }

            //        simpleProgressForm.Update("Creating Zone Groups");
            //        AddDefaultZoneGroups(tBDDocument?.Building, adjacencyCluster);

            //        if (!string.IsNullOrWhiteSpace(workflowSettings.Path_gbXML))
            //        {
            //            simpleProgressForm.Update("Updating Aperture Types");
            //            SetApertureTypes(tBDDocument.Building, adjacencyCluster, Core.Tolerance.MacroDistance);
            //        }

            //        if(workflowSettings.AddIZAMs)
            //        {
            //            simpleProgressForm.Update("Add IZAMs");
            //            UpdateIZAMs(tBDDocument, adjacencyCluster);
            //        }

            //        sAMTBDDocument.Save();
            //    }

            //}

            //if (coolingDesignDays != null || heatingDesignDays != null)
            //{
            //    if (adjacencyCluster == null)
            //    {
            //        adjacencyCluster = result.AdjacencyCluster;
            //    }

            //    if (adjacencyCluster != null)
            //    {
            //        if (coolingDesignDays != null)
            //        {
            //            for (int i = 0; i < coolingDesignDays.Count; i++)
            //            {
            //                adjacencyCluster.AddObject(new DesignDay(coolingDesignDays[i], LoadType.Cooling));
            //            }
            //        }

            //        if (heatingDesignDays != null)
            //        {
            //            for (int i = 0; i < heatingDesignDays.Count; i++)
            //            {
            //                adjacencyCluster.AddObject(new DesignDay(heatingDesignDays[i], LoadType.Heating));
            //            }
            //        }
            //    }
            //}

            //result = new AnalyticalModel(result, adjacencyCluster);

            //if (!hasWeatherData)
            //{
            //    return result;
            //}

            //count = 2;
            //if(workflowSettings.SurfaceOutputSpecs != null && workflowSettings.SurfaceOutputSpecs.Count > 0)
            //{
            //    count = count + 2;
            //}

            //if(workflowSettings.Simulate)
            //{
            //    count = count + 2;
            //}

            //if(workflowSettings.UnmetHours)
            //{
            //    count++;
            //}

            //if(!workflowSettings.Sizing)
            //{
            //    count--;
            //}

            //using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - Simulation", count))
            //{

                
                
            //    if(workflowSettings.Sizing)
            //    {
            //        progressForm.Update("Sizing");
            //        Query.Sizing(workflowSettings.Path_TBD, new SizingSettings() { ExcludeOutdoorAir = false, ExcludePositiveInternalGains = true }, result);
            //    }


            //    progressForm.Update("Opening TBD");
            //    using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(workflowSettings.Path_TBD))
            //    {
            //        TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

            //        if (workflowSettings.SurfaceOutputSpecs != null && workflowSettings.SurfaceOutputSpecs.Count > 0)
            //        {
            //            progressForm.Update("Updating Surface Output Specs");
            //            Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, workflowSettings.SurfaceOutputSpecs);
            //            progressForm.Update("Assigning Surface Output Specs");
            //            Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, workflowSettings.SurfaceOutputSpecs[0].Name);
            //            sAMTBDDocument.Save();
            //        }

            //        if (workflowSettings.Simulate)
            //        {
            //            progressForm.Update("Simulating Model");
            //            Simulate(tBDDocument, path_TSD, workflowSettings.SimulateFrom, workflowSettings.SimulateTo);
            //        }
            //    }

            //    if (!workflowSettings.Simulate)
            //    {
            //        return result;
            //    }

            //    progressForm.Update("Adding Results");
            //    adjacencyCluster = result.AdjacencyCluster;
            //    List<Core.Result> results = AddResults(path_TSD, adjacencyCluster);

            //    if (workflowSettings.UnmetHours)
            //    {
            //        progressForm.Update("Calculating Unmet Hours");
            //        List<Core.Result> results_UnmetHours = Query.UnmetHours(path_TSD, workflowSettings.Path_TBD, 0.5);
            //        if (results_UnmetHours != null && results_UnmetHours.Count > 0)
            //        {
            //            foreach (Core.Result result_UnmetHours in results_UnmetHours)
            //            {
            //                if (result_UnmetHours is AdjacencyClusterSimulationResult)
            //                {
            //                    adjacencyCluster.AddObject(result_UnmetHours);
            //                }
            //                else if (result_UnmetHours is SpaceSimulationResult)
            //                {
            //                    SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result_UnmetHours;

            //                    List<SpaceSimulationResult> spaceSimulationResults = Query.Results(results, spaceSimulationResult);
            //                    if (spaceSimulationResults == null)
            //                        results.Add(spaceSimulationResult);
            //                    else
            //                        spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, Analytical.SpaceSimulationResultParameter.UnmetHours, Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours));
            //                }
            //            }
            //        }
            //    }

            //    progressForm.Update("Updating Design Loads");
            //    adjacencyCluster = UpdateDesignLoads(workflowSettings.Path_TBD, adjacencyCluster);
            //}

            //return new AnalyticalModel(result, adjacencyCluster);
        }

        public static Dictionary<string, AnalyticalModel> RunWorkflow(this IEnumerable<AnalyticalModel> analyticalModels, WorkflowSettings workflowSettings, string directory, bool parallel = true, bool saveAnalyticalModels = false)
        {
            if (workflowSettings == null || analyticalModels is null || !analyticalModels.Any())
            {
                return null;
            }

            List<Tuple<string, string, AnalyticalModel>> tuples = [.. Enumerable.Repeat<Tuple<string, string, AnalyticalModel>>(null, analyticalModels.Count())];

            Func<int, int, string> getName = (i, count) =>
            {
                int charCount = count.ToString().Length;

                string result = i.ToString();
                while (result.Length < charCount)
                {
                    result = "0" + result;
                }

                return result;
            };

            int count = analyticalModels.Count();

            Action<int> action = i =>
            {
                AnalyticalModel analyticalModel = analyticalModels.ElementAt(i);

                if (!analyticalModel.TryGetValue("CaseDescription", out string caseDescription) || string.IsNullOrWhiteSpace(caseDescription))//CaseDescription
                {
                    caseDescription = i.ToString();
                }

                if (analyticalModel.Name is not string name || string.IsNullOrWhiteSpace(name))
                {
                    name = i.ToString();
                }

                string directory_AnalyticalModel = Path.Combine(directory, i.ToString() == caseDescription ? getName(i, count) : string.Format("{0} {1}", getName(i, count), caseDescription));
                if (!Directory.Exists(directory_AnalyticalModel))
                {
                    Directory.CreateDirectory(directory_AnalyticalModel);
                }

                string path_gbXML = Path.Combine(directory_AnalyticalModel, name + ".gbXML");

                gbXMLSerializer.gbXML gbXML = Analytical.gbXML.Convert.TogbXML(analyticalModel);
                if (gbXML == null)
                {
                    return;
                }

                bool exported = Core.gbXML.Create.gbXML(gbXML, path_gbXML);
                if (!exported)
                {
                    return;
                }

                string path_tbd = Path.Combine(directory_AnalyticalModel, name + ".tbd");

                WorkflowSettings workflowSettings_AnalyticalModel = new(workflowSettings)
                {
                    Path_gbXML = path_gbXML,
                    Path_TBD = path_tbd
                };

                WorkflowCalculator workflowCalculator = new(workflowSettings_AnalyticalModel);

                tuples[i] = new(directory_AnalyticalModel, name, workflowCalculator.Calculate(analyticalModel));
            };

            if (parallel)
            {
                Parallel.For(0, count, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount - 1
                },
                action.Invoke);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    action.Invoke(i);
                }
            }

            Dictionary<string, AnalyticalModel> result = [];
            foreach (Tuple<string, string, AnalyticalModel> tuple in tuples)
            {
                if (tuple is null)
                {
                    continue;
                }

                result[tuple.Item1] = tuple.Item3;

                if(saveAnalyticalModels)
                {
                    string path_json = Path.Combine(tuple.Item1, tuple.Item2 + ".json");
                    Core.Convert.ToFile(tuple.Item3, path_json);
                }
            }

            return result;
        }
    }
}