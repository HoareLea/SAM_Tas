using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel RunWorkflow(this AnalyticalModel analyticalModel, string path_TBD, string path_gbXML = null, Weather.WeatherData weatherData = null, List<DesignDay> heatingDesignDays = null, List<DesignDay> coolingDesignDays = null, List<SurfaceOutputSpec> surfaceOutputSpecs = null, bool unmetHours = true)
        {
            if(analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
            {
                return null;
            }

            string directory = System.IO.Path.GetDirectoryName(path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            if (!string.IsNullOrWhiteSpace(path_gbXML))
            {
                string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));

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
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    if (weatherData != null)
                    {
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData);
                    }

                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        tBDDocument.Building.GUID = guid;
                    }

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

                using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                {
                    TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;

                    t3DDocument.TogbXML(path_gbXML, true, true, true);
                    t3DDocument.SetUseBEWidths(false);
                    analyticalModel = Query.UpdateT3D(analyticalModel, t3DDocument);

                    t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                    t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                    t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                    sAMT3DDocument.Save();

                    Convert.ToTBD(t3DDocument, path_TBD, 1, 365, 15, true);
                }
            }

            AdjacencyCluster adjacencyCluster = null;

            bool hasWeatherData = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

                analyticalModel = Query.UpdateFacingExternal(analyticalModel, tBDDocument);
                AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);
                UpdateBuildingElements(tBDDocument, analyticalModel);

                adjacencyCluster = analyticalModel.AdjacencyCluster;
                UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

                UpdateZones(tBDDocument.Building, analyticalModel, true);

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                sAMTBDDocument.Save();
            }

            if(!hasWeatherData)
            {
                return new AnalyticalModel(analyticalModel, adjacencyCluster);
            }

            Query.Sizing(path_TBD, analyticalModel, false, true);

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                if (surfaceOutputSpecs != null && surfaceOutputSpecs.Count > 0)
                {
                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs);
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs[0].Name);
                    sAMTBDDocument.Save();
                }

                Simulate(tBDDocument, path_TSD, 1, 1);
            }

            adjacencyCluster = analyticalModel.AdjacencyCluster;
            List<Core.Result> results = AddResults(path_TSD, adjacencyCluster);

            if (unmetHours)
            {
                List<Core.Result> results_UnmetHours = Query.UnmetHours(path_TSD, path_TBD, 0.5);
                if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                {
                    foreach (Core.Result result in results_UnmetHours)
                    {
                        if (result is AdjacencyClusterSimulationResult)
                        {
                            adjacencyCluster.AddObject(result);
                        }
                        else if (result is SpaceSimulationResult)
                        {
                            SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result;

                            List<SpaceSimulationResult> spaceSimulationResults = Query.Results(results, spaceSimulationResult);
                            if (spaceSimulationResults == null)
                                results.Add(spaceSimulationResult);
                            else
                                spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, Analytical.SpaceSimulationResultParameter.UnmetHours, Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours));
                        }
                    }
                }
            }

            AnalyticalModel analyticalModel_Temp = new AnalyticalModel(analyticalModel, adjacencyCluster);

            //if (System.IO.File.Exists(path_TSD))
            //{
            //    string path_TPD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tpd"));

            //    CreateTPD(path_TPD, path_TSD, analyticalModel_Temp);
            //}

            adjacencyCluster = UpdateDesignLoads(path_TBD, analyticalModel_Temp.AdjacencyCluster);

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }
  }
}