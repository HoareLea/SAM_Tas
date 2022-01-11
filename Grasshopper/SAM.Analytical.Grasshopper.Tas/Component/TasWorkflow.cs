using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasWorkflow : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("3a47ec9c-d007-4c80-b91d-d828fb05baa3");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD;


        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasWorkflow()
          : base("Tas.Workflow", "Tas.Workflow",
              "Runs Tas workflow",
              "SAM WIP", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathgbXML", NickName = "_pathgbXML", Description = "A file path to a gbXML file", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                //result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasT3D", NickName = "_pathTasT3D", Description = "A file path to a Tas file T3D", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTBD", NickName = "_pathTasTBD", Description = "A file path to a Tas file TBD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                //result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "A file path to a Tas file TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));


                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_GenericObject genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceOutputSpec_", NickName = "surfaceOutputSpec_", Description = "Surface Output Spec", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(genericObject, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number number = null;

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_tolerance_", NickName = "_tolerance_", Description = "Tolerance", Access = GH_ParamAccess.item };
                number.SetPersistentData(Core.Tolerance.Distance);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));


                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "successful", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_successful = Params.IndexOfOutputParam("successful");
            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            string path_gbXML = null;
            index = Params.IndexOfInputParam("_pathgbXML");
            if (index == -1 || !dataAccess.GetData(index, ref path_gbXML) || string.IsNullOrWhiteSpace(path_gbXML))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            //string path_T3D = null;
            //index = Params.IndexOfInputParam("_pathTasT3D");
            //if (index == -1 || !dataAccess.GetData(index, ref path_T3D) || string.IsNullOrWhiteSpace(path_T3D))
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
            //    return;
            //}

            string path_TBD = null;
            index = Params.IndexOfInputParam("_pathTasTBD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string directory = System.IO.Path.GetDirectoryName(path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

            string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));
            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            //string path_TSD = null;
            //index = Params.IndexOfInputParam("_pathTasTSD");
            //if (index == -1 || !dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
            //    return;
            //}

            WeatherData weatherData = null;
            index = Params.IndexOfInputParam("weatherData_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref weatherData))
                {
                    weatherData = null;
                }
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

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

                if(!string.IsNullOrWhiteSpace(guid))
                {
                    tBDDocument.Building.GUID = guid;
                }

                TBD.Calendar calendar = tBDDocument.Building.GetCalendar();

                List<TBD.dayType> dayTypes = Grashopper.Tas.Query.DayTypes(calendar);
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

                t3DDocument.TogbXML(path_gbXML, false, true, true);
                t3DDocument.SetUseBEWidths(false);
                analyticalModel = Analytical.Tas.Query.UpdateT3D(analyticalModel, t3DDocument);

                t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                sAMT3DDocument.Save();

                Analytical.Tas.Convert.ToTBD(t3DDocument, path_TBD, 1, 365, 15, true);
            }

            List<DesignDay> heatingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if(index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            List<DesignDay> coolingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            AdjacencyCluster adjacencyCluster = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                analyticalModel = Analytical.Tas.Query.UpdateFacingExternal(analyticalModel, tBDDocument);
                Analytical.Tas.Modify.AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);
                Analytical.Tas.Modify.UpdateBuildingElements(tBDDocument, analyticalModel);

                adjacencyCluster = analyticalModel.AdjacencyCluster;
                Analytical.Tas.Modify.UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

                Analytical.Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    Analytical.Tas.Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                sAMTBDDocument.Save();
            }

            Analytical.Tas.Query.Sizing(path_TBD, analyticalModel, false, true);

            List<SurfaceOutputSpec> surfaceOutputSpecs = null;

            List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
            index = Params.IndexOfInputParam("surfaceOutputSpec_");
            if (index != -1 && dataAccess.GetDataList(index, objectWrappers) && objectWrappers != null && objectWrappers.Count != 0)
            {
                surfaceOutputSpecs = new List<SurfaceOutputSpec>();
                foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
                {
                    object value = objectWrapper.Value;
                    if (value is IGH_Goo)
                    {
                        value = (value as dynamic)?.Value;
                    }

                    if (value is bool && ((bool)value))
                    {
                        SurfaceOutputSpec surfaceOutputSpec = new SurfaceOutputSpec("Tas.Simulate");
                        surfaceOutputSpec.SolarGain = true;
                        surfaceOutputSpec.Conduction = true;
                        surfaceOutputSpec.ApertureData = false;
                        surfaceOutputSpec.Condensation = false;
                        surfaceOutputSpec.Convection = false;
                        surfaceOutputSpec.LongWave = false;
                        surfaceOutputSpec.Temperature = false;

                        surfaceOutputSpecs.Add(surfaceOutputSpec);
                    }
                    else if (Core.Query.IsNumeric(value) && Core.Query.TryConvert(value, out double @double) && @double == 2.0)
                    {
                        surfaceOutputSpecs = new List<SurfaceOutputSpec>() { new SurfaceOutputSpec("Tas.Simulate") };
                        surfaceOutputSpecs[0].SolarGain = true;
                        surfaceOutputSpecs[0].Conduction = true;
                        surfaceOutputSpecs[0].ApertureData = true;
                        surfaceOutputSpecs[0].Condensation = true;
                        surfaceOutputSpecs[0].Convection = true;
                        surfaceOutputSpecs[0].LongWave = true;
                        surfaceOutputSpecs[0].Temperature = true;
                    }
                    else if (value is SurfaceOutputSpec)
                    {
                        surfaceOutputSpecs.Add((SurfaceOutputSpec)value);
                    }

                }
            }

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                if (surfaceOutputSpecs != null && surfaceOutputSpecs.Count > 0)
                {
                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs);
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, surfaceOutputSpecs[0].Name);
                    sAMTBDDocument.Save();
                }

                Analytical.Tas.Modify.Simulate(tBDDocument, path_TSD, 1, 1);
            }

            adjacencyCluster = analyticalModel.AdjacencyCluster;
            List<Core.Result> results = Analytical.Tas.Modify.AddResults(path_TSD, adjacencyCluster);

            bool unmetHours = false;
            index = Params.IndexOfInputParam("_runUnmetHours_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHours))
                    unmetHours = true;

            if (unmetHours)
            {
                List<Core.Result> results_UnmetHours = Analytical.Tas.Query.UnmetHours(path_TSD, path_TBD, 0.5);
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

                            List<SpaceSimulationResult> spaceSimulationResults = Analytical.Tas.Query.Results(results, spaceSimulationResult);
                            if (spaceSimulationResults == null)
                                results.Add(spaceSimulationResult);
                            else
                                spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, SpaceSimulationResultParameter.UnmetHourFirstIndex, SpaceSimulationResultParameter.UnmetHours, SpaceSimulationResultParameter.OccupiedUnmetHours));
                        }
                    }
                }
            }

            adjacencyCluster = Analytical.Tas.Modify.UpdateDesignLoads(path_TBD, adjacencyCluster);
            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
                dataAccess.SetData(index, analyticalModel);

            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }

        }
    }
}