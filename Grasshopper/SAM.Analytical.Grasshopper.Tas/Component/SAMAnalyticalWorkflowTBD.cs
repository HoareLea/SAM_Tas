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
    public class SAMAnalyticalWorkflowTBD : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c34fc8ae-44a9-4c62-aa01-b1a11394b418");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTBD;


        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalWorkflowTBD()
          : base("SAMAnalytical.Workflow", "SAMAnalytical.Workflow",
              "SAM Analytical Workflow",
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

                global::Grasshopper.Kernel.Parameters.Param_GenericObject genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceOutputSpec_", NickName = "surfaceOutputSpec_", Description = "Surface Output Spec", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(genericObject, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_FilePath() { Name = "_path_TSD", NickName = "_path_TSD", Description = "A file path to a Tas file TSD", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
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

            List<DesignDay> heatingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            List<DesignDay> coolingDesignDays = new List<DesignDay>();
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

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

            int count = 5;
            if(weatherData != null)
            {
                count++;
            }

            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                count++;
            }

            using (Core.Windows.Forms.ProgressForm progressForm = new Core.Windows.Forms.ProgressForm("SAM Workflow - TBD Update", count))
            {
                progressForm.Update("Opening TBD document");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
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

                        if(Math.Abs(latitude_TBD - latitude_WeatherData) > 0.01 || Math.Abs(longitude_TBD - longitude_WeatherDate) > 0.01)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "WeatherData Longitude or Latitude mismatch");
                        }
                    }

                    progressForm.Update("Adding HDD and CDD Day Types");

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

                    progressForm.Update("Converting to TBD");
                    Analytical.Tas.Convert.ToTBD(analyticalModel, tBDDocument);

                    progressForm.Update("Updating Zones");
                    Analytical.Tas.Modify.UpdateZones(tBDDocument.Building, analyticalModel, true);

                    if (coolingDesignDays != null || heatingDesignDays != null)
                    {
                        progressForm.Update("Adding Design Days");
                        Analytical.Tas.Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                    }

                    progressForm.Update("Updating Shades");
                    simulate = Analytical.Tas.Modify.UpdateShading(tBDDocument, analyticalModel);

                    sAMTBDDocument.Save();
                }
            }

            analyticalModel = Analytical.Tas.Modify.RunWorkflow(analyticalModel, path_TBD, null, null, heatingDesignDays, coolingDesignDays, surfaceOutputSpecs, unmetHours, simulate, false);

            index = Params.IndexOfOutputParam("_path_TSD");
            if (index != -1)
            {
                string directory = System.IO.Path.GetDirectoryName(path_TBD);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TBD);

                string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

                dataAccess.SetData(index, path_TSD);
            }

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
                dataAccess.SetData(index, analyticalModel);

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, true);
            }
        }
    }
}