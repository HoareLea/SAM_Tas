using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Core.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDAddBuildingResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5cba3494-637a-497a-b943-cc67eda27898");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDAddBuildingResults()
          : base("Tas.AddBuildingResults", "Tas.AddBuildingResults",
              "AddBuildingResults",
              "SAM", "Tas")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "AnalyticalMOdel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "A file path to a TasTSD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTBD", NickName = "_pathTasTBD", Description = "A file path to a TasTBD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number number = new() { Name = "_unmetHoursMargin_", NickName = "_unmetHoursMargin_", Description = "Unmet Hours Calculation Margin, a setpoint Band", Access = GH_ParamAccess.item };
                number.SetPersistentData(0.5);
                result.Add(new GH_SAMParam(number, ParamVisibility.Binding));

                @boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Connect a boolean toggle to run.", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(@boolean, ParamVisibility.Binding));

                return [.. result];
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
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "AnalyticalModel", NickName = "AnalyticalModel", Description = "SAM AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionHeating", NickName = "ConsumptionHeating", Description = "Consumption Heating [kWh]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionHeatingPerArea", NickName = "ConsumptionHeatingPerArea", Description = "Consumption Heating Per Area [kWh/m2]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionHeatingPerVolume", NickName = "ConsumptionHeatingPerVolume", Description = "Consumption Heating Per Volume [kWh/m3]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakHeatingLoad", NickName = "PeakHeatingLoad", Description = "Peak Heating Load [kW]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakHeatingLoadPerArea", NickName = "PeakHeatingLoadPerArea", Description = "Peak Heating Load Per Area [kWh/m2]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakHeatingLoadPerVolume", NickName = "PeakHeatingLoadPerVolume", Description = "Peak Heating Load Per Volume [kWh/m3]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "PeakHeatingHour", NickName = "PeakHeatingHour", Description = "Peak Heating Hour [0-8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionCooling", NickName = "ConsumptionCooling", Description = "Consumption Cooling [kWh]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionCoolingPerArea", NickName = "ConsumptionCoolingPerArea", Description = "Consumption Cooling Per Area [kWh/m2]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "ConsumptionCoolingPerVolume", NickName = "ConsumptionCoolingPerVolume", Description = "Consumption Cooling Per Volume [kWh/m3]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakCoolingLoad", NickName = "PeakCoolingLoad", Description = "Peak Cooling Load [kW]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakCoolingLoadPerArea", NickName = "PeakCoolingLoadPerArea", Description = "Peak Cooling Load Per Area [kWh/m2]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "PeakCoolingLoadPerVolume", NickName = "PeakCoolingLoadPerVolume", Description = "Peak Cooling Load Per Volume [kWh/m3]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "PeakCoolingHour", NickName = "PeakCoolingHour", Description = "Peak Cooling Hour [0-8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "UnmetHours", NickName = "UnmetHours", Description = "Unmet Hours [0-8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "UnmetHoursHeating", NickName = "UnmetHoursHeating", Description = "Unmet Cooling Hours [0-8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "UnmetHoursCooling", NickName = "UnmetHoursCooling", Description = "Unmet Heating Hours [0-8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly added?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return [.. result];
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;
            int index_Successful;

            index_Successful = Params.IndexOfOutputParam("successful");
            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                run = false;
            }

            if (!run)
            {
                return;
            }

            index = Params.IndexOfInputParam("_pathTasTSD");
            if(index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TSD = null;
            if (!dataAccess.GetData(index, ref path_TSD) || string.IsNullOrWhiteSpace(path_TSD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_pathTasTBD");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string path_TBD = null;
            if (!dataAccess.GetData(index, ref path_TBD) || string.IsNullOrWhiteSpace(path_TBD))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AnalyticalModel analyticalModel = null;
            if (!dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool runUnmetHours = true;

            index = Params.IndexOfInputParam("_runUnmetHours_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref runUnmetHours))
                {
                    runUnmetHours = true;
                }
            }


            double unmetHoursMargin = 0.5;
            index = Params.IndexOfInputParam("_unmetHoursMargin_");
            if (index != -1)
            {
                if (!dataAccess.GetData(index, ref unmetHoursMargin))
                {
                    unmetHoursMargin = 0.5;
                }
            }

            analyticalModel = new AnalyticalModel(analyticalModel);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            double consumptionHeating = double.NaN;
            double consumptionHeatingPerArea = double.NaN;
            double consumptionHeatingPerVolume = double.NaN;
            double peakHeatingLoad = double.NaN;
            double peakHeatingLoadPerArea = double.NaN;
            double peakHeatingLoadPerVolume = double.NaN;
            int peakHeatingHour = -1;
            
            double consumptionCooling = double.NaN;
            double consumptionCoolingPerArea = double.NaN;
            double consumptionCoolingPerVolume = double.NaN;
            double peakCoolingLoad = double.NaN;
            double peakCoolingLoadPerArea = double.NaN;
            double peakCoolingLoadPerVolume = double.NaN;
            int peakCoolingHour = -1;

            int unmetHours = -1;
            int unmetHours_Heating = -1;
            int unmetHours_Cooling = -1;

            if(adjacencyCluster != null)
            {
                List<Result> results = [];

                adjacencyCluster = new AdjacencyCluster(adjacencyCluster, true);

                AnalyticalModelSimulationResult analyticalModelSimulationResult = Analytical.Tas.Convert.ToSAM_AnalyticalModelSimulationResult(path_TSD, analyticalModel);
                adjacencyCluster.AddObject(analyticalModelSimulationResult);

                consumptionHeating = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.ConsumptionHeating) / 1000;
                peakHeatingLoad = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.PeakHeatingLoad) / 1000;
                peakHeatingHour = analyticalModelSimulationResult.GetValue<int>(AnalyticalModelSimulationResultParameter.PeakHeatingHour);

                consumptionCooling = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.ConsumptionCooling) / 1000;
                peakCoolingLoad = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.PeakCoolingLoad) / 1000;
                peakCoolingHour = analyticalModelSimulationResult.GetValue<int>(AnalyticalModelSimulationResultParameter.PeakCoolingHour);

                double volume = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.Volume);
                if(!double.IsNaN(volume))
                {
                    if(!double.IsNaN(consumptionHeating))
                    {
                        consumptionHeatingPerVolume = volume == 0 ? 0 : consumptionHeating / volume;
                        consumptionHeatingPerVolume = Core.Query.Round(consumptionHeatingPerVolume, 0.01);
                    }

                    if(!double.IsNaN(peakHeatingLoad))
                    {
                        peakHeatingLoadPerVolume = volume == 0 ? 0 : peakHeatingLoad / volume * 1000;
                        peakHeatingLoadPerVolume = Core.Query.Round(peakHeatingLoadPerVolume, 0.01);
                    }

                    if (!double.IsNaN(consumptionCooling))
                    {
                        consumptionCoolingPerVolume = volume == 0 ? 0 : consumptionCooling / volume;
                        consumptionCoolingPerVolume = Core.Query.Round(consumptionCoolingPerVolume, 0.01);
                    }

                    if (!double.IsNaN(peakCoolingLoad))
                    {
                        peakCoolingLoadPerVolume = volume == 0 ? 0 : peakCoolingLoad / volume * 1000;
                        peakCoolingLoadPerVolume = Core.Query.Round(peakCoolingLoadPerVolume, 0.01);
                    }
                }

                double floorArea = analyticalModelSimulationResult.GetValue<double>(AnalyticalModelSimulationResultParameter.FloorArea);
                if (!double.IsNaN(floorArea))
                {
                    if (!double.IsNaN(consumptionHeating))
                    {
                        consumptionHeatingPerArea = floorArea == 0 ? 0 : consumptionHeating / floorArea;
                        consumptionHeatingPerArea = Core.Query.Round(consumptionHeatingPerArea, 0.01);
                    }

                    if (!double.IsNaN(peakHeatingLoad))
                    {
                        peakHeatingLoadPerArea = floorArea == 0 ? 0 : peakHeatingLoad / floorArea * 1000;
                        peakHeatingLoadPerArea = Core.Query.Round(peakHeatingLoadPerArea, 0.01);
                    }

                    if (!double.IsNaN(consumptionCooling))
                    {
                        consumptionCoolingPerArea = floorArea == 0 ? 0 : consumptionCooling / floorArea;
                        consumptionCoolingPerArea = Core.Query.Round(consumptionCoolingPerArea, 0.01);
                    }

                    if (!double.IsNaN(peakCoolingLoad))
                    {
                        peakCoolingLoadPerArea = floorArea == 0 ? 0 : peakCoolingLoad / floorArea * 1000;
                        peakCoolingLoadPerArea = Core.Query.Round(peakCoolingLoadPerArea, 0.01);
                    }
                }

                results.Add(analyticalModelSimulationResult);

                if (runUnmetHours)
                {
                    List<Result> results_UnmetHours = Analytical.Tas.Query.UnmetHours(path_TSD, path_TBD, unmetHoursMargin);
                    if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                    {
                        foreach (Result result in results_UnmetHours)
                        {
                            if (result is AdjacencyClusterSimulationResult adjacencyClusterSimulationResult)
                            {
                                adjacencyCluster.AddObject(result);
                                results.Add(result);

                                LoadType loadType = adjacencyClusterSimulationResult.GetValue<LoadType>(AdjacencyClusterSimulationResultParameter.LoadType);
                                if(loadType == LoadType.Cooling)
                                {
                                    unmetHours_Cooling = adjacencyClusterSimulationResult.GetValue<int>(AdjacencyClusterSimulationResultParameter.UnmetHours);
                                }
                                else if(loadType == LoadType.Heating)
                                {
                                    unmetHours_Heating = adjacencyClusterSimulationResult.GetValue<int>(AdjacencyClusterSimulationResultParameter.UnmetHours);
                                }
                            }
                            else if (result is SpaceSimulationResult)
                            {
                                SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result;

                                List<SpaceSimulationResult> spaceSimulationResults = Analytical.Tas.Query.Results(results, spaceSimulationResult);
                                if (spaceSimulationResults == null || spaceSimulationResults.Count == 0)
                                {
                                    results.Add(spaceSimulationResult);
                                }
                                else
                                {
                                    spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, SpaceSimulationResultParameter.UnmetHourFirstIndex, SpaceSimulationResultParameter.UnmetHours, SpaceSimulationResultParameter.OccupiedUnmetHours));
                                }
                            }
                        }
                    }

                    if(unmetHours_Cooling != -1 || unmetHours_Heating != -1)
                    {
                        if(unmetHours_Cooling == -1)
                        {
                            unmetHours = unmetHours_Heating;
                        }
                        else if(unmetHours_Heating == -1)
                        {
                            unmetHours = unmetHours_Cooling;
                        }
                        else
                        {
                            unmetHours = unmetHours_Heating + unmetHours_Cooling;
                        }
                    }
                }

                adjacencyCluster = Analytical.Tas.Modify.UpdateDesignLoads(path_TBD, adjacencyCluster);

                analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
            }

            index = Params.IndexOfOutputParam("AnalyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            index = Params.IndexOfOutputParam("ConsumptionHeating");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionHeating);
            }

            index = Params.IndexOfOutputParam("ConsumptionHeatingPerArea");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionHeatingPerArea);
            }


            index = Params.IndexOfOutputParam("ConsumptionHeatingPerVolume");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionHeatingPerVolume);
            }

            index = Params.IndexOfOutputParam("PeakHeatingLoad");
            if (index != -1)
            {
                dataAccess.SetData(index, peakHeatingLoad);
            }

            index = Params.IndexOfOutputParam("PeakHeatingLoadPerArea");
            if (index != -1)
            {
                dataAccess.SetData(index, peakHeatingLoadPerArea);
            }

            index = Params.IndexOfOutputParam("PeakHeatingLoadPerVolume");
            if (index != -1)
            {
                dataAccess.SetData(index, peakHeatingLoadPerVolume);
            }

            index = Params.IndexOfOutputParam("PeakHeatingHour");
            if (index != -1)
            {
                dataAccess.SetData(index, peakHeatingHour);
            }

            index = Params.IndexOfOutputParam("ConsumptionCooling");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionCooling);
            }

            index = Params.IndexOfOutputParam("ConsumptionCoolingPerArea");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionCoolingPerArea);
            }


            index = Params.IndexOfOutputParam("ConsumptionCoolingPerVolume");
            if (index != -1)
            {
                dataAccess.SetData(index, consumptionCoolingPerVolume);
            }

            index = Params.IndexOfOutputParam("PeakCoolingLoad");
            if (index != -1)
            {
                dataAccess.SetData(index, peakCoolingLoad);
            }

            index = Params.IndexOfOutputParam("PeakCoolingLoadPerArea");
            if (index != -1)
            {
                dataAccess.SetData(index, peakCoolingLoadPerArea);
            }

            index = Params.IndexOfOutputParam("PeakCoolingLoadPerVolume");
            if (index != -1)
            {
                dataAccess.SetData(index, peakCoolingLoadPerVolume);
            }

            index = Params.IndexOfOutputParam("PeakCoolingHour");
            if (index != -1)
            {
                dataAccess.SetData(index, peakCoolingHour);
            }

            index = Params.IndexOfOutputParam("UnmetHours");
            if (index != -1)
            {
                dataAccess.SetData(index, unmetHours);
            }

            index = Params.IndexOfOutputParam("UnmetHoursHeating");
            if (index != -1)
            {
                dataAccess.SetData(index, unmetHours_Heating);
            }

            index = Params.IndexOfOutputParam("UnmetHoursCooling");
            if (index != -1)
            {
                dataAccess.SetData(index, unmetHours_Cooling);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, !double.IsNaN(consumptionCooling) || !double.IsNaN(consumptionHeating));
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
            int index_Path = Params.IndexOfInputParam("_pathTasTBD");
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
            int index_Path = Params.IndexOfInputParam("_pathTasTSD");
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