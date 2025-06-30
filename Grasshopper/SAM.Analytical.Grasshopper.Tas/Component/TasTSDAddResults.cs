using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Tas.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SAM.Analytical.Grasshopper.Tas
{
    public class TasTSDAddResults : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("d2e6ed15-d7aa-4282-87fb-1c5081494df1");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.8";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_TasTSD3;

        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public TasTSDAddResults()
          : base("Tas.TSDAddResults", "Tas.TSDAddResults",
              "Updates an AdjacencyCluster from a TSD file with results. \n Cooling/Heating load and all variables per Space, Zone and Model \n *click "+" on inputs to add UnmetHours and UnmetHoursMargin",
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
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_analyticalObject", NickName = "_analyticalObject", Description = "A SAM analytical object such as an adjacency cluster or an analytical model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTSD", NickName = "_pathTasTSD", Description = "A file path to a TasTSD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_pathTasTBD", NickName = "_pathTasTBD", Description = "A file path to a TasTBD file.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean @boolean = null;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_runUnmetHours_", NickName = "_runUnmetHours_", Description = "Calculates the amount of hours that the Zone/Space will be outside of the thermostat setpoint (unmet hours).", Access = GH_ParamAccess.item };
                @boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Number number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_unmetHoursMargin_", NickName = "_unmetHoursMargin_", Description = "Unmet Hours Calculation Margin, a setpoint Band", Access = GH_ParamAccess.item };
                number.SetPersistentData(0.5);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "analyticalObject", NickName = "analyticalObject", Description = "SAM Analytical Object such as AdjacencyCluster or AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "adjacencyClusterSimulationResultHeating", NickName = "adjacencyClusterSimulationResultHeating", Description = "SAM Analytical AdjacencyClusterSimulationResult for Heating", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "zoneSimulationResultsHeating", NickName = "zoneSimulationResultsHeating", Description = "SAM Analytical ZoneSimulationResults for Heating", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "spaceSimulationResultsHeating", NickName = "spaceSimulationResultsHeating", Description = "SAM Analytical SpaceSimulationResults for Heating", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceSimulationResultsHeating", NickName = "surfaceSimulationResultsHeating", Description = "SAM Analytical SurfaceSimulationResultsHeating", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "adjacencyClusterSimulationResultCooling", NickName = "adjacencyClusterSimulationResultCooling", Description = "SAM Analytical AdjacencyClusterSimulationResult for Cooling", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "zoneSimulationResultsCooling", NickName = "zoneSimulationResultsCooling", Description = "SAM Analytical ZoneSimulationResults for Cooling", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "spaceSimulationResultsCooling", NickName = "spaceSimulationResultsCooling", Description = "SAM Analytical SpaceSimulationResults for Cooling", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "surfaceSimulationResultsCooling", NickName = "surfaceSimulationResultsCooling", Description = "SAM Analytical SurfaceSimulationResultsCooling", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Correctly added?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return result.ToArray();
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
                run = false;

            if (!run)
                return;

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

            index = Params.IndexOfInputParam("_analyticalObject");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Core.SAMObject sAMObject = null;
            if (!dataAccess.GetData(index, ref sAMObject) || sAMObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool unmetHours = false;

            index = Params.IndexOfInputParam("_runUnmetHours_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHours))
                    unmetHours = true;

            double unmetHoursMargin = 0.5;
            index = Params.IndexOfInputParam("_unmetHoursMargin_");
            if (index != -1)
                if (!dataAccess.GetData(index, ref unmetHoursMargin))
                    unmetHoursMargin = 0.5;

            AdjacencyCluster adjacencyCluster = null;
            if (sAMObject is AdjacencyCluster)
                adjacencyCluster = new AdjacencyCluster((AdjacencyCluster)sAMObject);
            else if(sAMObject is AnalyticalModel)
                adjacencyCluster = ((AnalyticalModel)sAMObject).AdjacencyCluster;

            List<Core.Result> results = null;
            if(adjacencyCluster != null)
            {
                results = Analytical.Tas.Modify.AddResults(path_TSD, adjacencyCluster);

                if (unmetHours)
                {
                    List<Core.Result> results_UnmetHours = Analytical.Tas.Query.UnmetHours(path_TSD, path_TBD, unmetHoursMargin);
                    if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                    {
                        foreach (Core.Result result in results_UnmetHours)
                        {
                            if (result is AdjacencyClusterSimulationResult)
                            {
                                adjacencyCluster.AddObject(result);
                                results.Add(result);
                            }
                            else if (result is SpaceSimulationResult)
                            {
                                SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result;

                                List<SpaceSimulationResult> spaceSimulationResults = Analytical.Tas.Query.Results(results, spaceSimulationResult);
                                if (spaceSimulationResults == null || spaceSimulationResults.Count == 0)//if (spaceSimulationResults == null)
                                    results.Add(spaceSimulationResult);
                                else
                                    spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, SpaceSimulationResultParameter.UnmetHourFirstIndex, SpaceSimulationResultParameter.UnmetHours, SpaceSimulationResultParameter.OccupiedUnmetHours));
                            }
                        }
                    }
                }

                adjacencyCluster = Analytical.Tas.Modify.UpdateDesignLoads(path_TBD, adjacencyCluster);

                if (sAMObject is AdjacencyCluster)
                    sAMObject = adjacencyCluster;
                else if (sAMObject is AnalyticalModel)
                    sAMObject = new AnalyticalModel((AnalyticalModel)sAMObject, adjacencyCluster);
            }

            index = Params.IndexOfOutputParam("analyticalObject");
            if (index != -1)
                dataAccess.SetData(index, sAMObject);

            index = Params.IndexOfOutputParam("zoneSimulationResultsHeating");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is ZoneSimulationResult && ((ZoneSimulationResult)x).LoadType() == LoadType.Heating));

            index = Params.IndexOfOutputParam("spaceSimulationResultsHeating");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SpaceSimulationResult && ((SpaceSimulationResult)x).LoadType() == LoadType.Heating));

            index = Params.IndexOfOutputParam("zoneSimulationResultsCooling");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is ZoneSimulationResult && ((ZoneSimulationResult)x).LoadType() == LoadType.Cooling));

            index = Params.IndexOfOutputParam("spaceSimulationResultsCooling");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SpaceSimulationResult && ((SpaceSimulationResult)x).LoadType() == LoadType.Cooling));

            index = Params.IndexOfOutputParam("surfaceSimulationResultsCooling");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SurfaceSimulationResult && Analytical.Query.LoadType(((SurfaceSimulationResult)x)) == LoadType.Cooling));

            index = Params.IndexOfOutputParam("surfaceSimulationResultsHeating");
            if (index != -1)
                dataAccess.SetDataList(index, results?.FindAll(x => x is SurfaceSimulationResult && Analytical.Query.LoadType(((SurfaceSimulationResult)x)) == LoadType.Heating));

            index = Params.IndexOfOutputParam("adjacencyClusterSimulationResultCooling");
            if (index != -1)
                dataAccess.SetData(index, results?.Find(x => x is AdjacencyClusterSimulationResult && Analytical.Query.LoadType(((AdjacencyClusterSimulationResult)x)) == LoadType.Cooling));

            index = Params.IndexOfOutputParam("adjacencyClusterSimulationResultHeating");
            if (index != -1)
                dataAccess.SetData(index, results?.Find(x => x is AdjacencyClusterSimulationResult && Analytical.Query.LoadType(((AdjacencyClusterSimulationResult)x)) == LoadType.Heating));

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, results != null && results.Count != 0);
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