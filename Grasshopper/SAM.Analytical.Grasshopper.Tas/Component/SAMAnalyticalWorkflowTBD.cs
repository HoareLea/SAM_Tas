using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using SAM.Core.Windows.Forms;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalWorkflowTBD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c34fc8ae-44a9-4c62-aa01-b1a11394b418");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.8";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD3;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalWorkflowTBD()
          : base("SAMAnalytical.WorkflowTBD", "SAMAnalytical.WorkflowTBD",
              "SAM Analytical Workflow TBD",
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item}, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_FilePath filePath = new global::Grasshopper.Kernel.Parameters.Param_FilePath() { Name = "_path_TBD", NickName = "_path_TBD", Description = "A file path to a Tas file TBD", Access = GH_ParamAccess.item };
                filePath.WireDisplay = GH_ParamWireDisplay.hidden;
                result.Add(new GH_SAMParam(filePath, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_addIZAMs_", NickName = "_addIZAMs_", Description = "Add IZAMs", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_sizing_", NickName = "_sizing_", Description = "Sizing", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_simulate_", NickName = "_simulate_", Description = "Simulates the model from 1 to 365 day.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runDaylightFactors_", NickName = "_runDaylightFactors_", Description = "Calculates Daylight Factors.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_GenericObject genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceOutputSpec_", NickName = "surfaceOutputSpec_", Description = "Surface Output Spec", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(genericObject, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_removeTBD_", NickName = "_removeTBD_", Description = "If True existing TBD file will be deleted before simulation", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "saveWeather_", NickName = "saveWeather_", Description = "Save Wetaher in Analytical Model", Optional = true, Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath() { Name = "_path_TSD", NickName = "_path_TSD", Description = "A file path to a Tas file TSD", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly imported?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful = Params.IndexOfOutputParam("successful");
            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
                run = false;

            if (!run)
                return;

            string path_TBD = null;
            index = Params.IndexOfInputParam("_path_TBD");
            if (index == -1 || !dataAccess.GetData(index, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            WeatherData weatherData = null;
            index = Params.IndexOfInputParam("weatherData_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref weatherData))
                {
                    weatherData = null;
                }
            }

            if(weatherData == null)
            {
                analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out weatherData);
            }

            if(weatherData != null)
            {
                weatherData = new WeatherData(weatherData);
            }

            List<DesignDay> heatingDesignDays = [];
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            if(heatingDesignDays != null)
            {
                heatingDesignDays = heatingDesignDays.ConvertAll(x => x.Clone());
            }

            List<DesignDay> coolingDesignDays = [];
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            if (coolingDesignDays != null)
            {
                coolingDesignDays = coolingDesignDays.ConvertAll(x => x.Clone());
            }

            List<SurfaceOutputSpec> surfaceOutputSpecs = null;

            List<GH_ObjectWrapper> objectWrappers = [];
            index = Params.IndexOfInputParam("surfaceOutputSpec_");
            if (index != -1 && dataAccess.GetDataList(index, objectWrappers) && objectWrappers != null && objectWrappers.Count != 0)
            {
                surfaceOutputSpecs = [];
                foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
                {
                    object value = objectWrapper.Value;
                    if (value is IGH_Goo)
                    {
                        value = (value as dynamic)?.Value;
                    }

                    if (value is bool && ((bool)value))
                    {
                        SurfaceOutputSpec surfaceOutputSpec = new ("Tas.Simulate");
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
                        surfaceOutputSpecs = [new ("Tas.Simulate")];
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

            bool unmetHours = false;
            index = Params.IndexOfInputParam("_runUnmetHours_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHours))
                    unmetHours = true;

            if (System.IO.File.Exists(path_TBD))
            {
                System.IO.File.Delete(path_TBD);
            }

            bool simulate = false;
            index = Params.IndexOfInputParam("_simulate_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref simulate))
                {
                    simulate = false;
                }
            }
                

            bool sizing = true;
            index = Params.IndexOfInputParam("_sizing_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref sizing))
                {
                    sizing = true;
                }
            }

            bool removeExistingTBD = false;
            index = Params.IndexOfInputParam("_removeTBD_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref removeExistingTBD))
                {
                    removeExistingTBD = false;
                }
            }


            int count = 5;
            if(weatherData != null)
            {
                count++;
            }

            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                count++;
            }

            bool shadingUpdated = false;

            using (Core.Windows.Forms.ProgressForm progressForm = new ("SAM Workflow - TBD Update", count))
            {
                progressForm.Update("Opening TBD document");
                using (SAMTBDDocument sAMTBDDocument = new (path_TBD))
                {
                    TBD.TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    if (weatherData != null)
                    {
                        progressForm.Update("Updating Weather Data");
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, analyticalModel.AdjacencyCluster.BuildingHeight());

                        double latitude_TBD = Core.Query.Round(analyticalModel.Location.Latitude, 0.01);
                        double longitude_TBD = Core.Query.Round(analyticalModel.Location.Longitude, 0.01);

                        double latitude_WeatherData = Core.Query.Round(weatherData.Latitude, 0.01);
                        double longitude_WeatherDate = Core.Query.Round(weatherData.Longitude, 0.01);

                        if (Math.Abs(latitude_TBD - latitude_WeatherData) > 0.01 || Math.Abs(longitude_TBD - longitude_WeatherDate) > 0.01)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "WeatherData Longitude or Latitude mismatch");
                        }
                    }

                    progressForm.Update("Adding HDD and CDD Day Types");

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

                    progressForm.Update("Converting to TBD");
                    Analytical.Tas.Convert.ToTBD(analyticalModel, tBDDocument);

                    progressForm.Update("Updating Zones");
                    Analytical.Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                    if (coolingDesignDays != null || heatingDesignDays != null)
                    {
                        progressForm.Update("Adding Design Days");
                        Analytical.Tas.Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                    }

                    //progressForm.Update("Updating Shades");
                    //shadingUpdated = Analytical.Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                    sAMTBDDocument.Save();
                }
            }

            int simulate_From = -1;
            int simulate_To = -1;

            if (simulate)
            {
                simulate_From = 1;
                simulate_To = 365;
            }

            if (shadingUpdated)
            {
                if(!simulate)
                {
                    simulate_From = 1;
                    simulate_To = 1;
                    simulate = true;
                }
            }

            bool addIZAMs = true;
            index = Params.IndexOfInputParam("_addIZAMs_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref addIZAMs))
                {
                    addIZAMs = true;
                }
            }

            WorkflowSettings workflowSettings = new ()
            {
                Path_TBD = path_TBD,
                Path_gbXML = null,
                WeatherData = null,
                DesignDays_Heating = heatingDesignDays,
                DesignDays_Cooling = coolingDesignDays,
                SurfaceOutputSpecs = surfaceOutputSpecs,
                UnmetHours = unmetHours,
                Simulate = simulate,
                Sizing = sizing,
                UpdateZones = false,
                UseWidths = false,
                AddIZAMs = addIZAMs,
                SimulateFrom = simulate_From,
                SimulateTo = simulate_To,
                RemoveExistingTBD = removeExistingTBD
            };

            analyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData_Temp);
            analyticalModel.RemoveValue(AnalyticalModelParameter.WeatherData);

            Modify.RunWorkflow(analyticalModel, workflowSettings);

            if(weatherData_Temp != null)
            {
                analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, weatherData_Temp);
            }

            bool saveWeather = false;
            index = Params.IndexOfInputParam("saveWeather_");
            if (index != -1 && dataAccess.GetData(index, ref saveWeather) && saveWeather)
            {
                analyticalModel = new AnalyticalModel(analyticalModel);
                analyticalModel.UpdateWeather(weatherData, coolingDesignDays, heatingDesignDays);
            }

            index = Params.IndexOfOutputParam("_path_TSD");
            if (index != -1)
            {
                string directory = System.IO.Path.GetDirectoryName(path_TBD);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

                string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

                dataAccess.SetData(index, path_TSD);
            }

            bool calculateDaylightFactors = false;
            index = Params.IndexOfInputParam("_runDaylightFactors_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref calculateDaylightFactors))
                {
                    calculateDaylightFactors = false;
                }
            }

            if(calculateDaylightFactors && analyticalModel != null)
            {
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                if(adjacencyCluster != null)
                {
                    adjacencyCluster.UpdateDaylightFactors();
                    analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
                }
            }

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open TBD", Menu_OpenTBD, Resources.SAM_TasTBD3, true, false);
            Menu_AppendItem(menu, "Open TSD", Menu_OpenTSD, Resources.SAM_TasTSD3, true, false);
        }

        private void Menu_OpenTBD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_path_TBD");
            if (index_Path == -1)
            {
                return;
            }

            string path = null;

            object @object = null;

            @object = Params.Input[index_Path].VolatileData.AllData(true)?.OfType<object>()?.ElementAt(0);
            if (@object is IGH_Goo)
            {
                path = (@object as dynamic).Value?.ToString();
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }

        private void Menu_OpenTSD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfOutputParam("_path_TSD");
            if (index_Path == -1)
            {
                return;
            }

            string path = null;

            object @object = null;

            @object = Params.Input[index_Path].VolatileData.AllData(true)?.OfType<object>()?.ElementAt(0);
            if (@object is IGH_Goo)
            {
                path = (@object as dynamic).Value?.ToString();
            }

            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return;
            }

            Core.Query.StartProcess(path);
        }
    }
}