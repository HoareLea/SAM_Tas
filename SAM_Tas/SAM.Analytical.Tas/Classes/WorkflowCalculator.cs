using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Weather;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class WorkflowCalculator : IJSAMObject
    {
        public event WorkflowCalculatorStartedEventHandler Started;
        public event WorkflowCalculatorUpdatingEventHandler Updating;
        public event WorkflowCalculatorStartedEventHandler Ended;

        public WorkflowSettings WorkflowSettings { get; set; }

        public WorkflowCalculator()
        {

        }

        public WorkflowCalculator(JObject jObject)
        {
            FromJObject(jObject);
        }

        public WorkflowCalculator(WorkflowSettings workflowSettings)
        {
            WorkflowSettings = workflowSettings == null ? null : new WorkflowSettings(workflowSettings);
        }

        public AnalyticalModel Calculate(AnalyticalModel analyticalModel)
        {
            if (analyticalModel == null || WorkflowSettings == null)
            {
                return null;
            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel);

            string directory = System.IO.Path.GetDirectoryName(WorkflowSettings.Path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(WorkflowSettings.Path_TBD);

            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData);
            if (WorkflowSettings?.WeatherData != null)
            {
                weatherData = WorkflowSettings.WeatherData;
            }

            analyticalModel.TryGetValue(AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays);
            if (WorkflowSettings?.DesignDays_Heating != null)
            {
                heatingDesignDays = new SAMCollection<DesignDay>(WorkflowSettings.DesignDays_Heating);
            }

            analyticalModel.TryGetValue(AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> coolingDesignDays);
            if (WorkflowSettings?.DesignDays_Cooling != null)
            {
                coolingDesignDays = new SAMCollection<DesignDay>(WorkflowSettings.DesignDays_Cooling);
            }

            int count = 6;
            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
            {
                count = count + 9;
            }

            if (WorkflowSettings.UpdateZones)
            {
                count++;
            }

            if (WorkflowSettings.AddIZAMs)
            {
                count++;
            }

            if (WorkflowSettings.DesignDays_Cooling != null || WorkflowSettings.DesignDays_Heating != null)
            {
                count++;
            }

            AdjacencyCluster adjacencyCluster = null;

            bool hasWeatherData = false;

            Core.Tas.Modify.SetProjectDirectory(directory);

            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
            {
                string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));
                if (System.IO.File.Exists(path_T3D))
                {
                    System.IO.File.Delete(path_T3D);
                }

                if (WorkflowSettings.RemoveExistingTBD)
                {
                    if (System.IO.File.Exists(WorkflowSettings.Path_TBD))
                    {
                        System.IO.File.Delete(WorkflowSettings.Path_TBD);
                    }
                }

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Extracting GUID"));
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
                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Opening TBD file"));
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Weather Data"));
                    if (weatherData != null)
                    {
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, adjacencyCluster.BuildingHeight());
                    }

                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        tBDDocument.Building.GUID = guid;
                    }

                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating HDD and CDD Day Types"));

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

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Opening T3D file"));
                using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                {
                    TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;

                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Importing gbXML"));
                    t3DDocument.TogbXML(WorkflowSettings.Path_gbXML, true, true, true);

                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating T3D file"));
                    t3DDocument.SetUseBEWidths(WorkflowSettings.UseWidths);
                    result = Query.UpdateT3D(result, t3DDocument);

                    t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                    t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                    t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                    sAMT3DDocument.Save();

                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("T3D to TBD -> Shading"));
                    Convert.ToTBD(t3DDocument, WorkflowSettings.Path_TBD, 1, 365, 15, true, false);
                }
            }

            Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Opening TBD"));
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Facing External Elements"));
                result = Query.UpdateFacingExternal(result, tBDDocument);

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Assigning Adiabatic Constructions"));
                Modify.AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Setting Adiabatic"));
                Modify.UpdateAdiabatic(tBDDocument, result, Core.Tolerance.MacroDistance);

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Building Elements"));
                Modify.UpdateBuildingElements(tBDDocument, result);

                adjacencyCluster = result.AdjacencyCluster;
                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Ids"));
                Modify.UpdateIds(adjacencyCluster, tBDDocument.Building);

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Thermal Parameters"));
                Modify.UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                result = new AnalyticalModel(result, adjacencyCluster);

                if (WorkflowSettings.UpdateZones)
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Updating Zones"));
                    Modify.UpdateZones(tBDDocument.Building, result, true);
                }

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Adding Design Days"));
                    Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Creating Zone Groups"));
                Modify.AddDefaultZoneGroups(tBDDocument?.Building, adjacencyCluster);

                if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Aperture Types"));
                    Modify.SetApertureTypes(tBDDocument.Building, adjacencyCluster, Core.Tolerance.MacroDistance);
                }

                if (WorkflowSettings.AddIZAMs)
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Add IZAMs"));
                    Modify.UpdateIZAMs(tBDDocument, adjacencyCluster);
                }

                sAMTBDDocument.Save();
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
            if (WorkflowSettings.SurfaceOutputSpecs != null && WorkflowSettings.SurfaceOutputSpecs.Count > 0)
            {
                count = count + 2;
            }

            if (WorkflowSettings.Simulate)
            {
                count = count + 2;
            }

            if (WorkflowSettings.UnmetHours)
            {
                count++;
            }

            if (!WorkflowSettings.Sizing)
            {
                count--;
            }

            if (WorkflowSettings.Sizing)
            {
                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Sizing"));
                Query.Sizing(WorkflowSettings.Path_TBD, new SizingSettings() { ExcludeOutdoorAir = false, ExcludePositiveInternalGains = true }, result);
            }

            Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Opening TBD"));
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                if (WorkflowSettings.SurfaceOutputSpecs != null && WorkflowSettings.SurfaceOutputSpecs.Count > 0)
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Surface Output Specs"));
                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, WorkflowSettings.SurfaceOutputSpecs);
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Assigning Surface Output Specs"));
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, WorkflowSettings.SurfaceOutputSpecs[0].Name);
                    sAMTBDDocument.Save();
                }

                if (WorkflowSettings.Simulate)
                {
                    Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Simulating Model"));
                    Modify.Simulate(tBDDocument, path_TSD, WorkflowSettings.SimulateFrom, WorkflowSettings.SimulateTo);
                }
            }

            if (!WorkflowSettings.Simulate)
            {
                return result;
            }

            Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Adding Results"));
            adjacencyCluster = result.AdjacencyCluster;
            List<Result> results = Modify.AddResults(path_TSD, adjacencyCluster);

            if (WorkflowSettings.UnmetHours)
            {
                Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Calculating Unmet Hours"));
                List<Core.Result> results_UnmetHours = Query.UnmetHours(path_TSD, WorkflowSettings.Path_TBD, 0.5);
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

            Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs("Updating Design Loads"));
            adjacencyCluster = Modify.UpdateDesignLoads(WorkflowSettings.Path_TBD, adjacencyCluster);

            return new AnalyticalModel(result, adjacencyCluster);
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("WorkflowSettings"))
            {
                WorkflowSettings = new WorkflowSettings(jObject.Value<JObject>("WorkflowSettings"));
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (jObject.ContainsKey("WorkflowSettings"))
            {
                WorkflowSettings = new WorkflowSettings(jObject.Value<JObject>("WorkflowSettings"));
            }

            return jObject;
        }
    }
}
