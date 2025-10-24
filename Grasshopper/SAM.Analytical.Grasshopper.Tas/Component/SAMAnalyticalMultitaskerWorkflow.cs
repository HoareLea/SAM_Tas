﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Analytical.Tas;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Tas;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class SAMAnalyticalMultitaskerWorkflow : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("32419d8e-061d-4e9e-99f7-489afd47bc71");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small3;


        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalMultitaskerWorkflow()
          : base("SAMAnalytical.MultitaskerWorkflow", "SAMAnalytical.MultitaskerWorkflow",
              "MultitaskerWorkflow",
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam { Name = "_analyticalModels", NickName = "_analyticalModels", Description = "AnalyticalModels", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_directory", NickName = "_directory", Description = "Directory", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new Weather.Grasshopper.GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

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

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_useBEthickness_", NickName = "_useBEthickness_", Description = "If True Building Element thickness will be applied in T3D. Default False.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_GenericObject genericObject = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceOutputSpec_", NickName = "surfaceOutputSpec_", Description = "Surface Output Spec", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(genericObject, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number number = null;

                number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_tolerance_", NickName = "_tolerance_", Description = "Tolerance", Access = GH_ParamAccess.item };
                number.SetPersistentData(Core.Tolerance.Distance);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_removeTBD_", NickName = "_removeTBD_", Description = "If True existing TBD file will be deleted before simulation", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Voluntary));

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
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "CaseDescriptions", NickName = "CaseDescriptions", Description = "CaseDescriptions", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "Directories", NickName = "Directories", Description = "Directories", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "successful", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return [.. result];
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

            string directory = null;
            index = Params.IndexOfInputParam("_directory");
            if (index == -1 || !dataAccess.GetData(index, ref directory) || string.IsNullOrWhiteSpace(directory))
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

            if(weatherData != null)
            {
                weatherData = new WeatherData(weatherData);
            }

            List<AnalyticalModel> analyticalModels = [];
            index = Params.IndexOfInputParam("_analyticalModels");
            if (index == -1 || !dataAccess.GetDataList(index, analyticalModels) || analyticalModels == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
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

            bool simulate = false;
            index = Params.IndexOfInputParam("_simulate_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref simulate))
                {
                    simulate = false;
                }
            }


            bool useBEWidths = false;
            index = Params.IndexOfInputParam("_useBEthickness_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref useBEWidths))
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


            bool unmetHours = false;
            index = Params.IndexOfInputParam("_runUnmetHours_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHours))
                    unmetHours = true;

            bool addIZAMs = true;
            index = Params.IndexOfInputParam("_addIZAMs_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref addIZAMs))
                {
                    addIZAMs = true;
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

            WorkflowSettings workflowSettings = new ()
            {
                Path_TBD = null,
                Path_gbXML = null,
                WeatherData = weatherData,
                DesignDays_Heating = heatingDesignDays,
                DesignDays_Cooling = coolingDesignDays,
                SurfaceOutputSpecs = surfaceOutputSpecs,
                UnmetHours = unmetHours,
                Simulate = simulate,
                Sizing = sizing,
                UpdateZones = true,
                UseWidths = useBEWidths,
                AddIZAMs = addIZAMs,
                SimulateFrom = 1,
                SimulateTo = 365,
                RemoveExistingTBD = removeExistingTBD,
            };

            Dictionary<string, AnalyticalModel> dictionary = Modify.RunWorkflow(analyticalModels, workflowSettings, directory);

            index = Params.IndexOfOutputParam("CaseDescriptions");
            if (index != -1)
            {
                dataAccess.SetDataList(index, dictionary?.Keys.ToList().ConvertAll(x => x == null ? null : System.IO.Path.GetFileNameWithoutExtension(x)));
            }

            index = Params.IndexOfOutputParam("Directories");
            if (index != -1)
            {
                dataAccess.SetDataList(index, dictionary?.Keys);
            }


            if (index_successful != -1)
            {
                dataAccess.SetData(index_successful, true);
            }

        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Open Directory", Menu_OpenTBD, Resources.SAM_Small, true, false);
        }

        private void Menu_OpenTBD(object sender, EventArgs e)
        {
            int index_Path = Params.IndexOfInputParam("_directory");
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

            if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
            {
                return;
            }

            Process.Start("explorer.exe", path);
        }
    }
}